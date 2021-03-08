using System;
using Api.Database;
using Api.Startup;
using Api.Users;
using Api.Pages;

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
		/// The post title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Meta("title")]
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
		[Meta("image")]
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

		/// <summary>
		/// A synopsis of the blog post that is generated from the 
		/// </summary>
		[DatabaseField(Length = 500)]
		[Meta("description")]
		public string Synopsis;

		/// <summary>
		/// The readtime in minutes. 
		/// </summary>
		public int ReadTime;

		/// <summary>
		/// The author id. Defaults to the creator user on creation unless specified otherwise.
		/// </summary>
		public int AuthorId;

		/// <summary>
		/// The author of the blog.
		/// </summary>
		public UserProfile Author { get; set; }
	}
	
}