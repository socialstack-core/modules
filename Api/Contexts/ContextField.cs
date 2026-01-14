using System;

namespace Api.Contexts
{
	/// <summary>
	/// Add [ContextField(..)] attributes to declare the specific shortcode and other config for a context field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	internal sealed class ContextFieldAttribute : Attribute
	{
		/// <summary>
		/// True if this context field should be omitted from the token e.g. because it is derived from some other code by event handlers.
		/// If unsure, leave this on the default (false).
		/// </summary>
		public bool OmitFromToken;

		/// <summary>
		/// a-z or A-Z shortcode of this ctx field.
		/// </summary>
		public char Shortcode;

		public ContextFieldAttribute(char shortcode = '\0', bool omitFromToken = false)
		{
			Shortcode = shortcode;
			OmitFromToken = omitFromToken;
		}

	}
}
