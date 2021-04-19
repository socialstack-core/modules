using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
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
		}

		private List<AutoFormInfo>[] _cachedAutoFormInfo = null;

		/// <summary>
		/// List of autoform info for the given role.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<List<AutoFormInfo>> List(Context context)
		{
			var roleId = context.RoleId;
			var role = await _roleService.Get(context, roleId);

			if (role == null)
			{
				// Bad role ID.
				return null;
			}

			if (_cachedAutoFormInfo == null)
			{
				_cachedAutoFormInfo = new List<AutoFormInfo>[role.Id];
			}
			else if (_cachedAutoFormInfo.Length < role.Id)
			{
				// Resize it:
				Array.Resize(ref _cachedAutoFormInfo, (int)role.Id);
			}
			
			var cachedList = _cachedAutoFormInfo[roleId - 1];

			if (cachedList != null)
			{
				return cachedList;
			}

			var result = new List<AutoFormInfo>();
			_cachedAutoFormInfo[role.Id - 1] = result;
			
			// Try getting the revision service to see if they're supported:
			var revisionsSupported = Services.Get("RevisionService") != null;
			
			// For each AutoService..
			foreach (var serviceKvp in Services.AutoServices)
			{
				var fieldStructure = await serviceKvp.Value.GetJsonStructure(context);

				var formType = serviceKvp.Value.InstanceType;
				var formMeta = GetFormInfo(fieldStructure);
				
				// Must inherit revisionRow and 
				// the revision module must be installed
				formMeta.SupportsRevisions = revisionsSupported && Api.Database.ContentTypes.IsAssignableToGenericType(serviceKvp.Value.InstanceType, typeof(VersionedContent<>));
				
				formMeta.Endpoint = "v1/" + formType.Name.ToLower();
				result.Add(formMeta);
			}
			
			return result;
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

			var field = new AutoFormField()
			{
				ValueType = fieldType.Name,
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
			else if (fieldType == typeof(int) && labelName.EndsWith("UserId"))
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
