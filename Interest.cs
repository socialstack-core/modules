using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Interests
{
	/// <summary>
	/// An Interest
	/// </summary>
	[ListAs("Interests")]
	public partial class Interest : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the interest in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Localized]
		public string Name;
			
		/// <summary>
		/// Description of this interest.
		/// </summary>
		[Localized]
		public string Description;
		
		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 80)]
		public string IconRef;
	}

}