using System;

namespace Api.Startup
{
	/// <summary>
	/// Add [Meta("fieldName")] attributes to your fields to declare that the field should be used for a particular meta property.
	/// These meta properties are used by the site header, RSS feeds and things like the content listing dropdown menus in the admin panel.
	/// If you don't declare a meta field, a best guess will be used instead for both "title" and "description".
	/// 
	/// Can also be used to link things to classes such as iocns 
	/// [Meta("icon","fa:fa-folder")
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class MetaAttribute : Attribute
	{
		/// <summary>
		/// The meta field name. Common ones are "title" and "description".
		/// </summary>
		public string FieldName;

		/// <summary>
		/// Optional value expose things like icons 
		/// </summary>
		public string Value;
		
		public MetaAttribute(string fieldName){
			FieldName = fieldName;
		}

        public MetaAttribute(string fieldName, string value)
        {
            FieldName = fieldName;
            Value = value;
        }
    }
}
