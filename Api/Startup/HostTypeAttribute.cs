using System;

namespace Api.Startup
{
    /// <summary>
    /// Add [HostType("task")] attributes to your service classes to define their context for microservices.	/// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class HostTypeAttribute : Attribute
	{
        /// <summary>
        /// The host type. So "web" "task" "indexing" for example
        /// </summary>
        public string HostType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostType"></param>
        public HostTypeAttribute(string hostType)
        {
            HostType = hostType;
        }
    }
}
