using System;

namespace Api.Startup
{
	/// <summary>
	/// Add [ListAs("fieldName")] attributes to your type to declare a virtual field with the given name on all other types. The field contains a list of your type.
	/// If the target type has a name of YourContentTypeId, the list will be based on that. Otherwise, a mapping type will be internally created and used.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	internal sealed class ListAsAttribute : Attribute
	{
		/// <summary>
		/// The virtual field name.
		/// </summary>
		public string FieldName;
		
		public ListAsAttribute(string fieldName){
			FieldName = fieldName;
		}
		
	}
}
