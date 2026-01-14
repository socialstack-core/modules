using System;

namespace Api.Startup
{
    /// <summary>
    /// Add [OmitIfNoService] if an endpoint can safely skip registration if the associated service does not actually exist.
	/// This happens with generic content types, such as revision, where the associated generic service wasn't created because a particular
	/// concrete version of the type is not revisionable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class OmitIfNoServiceAttribute : Attribute
	{
        /// <summary>
        /// </summary>
        public OmitIfNoServiceAttribute()
        {
        }
    }
}
