using System;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.CustomContentTypes
{

	/// <summary>
	/// A custom content type field.
	/// </summary>
	[ListAs("CustomContentTypeFields")]
	[ImplicitFor("CustomContentTypeFields", typeof(CustomContentType))]
	[HasVirtualField("CustomContentType", typeof(CustomContentType), "CustomContentTypeId")]
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
		/// The entity that this field links to (if DataType is 'entity')
		/// </summary>
		public string LinkedEntity;

		/// <summary>
		/// The name of the field, used by socialstack.
		/// </summary>
		[Module(Hide = true)]
		public string Name;

		/// <summary>
		/// The human readable nickname of this field.
		/// </summary>
		public string NickName;

		/// <summary>
		/// True if this field localised
		/// </summary>
		public bool Localised;

		/// <summary>
		/// The validation to be applied to this field
		/// </summary>
		public string Validation;

		/// <summary>
		/// Has this field been deleted?
		/// </summary>
		[Module(Hide = true)]
		public bool Deleted;
	}

}