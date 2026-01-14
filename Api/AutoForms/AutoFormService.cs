using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.AutoForms
{
    /// <summary>
    /// This service drives AutoForm - the form which automatically displays fields in the admin area.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    [HostType("web")]
    public partial class AutoFormService : AutoService
	{
		private RoleService _roleService;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AutoFormService(RoleService roleService)
		{
			_roleService = roleService;

			contentCache = new AutoFormCache(PopulateContentCache, roleService);

			// When any service changes state, ensure the content cache is clear. This permits runtime created types to have autoforms.
			Api.Eventing.Events.Service.AfterCreate.AddEventListener((Context context, AutoService svc) =>
			{
				contentCache.Clear();
				return new ValueTask<AutoService>(svc);
			});
		}

		private AutoFormCache contentCache;

		/// <summary>
		/// The underlying caches
		/// </summary>
		private ConcurrentDictionary<string, AutoFormCache> _caches = new ConcurrentDictionary<string, AutoFormCache>();

		/// <summary>
		/// Registers a custom AutoForm type.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="populate"></param>
		public void RegisterCustomFormType(string typeName, Func<Context, Dictionary<string, AutoFormInfo>, ValueTask> populate)
		{
			_caches[typeName] = new AutoFormCache(populate, _roleService);
		}

		/// <summary>
		/// Gets autoform info for a particular type.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public async ValueTask<AutoFormInfo> Get(Context context, string type, string name)
		{
			// Type is usually "content", "config" or "component".
			// We only directly handle content here - config is handled by an AutoForm extension in ConfigService and component is via FrontendCodeService.
			AutoFormCache cache;

			if (type == "content")
			{
				cache = contentCache;
			}
			else
			{
				_caches.TryGetValue(type, out cache);
			}

			if (cache == null)
			{
				// Bad cache name
				return null;
			}

			var set = await cache.GetForRole(context);
			if (set == null)
			{
				// Bad role
				return null;
			}
			set.TryGetValue(name, out AutoFormInfo result);
			return result;
		}

		/// <summary>
		/// Enumerates all the content types.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ContentType> AllContentTypes()
		{
			// Get the content types and their IDs:
			foreach (var kvp in Database.ContentTypes.TypeMap)
			{
				var type = kvp.Key;
				var name = type.Name;

				var backtick = name.IndexOf('`');

				if (backtick != -1)
				{
					// Chop off the generics
					name = name.Substring(0, backtick);
				}

				if (type.IsGenericType)
				{
					var genericArgs = type.GetGenericArguments();

					// They aren't nested so we can assume these type .Names are fine as-is.
					name += "<";
					for (var g = 0; g < genericArgs.Length; g++)
					{
						if (g != 0)
						{
							name += ",";
						}

						name += genericArgs[g].Name;
					}

					name += ">";
				}

				yield return new ContentType()
				{
					Name = name
				};
			}
		}

		/// <summary>
		/// Populate the given cache for the given context.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="cache"></param>
		private async ValueTask PopulateContentCache(Context context, Dictionary<string, AutoFormInfo> cache)
		{
			// For each AutoService..
			foreach (var serviceKvp in Services.All)
			{
				if (serviceKvp.Value.IsMapping || serviceKvp.Value.InstanceType == null)
				{
					// Omit mapping services
					continue;
				}

				try
				{
					var fieldStructure = await serviceKvp.Value.GetJsonStructure(context);

					var formType = serviceKvp.Value.InstanceType;
					var formMeta = GetFormInfo(fieldStructure, formType);

					// Trigger generic event - this is where revisions can connect up.
					await Events.AutoForm.BuildMeta.Dispatch(context, formMeta, serviceKvp.Value);

					var name = formMeta.ContentType.ToLower();

					if (serviceKvp.Value.InstanceType.IsGenericType)
					{
						// unknowable here (it's e.g. v1/user/revision/.. but may not be revisions)
						formMeta.Endpoint = null;
					}
					else
					{
						formMeta.Endpoint = "v1/" + name;
					}

					cache[formMeta.ContentType.ToLower()] = formMeta;
				}
				catch (Exception e)
				{
					Log.Error(LogTag, e);
				}
			}
		}

		/// <summary>
		/// List of autoform info for the given role.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<Dictionary<string, AutoFormInfo>> AllContentForms(Context context)
		{
			return await contentCache.GetForRole(context);
		}

		/// <summary>
		/// Gets the AutoForm info such as fields available for the given :AutoForm type.
		/// </summary>
		/// <param name="jsonStructure"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public AutoFormInfo GetFormInfo(JsonStructure jsonStructure, Type type)
		{
			var info = new AutoFormInfo
			{
				Fields = new List<AutoFormField>()
			};

			var name = type.Name;

			var backtick = name.IndexOf('`');

			if (backtick != -1)
			{
				// Chop off the generic count
				name = name.Substring(0, backtick);
			}

			if (type.IsGenericType)
			{
				var genericArgs = type.GetGenericArguments();

				// They aren't nested so we can assume these type .Names are fine as-is.
				name += "<";
				for (var g = 0; g < genericArgs.Length; g++)
				{
					if (g != 0)
					{
						name += ",";
					}

					name += genericArgs[g].Name;
				}

				name += ">";
			}

			info.ContentType = name;

			foreach (var field in jsonStructure.AllFields)
			{
				if(field.Value.Hide)
				{
					continue;
				}
				var formField = BuildFieldInfo(field.Value);

				if (formField != null)
				{
					info.Fields.Add(formField);

					/* WIP
					if (formField.Data.ContainsKey("divider") && (bool)formField.Data["divider"])
					{
						var paramset = new Dictionary<string, object>();

						var dividerField = new AutoFormField()
						{
							Includable = false,
							ValueType = null,
							Module = "UI/Divider",
							Data = paramset
						};

						info.Fields.Add(dividerField);
					}
					*/
					
				}
			}

			info.Fields = info.Fields.OrderBy(f => f.Order).ToList();

			return info;
		}

		/// <summary>
		/// Converts a Json field into an AutoForm field.
		/// </summary>
		/// <param name="jsonField"></param>
		/// <returns></returns>
		public AutoFormField BuildFieldInfo(JsonField jsonField)
		{
			var fieldType = jsonField.TargetType;
			var customAttributes = jsonField.Attributes;
			var isIncludable = false;
			string valueType = null;
			var isLocalized = false;

			if (fieldType.IsGenericType)
			{
				var def = fieldType.GetGenericTypeDefinition();

				if (def == typeof(Localized<>))
				{
					isLocalized = true;
					fieldType = fieldType.GetGenericArguments()[0];
				}
			}

			if (jsonField.ContentField != null && jsonField.ContentField.VirtualInfo != null && jsonField.ContentField.VirtualInfo.IsList)
			{
				// It's a virtual list field.
				// The valueType should be e.g. "User[]".
				var virtualInfo = jsonField.ContentField.VirtualInfo;
				isIncludable = true;

				valueType = virtualInfo.Type.Name + "[]";
			}
			else
			{
				valueType = fieldType.Name;
			}

			var paramset = new Dictionary<string, object>();

			// Copy from jsonField:
			foreach (var kvp in jsonField.Data)
			{
				paramset[kvp.Key] = kvp.Value;
			}

			var field = new AutoFormField()
			{
				FieldName = jsonField.OriginalName,
				Includable = isIncludable,
				ValueType = valueType,
				Module = jsonField.Module,
				Data = paramset
			};

			var type = "text";
			var name = jsonField.OriginalName;
			var labelName = name;

			// If the field is a string and ends with Json, it's going to be either canvas (default) or a json field:
			if (fieldType == typeof(JsonString) || (fieldType == typeof(string) && labelName.EndsWith("Json")))
			{
				type = "canvas";

				foreach (var attrib in customAttributes)
				{
					if (attrib is DataAttribute)
					{
						var dat = attrib as DataAttribute;

						if (dat.Name == "contentType")
						{
							var valStr = dat.Value as string;

							if (valStr == null)
							{
								continue;
							}

							if (valStr == "application/json" || valStr == "json")
							{
								// It's a json field
								type = "json";
							}
						}
					}
				}

				// Remove "Json" from the end of the label:
				if (labelName.EndsWith("Json"))
				{
					labelName = labelName.Substring(0, labelName.Length - 4);
				}
				field.Tokeniseable = false;
			}
			else if (fieldType == typeof(string) && labelName.EndsWith("Html"))
			{
				type = "html";

				// Remove "Html" from the end of the label:
				labelName = labelName.Substring(0, labelName.Length - 4);
			}
			else if (fieldType == typeof(string) && labelName.EndsWith("Ref"))
			{
				type = "image";

				// Remove "Ref" from the end of the label:
				labelName = labelName.Substring(0, labelName.Length - 3);
				
				// If the remaining name is exactly "Icon", then use type="icon" instead:
				if(labelName.ToLower() == "icon"){
					type = "icon";
				}
			}
			else if (fieldType == typeof(string) && (labelName.EndsWith("Color") || labelName.EndsWith("Colour")))
			{
				type = "color";

				// Retain the word color/ colour in this one
			}
			else if ((fieldType == typeof(int) || fieldType == typeof(int?) || fieldType == typeof(uint) || fieldType == typeof(uint?)) && labelName != "Id" && labelName.EndsWith("Id") && Api.Database.ContentTypes.GetType(labelName.Substring(0, labelName.Length - 2).ToLower()) != null)
			{
				
				// Remove "Id" from the end of the label:
				labelName = labelName.Substring(0, labelName.Length - 2);
				
				field.Data["contentType"] = labelName;
				field.Module = "Admin/ContentSelect";

			}
			else if (fieldType == typeof(uint) && labelName.EndsWith("UserId"))
			{
				// User selection:
				field.Module = "Admin/User/Select";

				// Remove "Id" from the end of the label:
				labelName = labelName.Substring(0, labelName.Length - 2);
				field.Tokeniseable = false;
			}
			else if (fieldType == typeof(bool) || fieldType == typeof(bool?))
			{
				type = "checkbox";
			}
			else if (fieldType == typeof(DateTime) || fieldType == typeof(DateTime?))
			{
				type = "datetime";
				
				if(labelName.EndsWith("Utc")){
					// Remove "Utc" from the end of the label:
					labelName = labelName.Substring(0, labelName.Length - 3);
				}
				
				field.Data["hint"] = "All dates should be entered as UTC";
			}
			else if (fieldType == typeof(int) || fieldType == typeof(int?)
				|| fieldType == typeof(uint) || fieldType == typeof(uint?)
				|| fieldType == typeof(long) || fieldType == typeof(long?)
				|| fieldType == typeof(ulong) || fieldType == typeof(ulong?)
				|| fieldType == typeof(float) || fieldType == typeof(float?)
				|| fieldType == typeof(double) || fieldType == typeof(double?)
			)
            {
				type = "number";

				if (!field.Data.ContainsKey("step"))
                {
					if (fieldType == typeof(float) || fieldType == typeof(float?)
						|| fieldType == typeof(double) || fieldType == typeof(double?)
					)
                    {
						field.Data["step"] = "any";
					} else
                    {
						field.Data["step"] = "1";
					}
				}
			}

			field.Data["label"] = SpaceCamelCase(labelName);
			field.Data["name"] = FirstCharacterToLower(name);
			field.Data["type"] = type;

			if (!jsonField.Writeable)
			{
				field.Data["readonly"] = true;
			}

			if (isLocalized)
			{
				field.Data["localized"] = true;
			}

			// Any of these [Module] or inheritors?
			foreach (var attrib in customAttributes)
			{
				if (attrib is ModuleAttribute)
				{
					var module = attrib as ModuleAttribute;

					if (module.Name != null)
					{
						field.Module = module.Name;
					}

					if (module.Name == "Admin/ContentSelect" || module.Name == "Admin/MultiSelect")
                    {
						field.Tokeniseable = false;
                    }

					if (module.Hide)
					{
						return null;
					}
				}
				else if (attrib is DataAttribute)
				{
					var data = attrib as DataAttribute;
					field.Data[data.Name] = data.Value;
				}
				else if (attrib is DatabaseFieldAttribute)
				{
					var dbField = attrib as DatabaseFieldAttribute;
					if (fieldType == typeof(string) && dbField.Length != 0)
					{
						// Set field max length:
						field.Data["maxlength"] = dbField.Length;
					}
				}
				else if (attrib is OrderAttribute)
                {
					var order = attrib as OrderAttribute;
					field.Order = order.Order;
                }
				else if (attrib is DividerAttribute)
				{
					field.Data["divider"] = true;
				}
				else if (attrib.GetType().ToString().Contains("PriceAttribute"))
                {
					field.Data["isPrice"] = true;
                }
			}

			if (labelName == "Name" && field.Order == uint.MaxValue)
            {
				field.Order = 0;
            }

			return field;
		}

		private static Regex SplitCamelCaseRegex = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

		/// <summary>
		/// Adds spaces to a CamelCase string (so it becomes "Camel Case")
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string SpaceCamelCase(string s)
		{
			return SplitCamelCaseRegex.Replace(s, " ");
		}

		/// <summary>
		/// Lowercases the first character of the given string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string FirstCharacterToLower(string str)
		{
			if (String.IsNullOrEmpty(str) || Char.IsLower(str, 0))
				return str;

			return Char.ToLowerInvariant(str[0]) + str.Substring(1);
		}

	}
    
}
