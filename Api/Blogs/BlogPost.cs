using Api.Database;
using Api.Startup;
using Api.Users;
using Api.AutoForms;

namespace Api.Blogs
{

	/// <summary>
	/// A blog post.
	/// </summary>
	[HasVirtualField("Blog", typeof(Blogs.Blog), "BlogId")]
	public partial class BlogPost : VersionedContent<uint>
	{
		/// <summary>
		/// The blog this post is in.
		/// </summary>
		public uint BlogId;

		/// <summary>
		/// The post title in the site default language.
		/// </summary>
		[DatabaseField(Length = 200)]
		[Meta("title")]
		[Data("validate", "Required")]
		[Data("required", true)]
		public string Title;

		/// <summary>
		/// The HTML body of the blog post.
		/// </summary>
		[Data("tab", "content")]
		public string BodyHtml;

		/// <summary>
		/// URL slug (generated).
		/// </summary>
		[DatabaseField(Length = 200)]
		[Data("readonly", true)]
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
		[Meta("description")]
		public string Description;

		/// <summary>
		/// A synopsis of the blog post that is generated from the rest of the body.
		/// </summary>
		[DatabaseField(Length = 500)]
		public string Synopsis;

		/// <summary>
		/// The readtime in minutes. 
		/// </summary>
		public int ReadTime;
	}

}