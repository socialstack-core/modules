using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.Startup;
using Api.TypeScript.Contracts;

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
    public partial class NonEntityController : MethodCollector
    {
        private readonly Type _referenceType;
        private readonly ESModule _container;

        private readonly List<Type> _requiredImports = [];

        /// <summary>
        /// Constructs an <see cref="EntityController"/> code generator instance.
        /// </summary>
        /// <param name="controllerType">The .NET controller type (e.g. <c>ArticlesController</c>).</param>
        /// <param name="container">The ES module container to register generated types and dependencies with.</param>
        /// <param name="includes"></param>
        /// <remarks>
        /// This constructor registers the target entity type and collects required dependencies such as
        /// other return types and Web API methods. It scans controller method signatures to prepare metadata
        /// for TypeScript output.
        /// </remarks>
        public NonEntityController(Type controllerType, ESModule container, ESModule includes) : base(container)
		{

            _container = container;
            _referenceType = controllerType;
            container.RequireWebApi(WebApis.GetList);

            foreach (var method in GetEndpointMethods(controllerType))
            {
                TypeScriptService.EnsureApis(method, container, null);
                TypeScriptService.EnsureTypeCreation(
                    method.WebSafeParams
                        .Where(param => !TypeScriptService.IsEntityType(param.ParameterType))
                        .Select(param => param.ParameterType).ToList()
                , container);

                var type = method.ReturnType;

                if (type is not null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ContentStream<,>))
                {
                    type = type.GetGenericArguments()[0];
                }

                type ??= method.ReturnType;
                
                if (container.HasTypeDefinition(type, out _))
                {
                    continue;
                }
                if (TypeScriptService.IsEntityType(type))
                {
                    // import instead
                    _requiredImports.Add(type);
                    method.RequiresIncludes = true;
                    container.Import("ApiIncludes", includes);
                }
                else
                {
                    TypeScriptService.EnsureTypeCreation([type], container);
                };
            }
        }

        /// <summary>
        /// Gets the set of imports required by this controller
        /// </summary>
        /// <returns></returns>
        public List<Type> GetRequiredImports()
        {
            return _requiredImports;
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
            builder.AppendLine($"export class {svc.GetGenericSignature(_referenceType)} {{");

            var baseUrlRoute = _referenceType.GetCustomAttribute<RouteAttribute>();
            var baseUrl = baseUrlRoute is not null ? baseUrlRoute.Template : "";

            builder.AppendLine();
            builder.AppendLine($"   private apiUrl: string = '/{baseUrl}';");
            builder.AppendLine();
            
            
            EndpointMethodToTypeScript.ConvertEndpointToTypeScript(GetType(), this, builder, svc);

            // Generate a method for each endpoint
            // foreach (var method in GetEndpointMethods())
            // {
            //     
            //
            //     string url = URLBuilder.BuildUrl(method);
            //
            //     var isArrayType = method.IsApiList;
            //
            //     builder.AppendLine("    /**");
            //     builder.AppendLine("     * Generated from a .NET type.");
            //     builder.AppendLine($"     * @see {{{_referenceType}}}::{{{method.Method.Name}}}");
            //     builder.AppendLine($"     * @url '{url}'");
            //     builder.AppendLine($"     * @trueReturnType {method.TrueReturnType.Name}");
            //     builder.AppendLine($"     * @passedReturnType {method.ReturnType.Name}");
            //     builder.AppendLine("     */");
            //     builder.Append($"    {TypeScriptService.LcFirst(method.Method.Name)} = (");
            //
            //     int paramCount = 0;
            //     if (method.RequiresSessionSet)
            //     {
            //         builder.Append("setSession: (s: SessionResponse) => Session");
            //         paramCount++;
            //     }
            //
            //     foreach (var param in method.WebSafeParams)
            //     {
            //         if (paramCount > 0) builder.Append(", ");
            //
            //         string paramType = param.ParameterType == typeof(JObject)
            //             ? "Record<string, any>"
            //             : svc.GetGenericSignature(param.ParameterType);
            //
            //         builder.Append($"{param.Name}{(param.IsOptional ? "?" : "")}: {paramType}");
            //         paramCount++;
            //     }
            //
            //     var isContentStream = false;
            //     var type = TypeScriptService.UnwrapTypeNesting(method.ReturnType);
            //
            //     if (method.RequiresIncludes)
            //     {
            //         if (paramCount != 0)
            //         {
            //             builder.Append(", ");
            //         }
            //
            //
            //         if (type is not null && type.IsGenericType &&
            //             type.GetGenericTypeDefinition() == typeof(ContentStream<,>))
            //         {
            //             type = type.GetGenericArguments()[0];
            //             isContentStream = true;
            //         }
            //
            //         type ??= method.TrueReturnType;
            //         
            //         builder.Append($"includes?: ApiIncludes[]");
            //         
            //     }
            //
            //     // Closing params and declaring return type
            //     string call = "getJson";
            //
            //     if (isArrayType)
            //     {
            //         if (TypeScriptService.IsEntityType(method.ReturnType))
            //         {
            //             call = $"getList<{svc.GetGenericSignature(method.ReturnType)}>";
            //             builder.Append($"): Promise<{svc.GetGenericSignature(method.ReturnType)}> => {{");
            //         }
            //         else
            //         {
            //             call = $"getJson<{svc.GetGenericSignature(method.ReturnType)}[]>";
            //             builder.Append($"): Promise<{svc.GetGenericSignature(method.ReturnType)}[]> => {{");
            //         }
            //     }
            //     else if (isContentStream)
            //     {
            //         call = $"getList<{type.Name}>";
            //         builder.Append($"): Promise<ApiList<{type.Name}>> => {{");
            //     }
            //     else
            //     {
            //         if (method.RequiresSessionSet)
            //         {
            //             call = "getJson<SessionResponse>";
            //             _container.RequireWebApi(WebApis.GetJson);
            //             builder.Append($"): Promise<Session> => {{");
            //         }
            //         else if (TypeScriptService.IsEntityType(method.ReturnType))
            //         {
            //             call = $"getOne<{svc.GetGenericSignature(method.ReturnType)}>";
            //             builder.Append($"): Promise<{svc.GetGenericSignature(method.ReturnType)}> => {{");
            //         }
            //         else if (method.ReturnType.IsGenericTypeDefinition &&
            //                  method.ReturnType.GetGenericTypeDefinition() == typeof(ContentStream<,>))
            //         {
            //             call = $"getList<{svc.GetGenericSignature(method.ReturnType)}>";
            //             builder.Append($"): Promise<ApiList<{svc.GetGenericSignature(method.ReturnType)}>> => {{");
            //         }
            //         else if (method.ReturnType == typeof(ValueTask) || method.ReturnType == typeof(void))
            //         {
            //             call = $"getText";
            //             builder.Append($"): Promise<string> => {{");
            //         }
            //         else
            //         {
            //             call = $"getJson<{svc.GetGenericSignature(method.ReturnType)}>";
            //             builder.Append($"): Promise<{svc.GetGenericSignature(method.ReturnType)}> => {{");
            //         }
            //     }
            //
            //     builder.AppendLine();
            //     
            //     builder.AppendLine($"        return {call}(this.apiUrl + '{url}'{(method.SendsData ? $", {method.BodyParam.Name}" : "")})");
            //     
            //     if (method.RequiresSessionSet)
            //     {
            //         builder.AppendLine("            .then(setSession)");
            //     }
            //
            //
            //     builder.AppendLine("    }");
            //     builder.AppendLine();
            // }
            //
            builder.AppendLine("}");
            builder.AppendLine();

            builder.AppendLine($"export default new {svc.GetGenericSignature(_referenceType)}();");
        }
    }
}
