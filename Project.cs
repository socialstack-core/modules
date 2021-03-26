using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Projects
{
	
	/// <summary>
	/// A project for that has been worked on - used for e.g. case studies.
	/// </summary>
	public partial class Project : VersionedContent<int>
	{
        /// <summary>
        /// The name of the project in the site default language.
        /// </summary>
        [DatabaseField(Length = 200)]
		[Localized]
		public string Name;

		/// <summary>
		/// A short description of the project.
		/// </summary>
		[DatabaseField(Length = 300)]
		[Localized]
		public string Description;

		/// <summary>
		/// The primary ID of the page that this project appears on.
		/// </summary>
		public int PageId;
		
		/// <summary>
		/// The content of this project.
		/// </summary>
		[Localized]
		public string BodyJson;

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