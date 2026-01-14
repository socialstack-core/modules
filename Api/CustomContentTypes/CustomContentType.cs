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
		[Data("required", true)]
		[Data("validate", "Required")]
		public string Name;

		/// <summary>
		/// The human readable nick name.
		/// </summary>
		[Data("label", "Name")]
		public string NickName;

        /// <summary>
        /// The summary/usage of the content.
        /// </summary>
        [DatabaseField(Length = 100)]
        [Data("label", "Summary")]
        public string Summary;

        /// <summary>
        /// Optional image to show with this item.
        /// </summary>
        [DatabaseField(Length = 300)]
		[Data("type", "icon")]
		public string IconRef;

		/// <summary>
		/// The fields in this type
		/// </summary>
		[JsonIgnore]
		public List<CustomContentTypeField> Fields {get; set; }

		/// <summary>
		/// Has this type been deleted?
		/// </summary>
		[Module(Hide = true)]
		public bool Deleted;
	}

}
