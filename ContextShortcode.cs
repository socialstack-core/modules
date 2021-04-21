using System;

namespace Api.Contexts
{
	/// <summary>
	/// Add [ContextShortcode(..)] attributes to declare the specific shortcode for a context field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	internal sealed class ContextShortcodeAttribute : Attribute
	{
		/// <summary>
		/// a-z or A-Z shortcode of this ctx field.
		/// </summary>
		public char Shortcode;

		public ContextShortcodeAttribute(char shortcode){
			Shortcode = shortcode;
		}

	}
}
