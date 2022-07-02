using System;

namespace Api.Startup
{
	/// <summary>
	/// Add [HasOptionalField(..)] attributes to declare that some named optional secondary data.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
	internal sealed class HasVirtualFieldAttribute : Attribute
	{
		/// <summary>
		/// The field name.
		/// </summary>
		public string FieldName;
		/// <summary>
		/// The type that the ID is for. If this is null, the optional field is of mixed type, and both IdSourceField and TypeIdSourceField must both be set.
		/// </summary>
		public Type Type;
		/// <summary>
		/// The field on the class that the ID of the optional object comes from.
		/// </summary>
		public string IdSourceField;

		public HasVirtualFieldAttribute(string fieldName, Type type, string idSourceField){
			FieldName = fieldName;
			Type = type;
			IdSourceField = idSourceField;
		}

	}
}
