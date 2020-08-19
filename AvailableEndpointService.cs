using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
#if NETCOREAPP2_1 || NETCOREAPP2_2
using Microsoft.AspNetCore.Mvc.Internal;
#else
using Microsoft.AspNetCore.Mvc.ActionConstraints;
#endif
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Api.AvailableEndpoints
{
	/// <summary>
	/// This optional service is for self-documentation and automated testing.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>

	public partial class AvailableEndpointService : IAvailableEndpointService
	{
		private List<Endpoint> _cachedList;
		private IActionDescriptorCollectionProvider _descriptionProvider;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AvailableEndpointService(IActionDescriptorCollectionProvider descriptionProvider)
        {
			_descriptionProvider = descriptionProvider;

		}

		/// <summary>
		/// Obtains the set of all available endpoints in this API.
		/// </summary>
		/// <returns></returns>
		public List<Endpoint> List()
		{
			if (_cachedList != null)
			{
				return _cachedList;
			}

			// Got the Api.xml file?
			var xmlDocFile = "Api.xml";
			XmlDoc docs = null;

			if (System.IO.File.Exists(xmlDocFile))
			{
				docs = new XmlDoc();
				docs.LoadFrom(xmlDocFile);
			}

			var result = new List<Endpoint>();
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

				foreach (var constraint in actionDescription.ActionConstraints)
				{
					var methodConstraint = constraint as HttpMethodActionConstraint;

					if (methodConstraint == null || methodConstraint.HttpMethods == null)
					{
						continue;
					}

					// Potentially multiple http methods on the same endpoint.
					// We treat each as a unique EP.
					foreach (var httpMethod in methodConstraint.HttpMethods)
					{
						// httpMethod is e.g. "GET" or "POST". Uppercase it just in case.
						var ep = CreateEndpointInfo(url, httpMethod.ToUpper(), methodInfo, docs);
						result.Add(ep);
					}
				}
			}
			
			return result;
		}

		private Endpoint CreateEndpointInfo(string url, string httpMethod, MethodInfo methodInfo, XmlDoc docs = null)
		{
			var endpoint = new Endpoint()
			{
				Url = url,
				HttpMethod = httpMethod
			};

			// Method info from the xmldoc document (if it's found).
			XmlDocMember member = null;

			if (docs != null)
			{
				var fullTypeName = methodInfo.DeclaringType.FullName;
				var documentedType = docs.GetType(fullTypeName, false);

				if (documentedType != null)
				{
					// Get the method:
					member = documentedType.GetMember(methodInfo.Name);

					if (member != null)
					{
						endpoint.Summary = member.Summary?.Trim();
					}
				}

			}

			var returnType = methodInfo.ReturnType;
			var methodParams = methodInfo.GetParameters();

			// If any method param has [FromRoute] then add to UrlFields.
			// If any method param has [FromBody] then add the objects fields to BodyFields.

			for (var i = 0; i < methodParams.Length; i++)
			{
				var methodParam = methodParams[i];

				if (methodParam.GetCustomAttribute(typeof(FromRouteAttribute)) != null) {

					// Got a UrlField:
					if (endpoint.UrlFields == null)
					{
						endpoint.UrlFields = new Dictionary<string, object>();
					}

					XmlDocMember paramInfo = null;

					if (member != null)
					{
						paramInfo = member.GetParameter(methodParam.Name);
					}

					endpoint.UrlFields[methodParam.Name] = GetFieldTypeInfo(methodParam.ParameterType, paramInfo);
				}
				else if (methodParam.GetCustomAttribute(typeof(FromBodyAttribute)) != null)
				{
					// Got a body:
					if (endpoint.BodyFields == null)
					{
						endpoint.BodyFields = new Dictionary<string, object>();
					}

					// Special case if this is a JObject:
					if (methodParam.ParameterType == typeof(JObject))
					{
						// If the parent type of the method is an AutoController, grab its base type and use that instead:
						if (methodInfo.DeclaringType.IsGenericType)
						{
							var genericDef = methodInfo.DeclaringType.GetGenericTypeDefinition();

							if (genericDef == typeof(AutoController<>))
							{
								var genericTypes = methodInfo.DeclaringType.GetGenericArguments();
								BuildBodyFields(genericTypes[0], endpoint.BodyFields);
							}
						}
					}
					else
					{
						BuildBodyFields(methodParam.ParameterType, endpoint.BodyFields);
					}

				}

			}

			return endpoint;
		}

		/// <summary>
		/// Gets general field info for a field of a given type.
		/// </summary>
		/// <param name="typeInfo"></param>
		/// <param name="documentation"></param>
		/// <returns></returns>
		private object GetFieldTypeInfo(System.Type typeInfo, XmlDocMember documentation = null)
		{
			var underlyingType = Nullable.GetUnderlyingType(typeInfo);

			var summary = documentation?.Summary?.Trim();

			// Future: handle arrays and sub-objects.

			if (underlyingType != null)
			{
				return new {
					summary,
					optional = true,
					type = underlyingType.Name
				};
			}

			if (typeInfo.IsValueType)
			{
				return new
				{
					summary,
					type = typeInfo.Name
				};
			}

			return new
			{
				summary,
				optional = true,
				type = typeInfo.Name
			};
		}

		/// <summary>
		/// Builds out the bodyfields set using fields in the given type.
		/// </summary>
		/// <param name="paramType"></param>
		/// <param name="fields"></param>
		private void BuildBodyFields(System.Type paramType, Dictionary<string, object> fields)
		{
			var props = paramType.GetProperties();
			var fieldSet = paramType.GetFields();

			foreach (var field in fieldSet)
			{
				if (field.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
				{
					continue;
				}

				var jsonAttrib = field.GetCustomAttribute(typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;
				var name = field.Name;

				if (jsonAttrib != null)
				{
					if (jsonAttrib.PropertyName != null)
					{
						name = jsonAttrib.PropertyName;
					}
				}

				fields[name] = GetFieldTypeInfo(field.FieldType);
			}

			foreach (var prop in props)
			{
				if (!prop.CanWrite)
				{
					continue;
				}

				if (prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
				{
					continue;
				}

				var jsonAttrib = prop.GetCustomAttribute(typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;
				var name = prop.Name;

				if (jsonAttrib != null)
				{
					if (jsonAttrib.PropertyName != null)
					{
						name = jsonAttrib.PropertyName;
					}
				}



				fields[name] = GetFieldTypeInfo(prop.PropertyType);
			}
		}

	}
    
}
