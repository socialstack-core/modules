using System;
using System.Collections.Generic;
using Api.Database;
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
		/// Type name.
		/// </summary>
		public string Name;

		/// <summary>
		/// Optional image to show with this item.
		/// </summary>
		[DatabaseField(Length = 100)]
		public string IconRef;

		/// <summary>
		/// The fields in this type (used internally only).
		/// </summary>
		[JsonIgnore]
		public List<CustomContentTypeField> Fields {get; set; }
	}

}