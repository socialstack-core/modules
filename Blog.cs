using System;
using Api.Database;
using Api.Users;

namespace Api.Blogs
{

    /// <summary>
    /// These are containers for blog posts.
    /// </summary>
    public partial class Blog : VersionedContent<uint>
	{
		/// <summary>
		/// The name of the blog in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Name;

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

        /// <summary>
        /// The description of the blog that will appear on the blog list.
        /// </summary>
        [DatabaseField(Length = 500)]
        public string Description;
	}
	
}