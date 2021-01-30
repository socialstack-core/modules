using System;
using Api.Database;
using Api.Users;

namespace Api.Blogs
{
	
	/// <summary>
	/// A blog post.
	/// </summary>
	public partial class BlogPost : RevisionRow
	{
		/// <summary>
		/// The blog this post is in.
		/// </summary>
		public int BlogId;
		/// <summary>
		/// The primary ID of the page that this blog post appears on.
		/// </summary>
		public int PageId;
		/// <summary>
		/// The post title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Title;
		/// <summary>
		/// The JSON body of the post. It's JSON because it is a *canvas*. 
		/// This means the reply can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		// [DatabaseField(Length = 8000)]
		public string BodyJson;
		/// <summary>
		/// URL slug (optional).
		/// </summary>
		[DatabaseField(Length = 200)]
		public string Slug;
		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		[DatabaseField(Length = 200)]
		public string IconRef;

        /// <summary>
        /// A description of the blog post. This will be displayed on blog post lists.
        /// </summary>
        [DatabaseField(Length = 500)]
        public string Description;
	}
	
}