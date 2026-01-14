using System;
using Api.AutoForms;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;

namespace Api.Tags
{
	/// <summary>
	/// A tag.
	/// These are the primary taxonomy mechanism; any site content can be grouped up in multiple tags.
	/// </summary>
	[ListAs("Tags", Tab = "tags_categories")]
	public partial class Tag : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the tag in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Data("required", true)]
		[Data("validate", "Required")]
		public Localized<string> Name;
		
		/// <summary>
		/// Description of this tag.
		/// </summary>
		public Localized<string> Description;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 300)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 300)]
		public string IconRef;

        /// <summary>
        /// The Hex color we want the tag to be.
        /// </summary>
        [DatabaseField(Length = 7)]
        public string HexColor;
	}
	
}
