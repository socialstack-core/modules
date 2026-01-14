using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Startup;
using Api.TypeScript.Contracts;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Generates a TypeScript controller class that mirrors an auto-generated .NET controller.
    /// </summary>
    /// <remarks>
    /// This class dynamically generates TypeScript code for an <c>AutoController</c> that includes methods
    /// corresponding to server-side endpoints.
    /// <para>
    /// Each endpoint method reflects return types and parameters and handles serialization of session state,
    /// data payloads, and includes (expansion of nested fields).
    /// </para>
    /// The resulting TypeScript controller exposes a familiar structure to frontend developers consuming the API.
    /// </remarks>
    public partial class AutoController : MethodCollector
    {
        private ESModule _container;

        /// <summary>
        /// Constructs a new <see cref="AutoController"/> instance, ensuring all required Web API modules
        /// are registered in the container.
        /// </summary>
        /// <param name="container">
        /// The <see cref="ESModule"/> container responsible for aggregating all generated TypeScript components.
        /// </param>
        /// <remarks>
        /// This constructor scans endpoint methods for required Web API types and registers them with the container.
        /// </remarks>
        public AutoController(ESModule container)
        {
            // Ensure the generic controller type exists.
            // container.AddType(typeof(AutoController<,>));
            _container = container;

            // Scan for used WebApis and ensure they're available.
            foreach (var method in GetEndpointMethods(typeof(AutoController<,>)))
            {
                TypeScriptService.EnsureApis(method, container, null);
                TypeScriptService.EnsureTypeCreation(method.WebSafeParams.Select(param => param.ParameterType).ToList(), container);
                TypeScriptService.EnsureTypeCreation([method.ReturnType], container);
            }
        }

        /// <summary>
        /// Appends the generated TypeScript class for the controller, including endpoint methods.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append generated TypeScript source code to.</param>
        /// <param name="svc">A <see cref="TypeScriptService"/> that provides type formatting and naming utilities.</param>
        /// <remarks>
        /// Generates a generic <c>AutoController&lt;T, ID&gt;</c> TypeScript class with methods mapped from backend endpoints.
        /// Each method supports session binding, request parameters, and optional includes for field expansion.
        /// </remarks>
        public override void ToSource(StringBuilder builder, TypeScriptService svc)
        {
            builder.AppendLine();
            builder.AppendLine("export class AutoController<T extends Content<uint>, ID, Includes extends ApiIncludes> {");

            builder.AppendLine();
            builder.AppendLine("    protected apiUrl: string;");
            builder.AppendLine();
            builder.AppendLine("    public includes: Includes;");

            builder.AppendLine("    constructor(baseUrl: string = '', includes: Includes) {");
            builder.AppendLine("        this.apiUrl = baseUrl?.toLowerCase();");
            builder.AppendLine("        this.includes = includes");
            builder.AppendLine("    }");

            EndpointMethodToTypeScript.ConvertEndpointToTypeScript(typeof(AutoController<,>), this, builder, svc);
            
            // Type[] getTextTypes = [typeof(void), typeof(ValueTask), typeof(object), typeof(FileContent)];

            // Generate methods for each controller endpoint
            // foreach (var method in GetEndpointMethods())
            // {
            //     var isArrayType = method.IsApiList;
            //
            //     builder.AppendLine();
            //     builder.AppendLine("    /**");
            //     builder.AppendLine("     * Generated from a .NET type.");
            //     builder.AppendLine($"     * @see {{T}}::{{{method.Method.Name}}}");
            //     builder.AppendLine($"     * @url '{method.RequestUrl}'");
            //     builder.AppendLine($"     * @trueReturnType {method.TrueReturnType.Name}");
            //     builder.AppendLine($"     * @passedReturnType {method.ReturnType.Name}");
            //     builder.AppendLine("     */");
            //     builder.Append($"    {TypeScriptService.LcFirst(method.Method.Name)} = (");
            //
            //     // Parameter list generation
            //     int paramCount = 0;
            //     if (method.RequiresSessionSet)
            //     {
            //         builder.Append("setSession: (s: SessionResponse) => Session");
            //         paramCount++;
            //     }
            //
            //     foreach (var param in method.WebSafeParams)
            //     {
            //         if (paramCount > 0)
            //         {
            //             builder.Append(", ");
            //         }
            //
            //         string typeName = param.ParameterType == typeof(JObject)
            //             ? "Partial<T>"
            //             : svc.GetGenericSignature(param.ParameterType);
            //
            //         if (TypeScriptService.IsEntityType(param.ParameterType))
            //         {
            //             typeName = "Partial<" + typeName + ">";
            //         }
            //
            //         builder.Append($"{param.Name}{(TypeScriptService.IsNullable(param.ParameterType) ? "?" : "")}: {typeName}");
            //         paramCount++;
            //     }
            //
            //     if (method.RequiresIncludes)
            //     {
            //         if (paramCount > 0) builder.Append(", ");
            //         builder.Append("includes?: ApiIncludes[]");
            //     }
            //
            //     // Method return signature and implementation
            //     string call = "getText";
            //     string returnType = "void";
            //
            //     if (isArrayType)
            //     {
            //         if (method.ReturnType.IsGenericType)
            //         {
            //             if (method.ReturnType is { IsGenericParameter: true })
            //             {
            //                 // It's a generic type like ApiList<T> where T is just a type parameter
            //                 call = $"getList<{svc.GetGenericSignature(method.ReturnType)}>";
            //                 returnType = $"Promise<ApiList<{method.ReturnType.Name}>>";
            //                 _container.RequireWebApi(WebApis.GetList);
            //             }
            //         }
            //         else
            //         {
            //             call = $"getList<{svc.GetGenericSignature(method.ReturnType)}>";
            //             returnType = $"Promise<ApiList<{svc.GetGenericSignature(method.ReturnType)}>>";
            //             _container.RequireWebApi(WebApis.GetList);
            //         }
            //     }
            //     else
            //     {
            //         if (method.RequiresSessionSet)
            //         {
            //             call = "getJson<SessionResponse>";
            //             _container.RequireWebApi(WebApis.GetJson);
            //             builder.Append($"): Promise<Session> => {{");
            //         }
            // else if (IsSingularContentStream(method.ReturnType))
            //         {
            //             call = "getList<T>";
            //             returnType = "Promise<ApiList<T>>";
            //             _container.RequireWebApi(WebApis.GetList);
            //         }
            //         else if (TypeScriptService.IsEntityType(method.ReturnType))
            //         {
            //             call = $"getOne<{svc.GetGenericSignature(method.ReturnType)}>";
            //             returnType = $"Promise<{svc.GetGenericSignature(method.ReturnType)}>";
            //             _container.RequireWebApi(WebApis.GetOne);
            //         }
            //         else if (getTextTypes.Contains(method.ReturnType))
            //         {
            //             call = $"getText";
            //             returnType = "Promise<string>";
            //             _container.RequireWebApi(WebApis.GetText);
            //         }
            //         else
            //         {
            //             call = $"getJson<{svc.GetGenericSignature(method.ReturnType)}>";
            //             returnType = $"Promise<{svc.GetGenericSignature(method.ReturnType)}>";
            //             _container.RequireWebApi(WebApis.GetJson);
            //         }
            //     }
            //
            //     builder.Append($"): {returnType} => {{\n");
            //     
            //     // v1/productCategory => v1/productcategory
            //     string url = URLBuilder.BuildUrl(method);
            //
            //     var reqMethodModify = "";
            //
            //     if (method.Method.GetCustomAttribute<HttpDeleteAttribute>() is not null)
            //     {
            //         reqMethodModify = ", {}, { method: 'DELETE' } ";
            //     }
            //     
            //     builder.AppendLine($"        return {call}(this.apiUrl + '{url}'{(method.SendsData ? $", {method.BodyParam.Name}" : "")}{reqMethodModify})");
            //     
            //     if (method.RequiresSessionSet)
            //     {
            //         builder.AppendLine("            .then(setSession)");
            //     }
            //     builder.AppendLine("    };");
            //     builder.AppendLine();
            // }

            builder.AppendLine("}");
            builder.AppendLine();
        }

        /// <summary>
        /// True if the given type is a content stream. 
        /// It can be nullable but not an array or list of them.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool IsSingularContentStream(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null)
            {
                return IsSingularContentStream(underlying);
            }

            if (!type.IsGenericType)
            {
                return false;
            }

            var def = type.GetGenericTypeDefinition();

            if (def == typeof(ContentStream<,>))
            {
                return true;
            }

            if (def == typeof(ValueTask<>) || def == typeof(Task<>))
            {
                return IsSingularContentStream(type.GetGenericArguments()[0]);
            }

            return false;
        }
    }
}
