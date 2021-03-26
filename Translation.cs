using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Translate
{
	
	/// <summary>
	/// A Translation
	/// </summary>
	public partial class Translation : VersionedContent<int>
	{
        /// <summary>
        /// E.g. "UI/AboutUs" - the exact JS module name that this translation is for.
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Module;
		
		/// <summary>The original text string.</summary>
		public string Original;
		
		/// <summary>The translation.</summary>
		[Localized]
		public string Translated;
		
	}

}