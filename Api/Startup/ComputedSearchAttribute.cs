using System;

namespace Api.Startup
{
    /// <summary>
    /// Use this attribute to include computed properties when mapping fields for mongo search
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class ComputedSearchAttribute : Attribute
    {
        public ComputedSearchAttribute()
        {
        }
    }
}