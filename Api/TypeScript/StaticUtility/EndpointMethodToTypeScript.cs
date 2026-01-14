using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Api.Startup;
using Api.Startup.Routing;
using Api.TypeScript.Contracts;
using Api.TypeScript.Objects;
using Newtonsoft.Json.Linq;

namespace Api.TypeScript
{
    /// <summary>
    /// A utility/helper class for taking Http endpoints
    /// from the C# side, and converting them to TypeScript
    /// equivalents.
    /// </summary>
    public static class EndpointMethodToTypeScript
    {
        /// <summary>
        /// Converts an C# controller endpoint into a callable API 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="controller"></param>
        /// <param name="output"></param>
        /// <param name="svc"></param>
        public static void ConvertEndpointToTypeScript(Type type, MethodCollector controller, StringBuilder output, TypeScriptService svc)
        {
            foreach (var endpoint in controller.GetEndpointMethods(type))
            {
                // add some light documentation to each method
                // a future TODO: add CSDoc in place of this
                output.AppendLine("     /*");
                output.AppendLine("     * Generated from a .NET type");
                output.AppendLine($"     * @see {{{svc.GetGenericSignature(endpoint.Method.DeclaringType)}::{endpoint.Method.Name}}}");
                output.AppendLine($"     * @url {endpoint.RequestUrl}");
                output.AppendLine($"     * @debug - method.ReturnType {(endpoint.ReturnType.FullName)}");
                output.AppendLine($"     * @debug - method.TrueReturnType {(endpoint.TrueReturnType.FullName)}");
                output.AppendLine("      */");
                
                // append 4 blank spaces, and then the method name with a lower case first character
                // for instance ListCSV => listCSV, Update => update etc...
                // then open the parameter block. 
                output.Append($"     {TypeScriptService.LcFirst(endpoint.Method.Name)} = (");
                
                // Parameter list generation
                // we keep count for comma the delimiter
                var paramCount = 0;
                
                // if the endpoint returns a session context
                // then we inject this little block, 
                // that way, the developer calling this function
                // can just set the session and handle the 
                // update in a necessary way.
                if (endpoint.RequiresSessionSet)
                {
                    output.Append("setSession: (s: SessionResponse) => Session");
                    paramCount++;
                }
                
                // web safe params are serializable structures that are
                // recognised and parseable on both sides of the application
                // both front and back, a web safe param would be a string
                // or struct required by an endpoint, a non-safe param would be
                // X059Certificate
                foreach (var param in endpoint.WebSafeParams)
                {
                    // keeps the args seperated correctly. 
                    if (paramCount > 0)
                    {
                        output.Append(", ");
                    }
                    
                    // this JObject generally applies to AutoController<,>
                    // descendants, however this is also not a guaranteed
                    // isolated case, so we assume Partial<T>
                    // the reason we use Partial is the entire object may 
                    // not be fully populated, i.e. in a creation context.
                    var typeName = param.ParameterType == typeof(JObject)
                        ? "Partial<T>"
                        // otherwise we get its generic signature.
                        : svc.GetGenericSignature(param.ParameterType);
                
                    // however, if the parameter is explictly a certain 
                    // type, make sure it changes to that instead. 
                    if (TypeScriptService.IsEntityType(param.ParameterType))
                    {
                        typeName = "Partial<" + typeName + ">";
                    }
                    
                    // outputs the argument, if the parameter is nullable, then append '?' to mark it optional
                    // and appends the target type too.
                    output.Append($"{param.Name}{(TypeScriptService.IsNullable(param.ParameterType) ? "?" : "")}: {typeName}");
                    
                    // increment the paramCount as we've just added one. 
                    paramCount++;
                }
                
                // includes are a powerful part of the API
                // when an entity is returned includes parameter
                // MUST exist.
                if (endpoint.RequiresIncludes)
                {
                    // if a parameter exists, we need a comma beforehand
                    // to keep the syntax legal
                    if (paramCount > 0)
                    {
                        output.Append(", ");
                    }
                    output.Append("includes?: ApiIncludes[]");
                }
                
                // close the closure parameter block. 
                output.Append("):");
                
                // next we need the return type, 
                // this is a promise, with the generic
                // argument set to whatever the 
                // endpoint returns. 
                var returnInfo = GetReturnTypeAndCaller(endpoint, svc);
                var returnType = returnInfo.Item1;
                var caller = returnInfo.Item2;
                
                // append the Promise enclosed return type
                // the return type at this point is the 
                // typescript return type.
                output.Append(" Promise<" + returnType + $">");
                
                // now the arrow
                output.Append(" => {");
                
                // clear a line for readability
                output.AppendLine();
                
                output.AppendLine($"        return {caller}(this.apiUrl + '{URLBuilder.BuildUrl(endpoint)}'{(endpoint.SendsData ? $", {endpoint.BodyParam.Name}" : ", undefined")}, {{ method: '{endpoint.RequestMethod}' }} )");
                
                if (endpoint.RequiresSessionSet)
                {
                    output.AppendLine("            .then(setSession)");
                }
                
                output.AppendLine("     }");

                output.AppendLine(); // empty line to separate methods.
                output.AppendLine();
            }
        }

        private static (string, string) GetReturnTypeAndCaller(ControllerMethod method, TypeScriptService svc)
        {
            var returnType = "string";
            var caller = "getText";

            Type[] textualTypes =
            [
                typeof(string),
                typeof(FileContent),
                typeof(void),
                typeof(ValueTask)
            ];
            
            // this is true if the entity is any type of collection
            if (method.IsList)
            {
                // if the return type is generic.
                if (method.ReturnType.IsGenericType)
                {
                    // most collections are generic stemming from bases 
                    // like IEnumerable<>, however SocialStack has a
                    // type called ContentStream, this needs to be handled
                    // as seen below.
                    if (method.ReturnType.GetGenericTypeDefinition() == typeof(ContentStream<,>))
                    {
                        // A content stream. ApiList<T> where T is just a type parameter
                        caller = $"getList<{svc.GetGenericSignature(method.ReturnType.GetGenericArguments()[0])}>";
                        returnType = $"ApiList<{svc.GetGenericSignature(method.ReturnType.GetGenericArguments()[0])}>";
                    }
                    else
                    {
                        // if a response is something like IEnumerable<User> or List<User>
                        caller = $"getJson<{svc.GetGenericSignature(method.ReturnType)}>";
                        returnType = svc.GetGenericSignature(method.ReturnType);
                    }
                }
                // just a normal array User[] for instance. 
                else
                {
                    caller = $"getJson<{svc.GetGenericSignature(method.ReturnType)}>";
                    returnType = svc.GetGenericSignature(method.ReturnType);
                }
            }
            else
            {
                // this else block means no collection is present, this is purely 
                // singular objects or plaintext responses.
                
                // most collections are generic stemming from bases 
                // like IEnumerable<>, however SocialStack has a
                // type called ContentStream, this needs to be handled
                // as seen below.
                // if the return type is generic.
                if (method.ReturnType.IsGenericType)
                {
                    if (method.ReturnType.GetGenericTypeDefinition() == typeof(ContentStream<,>))
                    {
                        // A content stream. ApiList<T> where T is just a type parameter
                        caller = $"getList<{svc.GetGenericSignature(method.ReturnType.GetGenericArguments()[0])}>";
                        returnType = $"ApiList<{svc.GetGenericSignature(method.ReturnType.GetGenericArguments()[0])}>";

                        return (returnType, caller);
                    }
                }

                // when a session set is required.
                // its return type and caller can
                // only be these two set.
                if (method.RequiresSessionSet)
                {
                    caller = "getJson<SessionResponse>";
                    returnType = "Session";
                }
                else if (TypeScriptService.IsEntityType(method.ReturnType))
                {
                    caller = $"getOne<{svc.GetGenericSignature(method.ReturnType)}>";
                    returnType = svc.GetGenericSignature(method.ReturnType);
                }
                else if (!textualTypes.Contains(method.ReturnType))
                {
                    caller = $"getJson<{svc.GetGenericSignature(method.ReturnType)}>";
                    returnType = svc.GetGenericSignature(method.ReturnType);
                }
            }
            
            
            return (returnType, caller);
        }
        
        private static bool IsSingularContentStream(Type type)
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