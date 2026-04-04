using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Api.Startup.Routing;
using Api.TypeScript.Objects;
using Microsoft.AspNetCore.Mvc;

namespace Api.TypeScript.Contracts;

/// <summary>
/// This class collects .NET methods and returns important information about
/// the target the TS engine is transforming
/// </summary>
public abstract class MethodCollector : AbstractTypeScriptObject
{
    /// <summary>
    /// caching mechanism
    /// </summary>
    private readonly List<ControllerMethod> _methods = [];
    private ESModule _module;

    /// <summary>
    /// Creates a new MethodCollector using the given ESModule.
    /// </summary>
    /// <param name="module"></param>
    public MethodCollector(ESModule module)
    {
        _module = module;
    }

    /// <summary>
    /// Collects an array of valid endpoints for said controller.
    /// </summary>
    /// <param name="controller"></param>
    /// <returns></returns>
    public List<ControllerMethod> GetEndpointMethods(Type controller)
    {
        // cached results, saves iterations below. 
        if (_methods.Count != 0)
        {
            return _methods;
        }
        
        // gets all instanced & public methods within a type
        // we ignore constructors
        foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            
            if (method.IsConstructor)
            {
                continue;
            }
            
            // both the AutoController & its inheritors both come through here
            // we don't want AutoController methods in all the
            // entity apis as this defeats the purpose of OOP.
            if (
                method.DeclaringType is { IsGenericType: true } && 
                method.DeclaringType.GetGenericTypeDefinition() == typeof(AutoController<,>) &&
                controller != typeof(AutoController<,>))
            {
                continue;
            }

            // Skip if method overload with more parameters already exists
            // overloads require a lot of complex logic, which at the moment
            // isn't viable.
            var existing = _methods.Find(m => m.Method.Name == method.Name);
            
            // if an existing one is found, if the parameter count is more than the current one
            // remove and replace, otherwise skip the generation for this method.
            if (existing is not null)
            {
                if (existing.Method.GetParameters().Length >= method.GetParameters().Length)
                {
                    continue;
                }
                // remove as mentioned above.
                _methods.Remove(existing);
            }
            
            // gets all the current attributes assigned to that method.
            var methodAttributes = method.GetCustomAttributes().ToArray();
            
            // we're looking for any http attribute, this is what seperates
            // C# only methods from frontend methods
            var httpAttribute = methodAttributes.FirstOrDefault(attr => attr is RouteAttribute
                or HttpGetAttribute or HttpPostAttribute or HttpPutAttribute
                or HttpDeleteAttribute or HttpPatchAttribute);
            
            // if non is found, we just skip.
            if (httpAttribute is null)
            {
                continue;
            }
            
            var requestMethod = httpAttribute switch
            {
                HttpGetAttribute => "GET",
                HttpPostAttribute => "POST",
                HttpPutAttribute => "PUT",
                HttpDeleteAttribute => "DELETE",
                HttpPatchAttribute => "PATCH",
                RouteAttribute => "ANY", // Optional: or you can handle this differently
                _ => "UNKNOWN"
            };
            
            // create the controller method instance
            var controllerMethod = new ControllerMethod()
            {
                // this is a direct reference
                // back to the method via reflection.
                Method = method,
                // Initialise an array of web safe params. 
                WebSafeParams = [],
                RequestUrl = GetRouteTemplate(httpAttribute),
                RequestMethod = requestMethod,
            };

            controllerMethod.RequestUrl = URLBuilder.BuildUrl(controllerMethod);

            // check for an override.
            var returnsAttribute = methodAttributes.FirstOrDefault(attr => attr is ReturnsAttribute);
            
            // if an override exists, use the said type
            // otherwise, use the one the method specifies.
            var returnType = returnsAttribute != null
                // the check above guarantees this,
                // the bang operator here is valid.
                ? (returnsAttribute as ReturnsAttribute)!.ReturnType
                // unwrap value task removes the value task, should there
                // be one.
                : UnwrapValueTask(method.ReturnType);
            
            // the return type is then stored in here.
            controllerMethod.ReturnType = Nullable.GetUnderlyingType(returnType) ?? returnType;
            
            // the original return type is stored in here.
            controllerMethod.TrueReturnType = method.ReturnType;
            
            // if the return type is an array
            controllerMethod.IsList = typeof(IEnumerable<>).IsAssignableFrom(controllerMethod.ReturnType);
            
            // start off with it being false.
            controllerMethod.RequiresIncludes = false;
            
            // if the response is a content stream, we need includes too.
            if (controllerMethod.ReturnType.IsGenericType && controllerMethod.ReturnType.GetGenericTypeDefinition() == typeof(ContentStream<,>))
            {
                controllerMethod.RequiresIncludes = true;
            }
            
            // if it returns an entity, it should also accept includes.
            if (TypeScriptService.IsEntityType(returnType))
            {
                controllerMethod.RequiresIncludes = true;
            }

            if (method.DeclaringType.IsGenericType &&
                method.DeclaringType.GetGenericTypeDefinition() == typeof(AutoController<,>))
            {
                controllerMethod.RequiresIncludes = true;
            }
            
            // if it returns the context, it most certainly
            // needs the session set
            if (returnType == typeof(Context))
            {
                controllerMethod.RequiresSessionSet = true;
                controllerMethod.RequiresIncludes = false;
            }
            
            // now for the params.
            var webSafeParams = new List<ParameterInfo>();
            foreach (var param in method.GetParameters())
            {
                if (param.ParameterType == typeof(Context))
                {
                    continue;
                }

                if (param.Name == "includes")
                {
                    continue;
                }
                var isFromRoute = controllerMethod.RequestUrl?.Contains($"{{{param.Name}}}") == true &&
                                   param.GetCustomAttribute<FromRouteAttribute>() != null;
                var isFromQuery = param.GetCustomAttribute<FromQueryAttribute>() != null;
                var isFromBody = param.GetCustomAttribute<FromBodyAttribute>() != null;
                
                if (isFromRoute || isFromQuery || isFromBody || _module.TypeScriptService.GetTypeOverwrite(param.ParameterType) != null)
                {
                     webSafeParams.Add(param);
                }

                if (isFromBody)
                {
                    controllerMethod.SendsData = true;
                    controllerMethod.BodyParam = param;
                }
            }

            controllerMethod.WebSafeParams = webSafeParams;
            
            _methods.Add(controllerMethod);
        }
        return _methods;
    }
    private static Type UnwrapValueTask(Type returnType)
    {
        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)
            ? returnType.GetGenericArguments()[0]
            : returnType;
    }
    private static string GetRouteTemplate(Attribute httpAttribute)
    {
        return httpAttribute switch
        {
            HttpGetAttribute get => get.Template,
            HttpPostAttribute post => post.Template,
            HttpPutAttribute put => put.Template,
            HttpDeleteAttribute delete => delete.Template,
            HttpPatchAttribute patch => patch.Template,
            RouteAttribute route => route.Template,
            _ => null
        };
    }
}