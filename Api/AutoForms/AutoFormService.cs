using Api.Contexts;
using Api.CustomContentTypes;
using Api.Database;
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
	public partial class AutoFormService
	{
		private IActionDescriptorCollectionProvider _descriptionProvider;
		private RoleService _roleService;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AutoFormService(IActionDescriptorCollectionProvider descriptionProvider, RoleService roleService)
		{
			_descriptionProvider = descriptionProvider;
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
				yield return new ContentType()
				{
					Id = kvp.Value.Id,
					Name = kvp.Value.Name
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
			// Try getting the revision service to see if they're supported:
			var revisionsSupported = Services.Get("RevisionService") != null;

			// For each AutoService..
			foreach (var serviceKvp in Services.AutoServices)
			{
				if (serviceKvp.Value.IsMapping)
				{
					// Omit mapping services
					continue;
				}

				var fieldStructure = await serviceKvp.Value.GetJsonStructure(context);

				var formType = serviceKvp.Value.InstanceType;
				var formMeta = GetFormInfo(fieldStructure);

				// Must inherit revisionRow and 
				// the revision module must be installed
				formMeta.SupportsRevisions = revisionsSupported && Api.Database.ContentTypes.IsAssignableToGenericType(serviceKvp.Value.InstanceType, typeof(VersionedContent<>));

				var name = formType.Name.ToLower();
				formMeta.Endpoint = "v1/" + name;
				cache[name] = formMeta;
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
		/// <returns></returns>
		public AutoFormInfo GetFormInfo(JsonStructure jsonStructure)
		{
			var info = new AutoFormInfo
			{
				Fields = new List<AutoFormField>()
			};

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

			var field = new AutoFormField()
			{
				Includable = isIncludable,
				ValueType = valueType,
				Module = jsonField.Module,
				Data = jsonField.Data
			};

			var type = "text";
			var name = jsonField.OriginalName;
			var labelName = name;

			// If the field is a string and ends with Json, it's canvas:
			if (fieldType == typeof(string) && labelName.EndsWith("Json"))
			{
				type = "canvas";

				// Remove "Json" from the end of the label:
				labelName = labelName.Substring(0, labelName.Length - 4);
			}
			else if (fieldType == typeof(string) && labelName.EndsWith("Ref"))
			{
				type = "image";

				// Remove "Ref" from the end of the label:
				labelName = labelName.Substring(0, labelName.Length - 3);
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
				else if (attrib is LocalizedAttribute)
				{
					// Yep - it's translatable.
					field.Data["localized"] = true;
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
