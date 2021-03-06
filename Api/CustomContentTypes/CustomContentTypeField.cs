using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.CustomContentTypes
{
	
	/// <summary>
	/// A custom content type field.
	/// </summary>
	public partial class CustomContentTypeField : VersionedContent<uint>
	{
		/// <summary>
		/// The content type that this field belongs to.
		/// </summary>
		public uint CustomContentTypeId;
		
		/// <summary>
		/// This fields default value.
		/// </summary>
		public string DefaultValue;
		
		/// <summary>
		/// The type of this field.
		/// </summary>
		public string DataType;
		
		/// <summary>
		/// The name of this field.
		/// </summary>
		public string Name;
	}

}