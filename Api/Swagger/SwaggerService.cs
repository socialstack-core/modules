using Api.AvailableEndpoints;
using Api.Database;
using Api.Startup;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Stripe;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Swagger
{
    /// <summary>
    /// Service to assist with swagger content management
    /// </summary>
    [LoadPriority(101)]
    [HostType("web")]
    public partial class SwaggerService : AutoService
    {
        private SwaggerConfig _cfg;

        /// <summary>
        /// True if the service is active.
        /// </summary>
        private bool _isConfigured;

        private AvailableEndpointService _endpoints;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SwaggerService(AvailableEndpointService endpoints)
        {
            _endpoints = endpoints;
            _cfg = GetConfig<SwaggerConfig>();
            UpdateIsConfigured();

            _cfg.OnChange += () =>
            {
                UpdateIsConfigured();
                return new ValueTask();
            };
        }

        private Type ExpandReturnType(Type returnType)
        {
            if (returnType == null || returnType == typeof(ValueTask) || returnType == typeof(void))
            {
                return null;
            }

            var baseNullable = Nullable.GetUnderlyingType(returnType);

            if (baseNullable != null)
            {
                return ExpandReturnType(baseNullable);
            }

            if (returnType.IsGenericType)
            {
                var def = returnType.GetGenericTypeDefinition();

                if (def == typeof(ValueTask<>) || def == typeof(Task<>))
                {
                    var childType = returnType.GetGenericArguments()[0];
					return ExpandReturnType(childType);
                }
                else if (def == typeof(ContentStream<,>))
                {
                    var mainType = returnType.GetGenericArguments()[0];
                    return typeof(ApiList<>).MakeGenericType(mainType);
                }
            }

            if (IsContentType(returnType))
            {
                return typeof(ApiContent<>).MakeGenericType(returnType);
            }

            return returnType;
        }

        /// <summary>
        /// Same as the router: true if the given type is a content type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
		private bool IsContentType(Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Content<>))
			{
				return true;
			}

			var baseType = type.BaseType;

			if (baseType == null)
			{
				return false;
			}

			return IsContentType(baseType);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="swaggerDoc"></param>
		/// <param name="context"></param>
		public void FilterDocuments(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var allEndpoints = _endpoints.List();

            var sets = new Dictionary<string, SwaggerEndpointSet>();

			foreach (var epInfo in allEndpoints)
            {
                if (epInfo.Method == null)
                {
                    continue;
                }

                OperationType type;

                if (epInfo.HttpMethod == "GET")
                {
                    type = OperationType.Get;
                }
                else if (epInfo.HttpMethod == "POST")
                {
                    type = OperationType.Post;
                }
                else if (epInfo.HttpMethod == "DELETE")
                {
                    type = OperationType.Delete;
                }
                else if (epInfo.HttpMethod == "OPTIONS")
                {
                    type = OperationType.Options;
                }
                else if (epInfo.HttpMethod == "PUT")
                {
                    type = OperationType.Put;
                }
                else if (epInfo.HttpMethod == "HEAD")
                {
                    type = OperationType.Head;
                }
                else if (epInfo.HttpMethod == "PATCH")
                {
                    type = OperationType.Patch;
                }
                else if (epInfo.HttpMethod == "TRACE")
                {
                    type = OperationType.Trace;
                }
                else
                {
                    throw new Exception("Unrecognised http method: " + epInfo.HttpMethod);
                }

                var retType = ExpandReturnType(epInfo.Method.ReturnType);
				var schema = retType == null ? null : context.SchemaGenerator.GenerateSchema(retType, context.SchemaRepository);
                
				var op = new OpenApiOperation
                {
                    Summary = epInfo.Summary,
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Successful response",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = schema
                                }
                            }
                        }
                    }
                };

                if (!sets.TryGetValue(epInfo.Url, out SwaggerEndpointSet set))
                {
                    set = new SwaggerEndpointSet();
                    sets[epInfo.Url] = set;
				}

                set.Operations[type] = op;
            }

            foreach(var kvp in sets)
            {
				swaggerDoc.Paths.Add(kvp.Key, new OpenApiPathItem
				{
					Operations = kvp.Value.Operations
				});
			}

			// Get all controllers flagged with the internal attribute
			var excludedControllers = context.ApiDescriptions
                .Where(desc =>
                    desc.ActionDescriptor is ControllerActionDescriptor controllerDescriptor &&
                    controllerDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(InternalApiAttribute), true).Any())
                .Select(desc => desc.RelativePath)
                .Distinct()
                .ToList();

            foreach (var path in excludedControllers)
            {
                swaggerDoc.Paths.Remove("/" + path);
            }

            if (!_isConfigured)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_cfg.Title))
            {
                swaggerDoc.Info.Title = _cfg.Title;
            }

            if (!string.IsNullOrWhiteSpace(_cfg.Description))
            {
                swaggerDoc.Info.Description = _cfg.Description;
            }

            if (!string.IsNullOrWhiteSpace(_cfg.Version))
            {
                swaggerDoc.Info.Version = _cfg.Version;
            }

            if (_cfg.IncludedServices != null && _cfg.IncludedServices.Any())
            {
                var notIncludedServices = context.ApiDescriptions
                    .Where(desc =>
                        desc.ActionDescriptor is ControllerActionDescriptor controllerDescriptor &&
                        !string.IsNullOrEmpty(desc.ActionDescriptor.RouteValues["controller"]) &&
                        !_cfg.IncludedServices.Contains(desc.ActionDescriptor.RouteValues["controller"], StringComparer.OrdinalIgnoreCase))
                    .Select(desc => desc.RelativePath)
                    .Distinct()
                    .ToList();

                // Remove the paths from the Swagger document
                foreach (var path in notIncludedServices)
                {
                    swaggerDoc.Paths.Remove("/" + path);
                }
            }
            else if (_cfg.ExcludedServices != null && _cfg.ExcludedServices.Any())
            {
                var excludedServices = context.ApiDescriptions
                    .Where(desc =>
                        desc.ActionDescriptor is ControllerActionDescriptor controllerDescriptor &&
                        !string.IsNullOrEmpty(desc.ActionDescriptor.RouteValues["controller"]) &&
                        _cfg.ExcludedServices.Contains(desc.ActionDescriptor.RouteValues["controller"], StringComparer.OrdinalIgnoreCase))
                .Select(desc => desc.RelativePath)
                .Distinct()
                .ToList();

                // Remove the paths from the Swagger document
                foreach (var path in excludedServices)
                {
                    swaggerDoc.Paths.Remove("/" + path);
                }
            }

            // exclude any entries with specific Operations so POST, DELETE etc
            if (_cfg.ExcludedOperations != null && _cfg.ExcludedOperations.Any())
            {

                var _methodsToRemove = _cfg.ExcludedOperations
                    .Select(method => Enum.TryParse<OperationType>(method.Trim(), true, out var op) ? op : (OperationType?)null)
                    .Where(op => op.HasValue)
                    .Select(op => op.Value)
                    .ToArray();

                if (_methodsToRemove.Any())
                {
                    // Iterate through paths and operations to remove matching HTTP methods
                    foreach (var path in swaggerDoc.Paths.ToList())
                    {
                        foreach (var methodToRemove in _methodsToRemove)
                        {
                            if (path.Value.Operations.ContainsKey(methodToRemove))
                            {
                                // Remove the specific operation
                                path.Value.Operations.Remove(methodToRemove);
                            }
                        }

                        // If no operations remain in the path, remove the entire path
                        if (!path.Value.Operations.Any())
                        {
                            swaggerDoc.Paths.Remove(path.Key);
                        }
                    }
                }
            }

            // finally exclude any entries with specific suffixes so .csv .pot etc
            if (_cfg.ExcludedEndpointTypes != null && _cfg.ExcludedEndpointTypes.Any())
            {
                var pathsToRemove = swaggerDoc.Paths
                    .Where(p => _cfg.ExcludedEndpointTypes.Any(ep => p.Key.EndsWith("." + ep, StringComparison.OrdinalIgnoreCase)))
                    .Select(p => p.Key)
                    .ToList();

                foreach (var path in pathsToRemove)
                {
                    swaggerDoc.Paths.Remove(path);
                }
            }

            // Quicker to use the built in attribute 
            // [ApiExplorerSettings(IgnoreApi = true)]

            /*
            // Get all endpoints flagged with the attribute
            var endpointsToRemove = swaggerDoc.Paths
                        .Where(pathItem =>
                            context.ApiDescriptions.Any(desc =>
                                desc.RelativePath == pathItem.Key.TrimStart('/') &&
                                desc.ActionDescriptor is ControllerActionDescriptor controllerDescriptor &&
                                controllerDescriptor.MethodInfo.GetCustomAttributes(typeof(InternalApiAttribute), true).Any()
                            )
                        )
                        .Select(pathItem => pathItem.Key)
                        .ToList();

            foreach (var path in endpointsToRemove)
            {
                swaggerDoc.Paths.Remove(path);
            }
            */

            if (_cfg.ExcludeSchema)
            {
                // Remove the schemas section from components
                swaggerDoc.Components.Schemas.Clear();
            }
        }

        /// <summary>
        /// Gets the service config (readonly).
        /// </summary>
        /// <returns></returns>
        public SwaggerConfig GetConfiguration()
        {
            return _cfg;
        }


        /// <summary>
        /// True if this service is configured and active.
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            return _isConfigured;
        }

        /// <summary>
        /// Sets _isConfigured
        /// </summary>
        private void UpdateIsConfigured()
        {
            _isConfigured = !string.IsNullOrWhiteSpace(_cfg.Title)
                || !string.IsNullOrWhiteSpace(_cfg.Description)
                || !string.IsNullOrWhiteSpace(_cfg.Version)
                || _cfg.ExcludedOperations.Any()
                || _cfg.ExcludedEndpointTypes.Any()
                || _cfg.IncludedServices.Any()
                || _cfg.ExcludedServices.Any();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetAssemblyAttribute<T>() where T : Attribute
        {
            var thisAsm = typeof(EventListener).Assembly;

            object[] attributes = thisAsm.GetCustomAttributes(typeof(T), false);

            if (attributes.Length == 0)
                return null;

            return attributes.OfType<T>().SingleOrDefault();
        }

    }

    internal class SwaggerEndpointSet {
        public Dictionary<OperationType, OpenApiOperation> Operations = new Dictionary<OperationType, OpenApiOperation>();
	}
}

