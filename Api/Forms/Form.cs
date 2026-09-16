using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Forms
{
	
	/// <summary>
	/// A Form
	/// </summary>
	public partial class Form : InstallableContent<uint>
	{
        /// <summary>
        /// The name of the form
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;
		
		/// <summary>
		/// The fields of this form.
		/// </summary>
		[Module("Admin/Form/Editor")]
		public JsonString ContentJson;
	}

}