using System;

namespace Api.Startup
{
	/// <summary>
	/// Add [ListAs("fieldName")] attributes to your type to declare a virtual field with the given name on all other types. The field contains a list of your type.
	/// If the target type has a name of YourContentTypeId, the list will be based on that. Otherwise, a mapping type will be internally created and used.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class ListAsAttribute : Attribute
	{
		/// <summary>
		/// The virtual field name.
		/// </summary>
		public string FieldName;

		/// <summary>
		/// True if this ListAs declaration is the primary one. A type can have multiple ListAs declarations, but only one can be primary.
		/// </summary>
		public bool IsPrimary = true;

		/// <summary>
		/// True if this ListAs must be explicitly included. It doesn't happen when * is used. I.e. you must do "*,Thing" to obtain it at all.
		/// You can specify particular types should be implicit with [ImplicitFor("ListAsFieldName", typeof(TYPE))]
		/// Note that this is implied true if there are any ImplicitFor attributes.
		/// </summary>
		public bool Explicit;

		public ListAsAttribute(string fieldName){
			FieldName = fieldName;
		}

		public ListAsAttribute(string fieldName, bool isPrimary)
		{
			FieldName = fieldName;
			IsPrimary = isPrimary;
		}

	}

	/// <summary>
	/// Used to indicate if an explicit ListAs is implicit for a particular type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class ImplicitForAttribute : Attribute
	{
		/// <summary>
		/// The name of the ListAs.
		/// </summary>
		public string ListAsName;

		/// <summary>
		/// The type.
		/// </summary>
		public Type Type;


		public ImplicitForAttribute(string fieldName, Type type)
		{
			ListAsName = fieldName;
			Type = type;
		}

	}

}
