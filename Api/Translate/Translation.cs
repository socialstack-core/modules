using Api.AutoForms;
using Api.Database;
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
        [Data("required", true)]
        [Data("validate", "Required")]
		public string Module;

		/// <summary>The original text string.</summary>
		[Data("hint", "The baseline/original text to find within the component (normally no need to change this)")]
		[Data("required", true)]
		[Data("validate", "Required")]
		public string Original;
		
		/// <summary>The translation.</summary>
        [Data("hint", "The translated text to replace the original value")]
		[Data("required", true)]
		[Data("validate", "Required")]
		public Localized<string> Translated;
		
	}

}