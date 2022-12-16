using System;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json;

namespace Api.CustomContentTypes
{

	/// <summary>
	/// A custom content type. Has a list of fields.
	/// </summary>
	public partial class CustomContentType : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the type, used by socialstack.
		/// </summary>
		[Module(Hide = true)]
		public string Name;

		/// <summary>
		/// The human readable nick name.
		/// </summary>
		[Data("label", "Name")]
		public string NickName;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		[DatabaseField(Length = 100)]
		public string IconRef;

		/// <summary>
		/// The fields in this type
		/// </summary>
		[Module("Admin/CustomFieldEditor")]
		public List<CustomContentTypeField> Fields {get; set; }
	}

}