using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Api.AutoForms
{
	/// <summary>
	/// This service drives AutoForm - the form which automatically displays fields in the admin area.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>

	public partial class AutoFormService : IAutoFormService
	{
		private List<AutoFormInfo> _cachedList;
		private IActionDescriptorCollectionProvider _descriptionProvider;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AutoFormService(IActionDescriptorCollectionProvider descriptionProvider)
        {
			_descriptionProvider = descriptionProvider;
		}

		/// <summary>
		/// Obtains the set of all available endpoints in this API.
		/// </summary>
		/// <returns></returns>
		public List<AutoFormInfo> List()
		{
			if (_cachedList != null)
			{
				// return _cachedList;
			}
			
			var result = new List<AutoFormInfo>();
			_cachedList = result;
			
			foreach (var actionDescription in _descriptionProvider.ActionDescriptors.Items)
			{
				var cad = actionDescription as ControllerActionDescriptor;

				if (cad == null)
				{
					// We're only after controller descriptors here.
					continue;
				}

				var url = cad.AttributeRouteInfo.Template;
				var methodInfo = cad.MethodInfo;

				if (cad.ActionConstraints == null)
				{
					continue;
				}
				
				// Must have a FromBody of an AutoForm derived type.
				var parameters = methodInfo.GetParameters();

				if (parameters.Length == 0)
				{
					continue;
				}
					
				for (var i = 0; i < parameters.Length; i++)
				{
					var methodParam = parameters[i];

					if (methodParam.GetCustomAttribute(typeof(FromBodyAttribute)) != null)
					{
						var type = methodParam.ParameterType;

						if (type.BaseType == null || !type.BaseType.IsConstructedGenericType)
						{
							continue;
						}

						if (type.BaseType.GetGenericTypeDefinition() != typeof(AutoForm<>))
						{
							continue;
						}

						// Method MUST be called Create otherwise we ignore it.
						// This blocks duplicate Update endpoints with /{id} on the end of the URL.
						if (methodInfo.Name != "Create")
						{
							continue;
						}

						var formMeta = GetFormInfo(type);
						formMeta.Endpoint = url;

						result.Add(formMeta);
					}
				}
				
			}
			
			return result;
		}

		/// <summary>
		/// Gets the AutoForm info such as fields available for the given :AutoForm type.
		/// </summary>
		/// <param name="autoFormType"></param>
		/// <returns></returns>
		public AutoFormInfo GetFormInfo(Type autoFormType)
		{
			var info = new AutoFormInfo();
			info.Fields = new List<AutoFormField>();

			// Get the raw fields:
			var fieldInfoSet = autoFormType.GetFields(BindingFlags.Public | BindingFlags.Instance);

			for (var i = 0; i < fieldInfoSet.Length; i++)
			{
				var fieldInfo = fieldInfoSet[i];

				if (
					fieldInfo.DeclaringType == typeof(AutoForm) || 
					(fieldInfo.DeclaringType.IsConstructedGenericType && fieldInfo.DeclaringType.GetGenericTypeDefinition() == typeof(AutoForm<>))
				)
				{
					// Skip internal autoform fields
					continue;
				}

				var customAttributes = fieldInfo.GetCustomAttributes();

				var fieldType = fieldInfo.FieldType;

				var field = new AutoFormField()
				{
					ValueType = fieldType.Name,
					Module = "UI/Input",
					Data = new Dictionary<string, object>()
				};

				var type = "text";
				var name = fieldInfo.Name;
				var labelName = name;

				// If the field is a string and ends with Json, it's canvas:
				if (fieldType == typeof(string) && labelName.EndsWith("Json"))
				{
					type = "canvas";

					// Remove "Json" from the end of the label:
					labelName = labelName.Substring(0, labelName.Length - 4);
				}
				else if(fieldType == typeof(int) && labelName.EndsWith("PageId"))
				{
					field.Module = "Admin/Page/Select";
					
					// Remove "Id" from the end of the label:
					labelName = labelName.Substring(0, labelName.Length - 2);
				}
				else if (fieldType == typeof(bool))
				{
					type = "checkbox";
				}

				field.Data["label"] = SpaceCamelCase(labelName);
				field.Data["name"] = FirstCharacterToLower(name);
				field.Data["type"] = type;

				// Any of these [Module] or inheritors?
				foreach (var attrib in customAttributes)
				{
					if (attrib is ModuleAttribute)
					{
						var module = attrib as ModuleAttribute;
						field.Module = module.Name;
					}
					else if (attrib is DataAttribute)
					{
						var data = attrib as DataAttribute;
						field.Data[data.Name] = data.Value;
					}
				}
				
				info.Fields.Add(field);

			}

			return info;
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
