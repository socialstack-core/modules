using System;
using System.Collections.Generic;
using System.Reflection;

namespace Api.TypeScript.Objects
{
    /// <summary>
    /// Represents metadata about a controller method for TypeScript client generation.
    /// </summary>
    public class ControllerMethod
    {
        /// <summary>
        /// The method
        /// </summary>
        public MethodInfo Method { get; set; }
        /// <summary>
        /// URL pattern
        /// </summary>
        public string RequestUrl { get; set; }
        /// <summary>
        /// The body parameter if one is present.
        /// </summary>
        public ParameterInfo BodyParam { get; set; }
        /// <summary>
        /// True if it requires setSession being given to it.
        /// </summary>
        public bool RequiresSessionSet { get; set; } = false;
        /// <summary>
        /// True if the endpoint supports includes
        /// </summary>
        public bool RequiresIncludes { get; set; } = false;
        /// <summary>
        /// True if the endpoint sends anything
        /// </summary>
        public bool SendsData { get; set; } = false;
        /// <summary>
        /// True if the endpoint returns a list
        /// </summary>
        public bool IsList { get; set; } = false;
        /// <summary>
        /// The actual stated return type
        /// </summary>
        public Type TrueReturnType { get; set; }
        /// <summary>
        /// The resolved return type
        /// </summary>
        public Type ReturnType { get; set; }
        /// <summary>
        /// The set of usable parameters
        /// </summary>
        public List<ParameterInfo> WebSafeParams { get; set; }
        
        /// <summary>
        /// Takes the request method.
        /// </summary>
        public string RequestMethod { set; get; }
    }
}
