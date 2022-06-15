using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Translate
{
	
	/// <summary>
	/// A Translation
	/// </summary>
	public partial class Translation : VersionedContent<uint>
	{
        /// <summary>
        /// E.g. "UI/AboutUs" - the exact JS module name that this translation is for.
        /// </summary>
        [DatabaseField(Length = 200)]
        [Data("hint", "The ux module exposing the value (where it exposed within the site)")]
		public string Module;

		/// <summary>The original text string.</summary>
		[Data("hint", "The baseline/original text to find within the component (normally no need to change this)")]
		public string Original;
		
		/// <summary>The translation.</summary>
		[Localized]
        [Data("hint", "The translated text to replace the original value (site restart required if changed !)")]
		public string Translated;
		
	}

}