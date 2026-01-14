using System;

namespace Api.Startup
{
    /// <summary>
    /// Use this attribute to exclude data types for content indexing (used in admin search)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class , Inherited = false, AllowMultiple = false)]
    internal sealed class IsNotSearchableAttribute : Attribute
    {
        public IsNotSearchableAttribute()
        {
        }
    }
}