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
using Api.Contexts;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Startup;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Api.Startup.Routing;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Api.AvailableEndpoints
{
    /// <summary>
    /// This optional service is for self-documentation and automated testing.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    [HostType("web")]
    public partial class AvailableEndpointService : AutoService
	{
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public AvailableEndpointService()
        {
		}

		/// <summary>
		/// Gets the API structure, considering user permissions.
		/// </summary>
		/// <returns></returns>
		public ApiStructure GetStructure(Context context)
		{

			// Get the content types and their IDs:
			var cTypes = new List<ContentType>();

			foreach (var kvp in Database.ContentTypes.TypeMap)
			{
				cTypes.Add(new ContentType()
				{
					Name = kvp.Value.Name
				});
			}

			// The result object:
			var structure = new ApiStructure()
			{
				Endpoints = List(),
				ContentTypes = cTypes
			};

			return structure;
		}

		private XmlDoc _doc;

		/// <summary>
		/// Gets the documentation set.
		/// </summary>
		public XmlDoc Documentation {
			get {
				if (_doc == null)
				{
					var d = new XmlDoc();

					if (System.IO.File.Exists("SocialStack.Api.xml"))
					{
						d.LoadFrom("SocialStack.Api.xml");
					}

					_doc = d;
				}

				return _doc;
			}
		}

		private List<HttpMethodInfo> _builtInRoutes;

		/// <summary>
		/// Gets the set of built in routes.
		/// </summary>
		/// <returns></returns>
		public List<HttpMethodInfo> GetBuiltIn()
		{
			if (_builtInRoutes != null)
			{
				return _builtInRoutes;
			}

			var routes = new List<HttpMethodInfo>();

			// Locate all builtin AutoController objects
			var allTypes = typeof(RouterBuilder).Assembly.DefinedTypes;

			foreach (var type in allTypes)
			{
				if (!typeof(AutoController).IsAssignableFrom(type))
				{
					// It's not an autocontroller.
					continue;
				}

				if (type.IsGenericTypeDefinition || type == typeof(AutoController))
				{
					// Skip the underlying scaffolding types
					continue;
				}

				CollectRoutes(type, routes);
			}

			_builtInRoutes = routes;
			return _builtInRoutes;
		}

		/// <summary>
		/// Collects routes for the given controller type and adds them to the given set.
		/// </summary>
		/// <param name="controllerType"></param>
		/// <param name="routes"></param>
		public void CollectRoutes(Type controllerType, List<HttpMethodInfo> routes)
		{
			var controllerInfo = new ControllerInfo()
			{
				Type = controllerType
			};

			// Foreach method in the controller..
			var methods = controllerType.GetMethods();
			var baseRoute = controllerType.GetCustomAttribute<RouteAttribute>();

			foreach (var method in methods)
			{
				// For it to be an endpoint, it must have at least 1 HttpGet/ HttpPost/ HttpPut/ HttpDelete attribute.
				var methodAttribs = method.GetCustomAttributes();

				var routeSet = GetHttpRoutes(methodAttribs, baseRoute);

				if (routeSet == null || routeSet.Count == 0)
				{
					continue;
				}

				// Now got the routes and note that they don't start with a /.
				foreach (var route in routeSet)
				{
					routes.Add(
						new HttpMethodInfo()
						{
							Route = route.Route,
							Method = method,
							Controller = controllerInfo,
							Verb = route.Verb
						}
					);
				}
			}
		}

		private List<HttpMethodInfo> GetHttpRoutes(IEnumerable<Attribute> attribs, RouteAttribute baseRoute)
		{
			if (attribs == null)
			{
				return null;
			}

			List<HttpMethodInfo> result = null;
			var baseHasSlash = (baseRoute == null || baseRoute.Template == null) ? false : baseRoute.Template.EndsWith("/");

			foreach (var attr in attribs)
			{
				if (attr is HttpMethodAttribute)
				{
					if (result == null)
					{
						result = new List<HttpMethodInfo>();
					}

					var methodAttr = (HttpMethodAttribute)attr;

					foreach (var verb in methodAttr.HttpMethods)
					{
						var route = CombineRoutes(methodAttr.Template, baseHasSlash, baseRoute);

						result.Add(new HttpMethodInfo()
						{
							Verb = verb,
							Route = route.StartsWith("/") ? route.Substring(1) : route
						});
					}
				}
				else if (attr is RouteAttribute)
				{
					if (result == null)
					{
						result = new List<HttpMethodInfo>();
					}

					var route = CombineRoutes((attr as RouteAttribute).Template, baseHasSlash, baseRoute);

					result.Add(new HttpMethodInfo()
					{
						Verb = "GET",
						Route = route.StartsWith("/") ? route.Substring(1) : route
					});
				}
			}

			return result;
		}

		private string CombineRoutes(string route, bool baseHasSlash, RouteAttribute baseRoute)
		{

			if (route == null)
			{
				route = "";
			}

			if (baseRoute != null)
			{
				var routeHasSlash = route.StartsWith("/");

				if (baseHasSlash)
				{
					if (routeHasSlash)
					{
						// They both have one
						route = baseRoute.Template + route.Substring(1);
					}
					else
					{
						route = baseRoute.Template + route;
					}
				}
				else if (routeHasSlash)
				{
					route = baseRoute.Template + route;
				}
				else
				{
					route = baseRoute.Template + "/" + route;
				}
			}

			return route;
		}
		
		/// <summary>
		/// Obtains the set of all available endpoints, grouped by the module (controller) that they are from.
		/// </summary>
		/// <returns></returns>
		public List<ModuleEndpoints> ListByModule(bool staticOnly = true)
		{
			var result = new List<ModuleEndpoints>();

			var mapByControllerType = new Dictionary<Type, ModuleEndpoints>();

			var routes = GetBuiltIn();

			foreach(var route in routes) {
				var url = route.Route;
				var methodInfo = route.Method;

				if (methodInfo == null)
				{
					continue;
				}

				var httpVerb = route.Verb;
				var controllerType = route.Controller.Type;
					
				if (!mapByControllerType.TryGetValue(controllerType, out ModuleEndpoints module))
				{
					module = new ModuleEndpoints();
					module.ControllerType = controllerType;
					mapByControllerType[controllerType] = module;
					result.Add(module);
				}

				// httpMethod is e.g. "GET" or "POST". Uppercase it just in case.
				var ep = CreateEndpointInfo(url, httpVerb, methodInfo);
				module.Endpoints.Add(ep);
			}

			return result;
		}

		/// <summary>
		/// Obtains the set of all available endpoints in this API.
		/// </summary>
		/// <returns></returns>
		public List<Endpoint> List()
		{
			var byModule = ListByModule();

			var allEndpoints = new List<Endpoint>();

			foreach (var mod in byModule) {
				allEndpoints.AddRange(mod.Endpoints);
			}

			return allEndpoints;
		}

		private Endpoint CreateEndpointInfo(string url, string httpMethod, MethodInfo methodInfo)
		{
			var endpoint = new Endpoint()
			{
				Url = url,
				Method = methodInfo,
				HttpMethod = httpMethod
			};

			// Method info from the xmldoc document (if it's found).
			XmlDocMember member = null;

			if (Documentation != null)
			{
				var fullTypeName = methodInfo.DeclaringType.FullName;
				var documentedType = Documentation.GetType(fullTypeName, false);

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

	/// <summary>
	/// General information about the controller that an endpoint is a part of.
	/// </summary>
	public class ControllerInfo
	{
		/// <summary>
		/// The type.
		/// </summary>
		public Type Type;

		/// <summary>
		/// Controller instance.
		/// </summary>
		private object _instance;

		/// <summary>
		/// Gets a shared controller instance. 
		/// Only usable after all services have started.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public object GetInstance()
		{
			if (_instance != null)
			{
				return _instance;
			}

			// Instance the controller now, handling any injection necessary.
			object controllerInstance = null;

			var ctors = Type.GetConstructors();

			if (ctors.Length == 0)
			{
				controllerInstance = Activator.CreateInstance(Type);
			}
			else if (ctors.Length == 1)
			{
				var ctor = ctors[0];
				var paramTypes = ctor.GetParameters();
				var args = new object[paramTypes.Length];

				for (var i = 0; i < paramTypes.Length; i++)
				{
					// Ask the service engine to collect the service of the specified type.
					var pType = paramTypes[i].ParameterType;
					var svc = Services.GetByServiceType(pType);

					if (svc == null)
					{
						throw new Exception("Unable to locate service for type '" + pType.Name + "' in controller constructor " + Type.Name);
					}

					args[i] = svc;
				}

				controllerInstance = Activator.CreateInstance(Type, args);
			}
			else
			{
				throw new Exception("The controller '" + Type.Name + "' has more than one constructor. Either none or 1 is permitted.");
			}

			_instance = controllerInstance;
			return controllerInstance;
		}
	}

	/// <summary>
	/// Info about a http method route.
	/// </summary>
	public struct HttpMethodInfo
	{
		/// <summary>
		/// The http verb.
		/// </summary>
		public string Verb;

		/// <summary>
		/// The full absolute route minus the initial fwdslash.
		/// </summary>
		public string Route;

		/// <summary>
		/// The method.
		/// </summary>
		public MethodInfo Method;

		/// <summary>
		/// The controller.
		/// </summary>
		public ControllerInfo Controller;
	}
	
}
