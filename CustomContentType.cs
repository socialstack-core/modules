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
	public partial class CustomContentType : RevisionEntity<int>
	{
		/// <summary>
		/// Type name.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// The fields in this type (used internally only).
		/// </summary>
		[JsonIgnore]
		public List<CustomContentTypeField> Fields {get; set; }
	}

}