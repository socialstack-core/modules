using System;

namespace Api.Startup
{
	/// <summary>
	/// Add [HasSecondaryResult(..)] attributes to declare that some named data is present in a secondary result.
	/// Not every endpoint of this type necessarily provides the secondary result(s).
	/// They are, for example, search facets from a search endpoint.
	/// </summary>
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
	internal sealed class HasSecondaryResultAttribute : Attribute
	{
		/// <summary>
		/// The field name.
		/// </summary>
		public string FieldName;
		/// <summary>
		/// The type of the secondary result records.
		/// </summary>
		public Type Type;
		
		/// <summary>
		/// Inform socialstack that this content type potentially has a secondary result with the given include name and it is of the given type.
		/// The given type can be any type - non-content types as well - and it is permitted to use HasVirtualField.
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="type"></param>
		public HasSecondaryResultAttribute(string fieldName, Type type){
			FieldName = fieldName;
			Type = type;
		}
		
	}
}
