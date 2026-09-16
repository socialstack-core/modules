using System;
using System.Collections.Generic;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Forms
{
	
	/// <summary>
	/// A FormResponse
	/// </summary>
	[HasVirtualField("form", typeof(Form), "FormId")]
	public partial class FormResponse : VersionedContent<uint>
	{
		
		/// <summary>
		/// The form this was a submit of.
		/// </summary>
		public uint FormId;
		
		/// <summary>
		/// The URL the submit happened from.
		/// </summary>
		public string Url;

		/// <summary>
		/// The responses in the form. Contains a copy of the original form design but with the value set.
		/// </summary>
		[Module("UI/Input")]
		[Data("contentType", "application/json")]
		public JsonString FieldJson;

		/// <summary>
		/// Loads the FieldJson into a dictionary of field names and values.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string> LoadFieldJson()
		{
			var jsonString = FieldJson.ValueOf();

			if(string.IsNullOrEmpty(jsonString))
			{
				return new Dictionary<string, string>();
			}

			return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
		}
	}

}