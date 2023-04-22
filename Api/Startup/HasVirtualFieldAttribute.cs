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
		/// An alternative to a type reference, you can instead use the name, or the name of a field holding the type name.
		/// Similar to the Type field, and the Type field wins if they are both set.
		/// </summary>
		public string TypeSourceField;
		/// <summary>
		/// The field on the class that the ID of the optional object comes from.
		/// </summary>
		public string IdSourceField;
		/// <summary>
		/// The virtual field is a list of the given type/typename.
		/// </summary>
		public bool List;

		public HasVirtualFieldAttribute(string fieldName, Type type, string idSourceField){
			FieldName = fieldName;
			Type = type;
			IdSourceField = idSourceField;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="typeSourceField">Can be either a type name, or the name of a field where the type will be loaded.
		/// In the latter case, ID source field MUST be a ulong field. 
		/// Otherwise, it must match the type of ID that the referenced type uses.</param>
		/// <param name="idSourceField"></param>
		public HasVirtualFieldAttribute(string fieldName, string typeSourceField, string idSourceField){
			FieldName = fieldName;
			TypeSourceField = typeSourceField;
			IdSourceField = idSourceField;
		}
		
		public HasVirtualFieldAttribute(string fieldName, string typeSourceField, bool isList){
			FieldName = fieldName;
			TypeSourceField = typeSourceField;
			List = isList;

			if (isList)
			{
				IdSourceField = "Id";
			}
		}

	}
}
