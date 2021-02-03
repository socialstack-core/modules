using System;

namespace Api.Startup
{
	/// <summary>
	/// Add [Meta("fieldName")] attributes to your fields to declare that the field should be used for a particular meta property.
	/// These meta properties are used by the site header, RSS feeds and things like the content listing dropdown menus in the admin panel.
	/// If you don't declare a meta field, a best guess will be used instead for both "title" and "description".
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class MetaAttribute : Attribute
	{
		/// <summary>
		/// The meta field name. Common ones are "title" and "description".
		/// </summary>
		public string FieldName;
		
		public MetaAttribute(string fieldName){
			FieldName = fieldName;
		}
		
	}
}
