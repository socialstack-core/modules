using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Api.TypeScript.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Generates a TypeScript API class for a specific entity/controller pair.
    /// </summary>
    /// <remarks>
    /// This controller generator builds a concrete TypeScript API class based on a server-side controller
    /// and entity type. It ensures required API modules are registered and produces methods that match
    /// the .NET controller's endpoints.
    ///
    /// The resulting class extends <c>AutoController&lt;T, ID&gt;</c> and provides a typed frontend interface
    /// with IntelliSense and route bindings.
    /// </remarks>
    public partial class EntityController : MethodCollector
    {
        private readonly (Type controllerType, Type entityType) _referenceTypes;
        private readonly ESModule _container;
        
        /// <summary>
        /// The entity type this controller references.
        /// </summary>
        public Type EntityType => _referenceTypes.entityType;

        /// <summary>
        /// Constructs an <see cref="EntityController"/> code generator instance.
        /// </summary>
        /// <param name="controllerType">The .NET controller type (e.g. <c>ArticlesController</c>).</param>
        /// <param name="entityType">The associated entity type (e.g. <c>Article</c>).</param>
        /// <param name="container">The ES module container to register generated types and dependencies with.</param>
        /// <remarks>
        /// This constructor registers the target entity type and collects required dependencies such as
        /// other return types and Web API methods. It scans controller method signatures to prepare metadata
        /// for TypeScript output.
        /// </remarks>
        public EntityController(Type controllerType, Type entityType, ESModule container)
        {
            container.AddType(entityType);
            container.MarkAsEntityModule();

            _container = container;
            _referenceTypes = (controllerType, entityType);
            _container.SetEntityController(this);
            _container.RequireWebApi(WebApis.GetJson);
            _container.RequireWebApi(WebApis.GetList);
            _container.RequireWebApi(WebApis.GetOne);
            _container.RequireWebApi(WebApis.GetText);

            foreach (var method in GetEndpointMethods(controllerType))
            {
                if (method.ReturnType != _referenceTypes.entityType && !TypeScriptService.IsEntityType(method.ReturnType))
                {
                    container.AddType(method.ReturnType);
                }
                
                TypeScriptService.EnsureTypeCreation(method.WebSafeParams.Select(param => param.ParameterType).ToList(), container);
                TypeScriptService.EnsureApis(method, container, _referenceTypes.entityType);
            }
        }

        /// <summary>
        /// Emits the TypeScript source code for the generated API class for the given controller/entity pair.
        /// </summary>
        /// <param name="builder">A <see cref="StringBuilder"/> to receive the generated TypeScript source.</param>
        /// <param name="svc">The <see cref="TypeScriptService"/> used for formatting types and identifiers.</param>
        /// <remarks>
        /// Outputs a TypeScript class that extends <c>AutoController&lt;T, ID&gt;</c> with route-specific methods.
        /// Each method includes appropriate return types, parameters, and request bindings (e.g., <c>getJson</c>,
        /// <c>getList</c>, <c>getOne</c>).
        /// </remarks>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            builder.AppendLine();
            builder.AppendLine($"export class {svc.GetGenericSignature(_referenceTypes.entityType)}Api extends AutoController<{svc.GetGenericSignature(_referenceTypes.entityType)},uint, {svc.GetGenericSignature(_referenceTypes.entityType)}Includes>{{");

            var baseUrlRoute = _referenceTypes.controllerType.GetCustomAttribute<RouteAttribute>();
            var baseUrl = baseUrlRoute is not null ? baseUrlRoute.Template : "";

            baseUrl = baseUrl.ToLower();

            builder.AppendLine();
            builder.AppendLine("    constructor(){");
            builder.AppendLine($"        super('/{baseUrl}', new {svc.GetGenericSignature(_referenceTypes.entityType)}Includes());");
            builder.AppendLine("    }");
            builder.AppendLine();
            
            EndpointMethodToTypeScript.ConvertEndpointToTypeScript(_referenceTypes.controllerType, this, builder,  svc);

            builder.AppendLine("}");
            builder.AppendLine();

            builder.AppendLine($"export default new {svc.GetGenericSignature(_referenceTypes.entityType)}Api();");
        }
    }
}
