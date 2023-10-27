using System;
using Api.CustomContentTypes;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.CustomContentTypes
{

	/// <summary>
	/// A CustomContentTypeSelectOption
	/// </summary>
	[HasVirtualField("CustomContentTypeField", typeof(CustomContentTypeField), "CustomContentTypeFieldId")]
	public partial class CustomContentTypeSelectOption : VersionedContent<uint>
	{
		/// <summary>
		/// The content type that this field belongs to.
		/// </summary>
		public uint CustomContentTypeFieldId;

		/// <summary>
		/// The value of the option
		/// </summary>
		[Localized]
		public string Value;

		/// <summary>
		/// The order the field should display
		/// </summary>
		public uint Order;
	}

}