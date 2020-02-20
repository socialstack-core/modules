using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.BlogPosts
{
    /// <summary>
    /// Used when creating or updating a blog post.
    /// </summary>
    public partial class BlogPostAutoForm : AutoForm<BlogPost>
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
		public string Title;
		/// <summary>
		/// The JSON body of the post. It's JSON because it is a *canvas*. 
		/// This means the reply can easily include other components such as polls etc 
		/// and be formatted in complex ways.
		/// </summary>
		public string BodyJson;

		/// <summary>
		/// The feature image ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string FeatureRef;

		/// <summary>
		/// The icon ref. See also: "Upload.Ref" in the Uploads module.
		/// </summary>
		public string IconRef;

		/// <summary>
		/// A description of the blog post. This will be displayed on blog post lists.
		/// </summary>
		public string Description;
		
		/// <summary>
		/// The date the post was created.
		/// </summary>
		public DateTime CreatedUtc;
		
		/// <summary>
		/// The user who created it.
		/// </summary>
		public int UserId;
	}
}
