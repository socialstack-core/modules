using Api.BlogPosts;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		#region Service events

		/// <summary>
		/// Just before a new blog post is created. The given blog post won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostBeforeCreate;

		/// <summary>
		/// Just after an blog post has been created. The given blog post object will now have an ID.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostAfterCreate;

		/// <summary>
		/// Just before an blog post is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostBeforeDelete;

		/// <summary>
		/// Just after an blog post has been deleted.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostAfterDelete;

		/// <summary>
		/// Just before updating an blog post. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostBeforeUpdate;

		/// <summary>
		/// Just after updating an blog post.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostAfterUpdate;

		/// <summary>
		/// Just after an blog post was loaded.
		/// </summary>
		public static EventHandler<BlogPost> BlogPostAfterLoad;
		/// <summary>
		/// Just before a service loads a blog post list.
		/// </summary>
		public static EventHandler<Filter<BlogPost>> BlogPostBeforeList;

		/// <summary>
		/// Just after a blog post list was loaded.
		/// </summary>
		public static EventHandler<List<BlogPost>> BlogPostAfterList;
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new blog post.
		/// </summary>
		public static EndpointEventHandler<BlogPostAutoForm> BlogPostCreate;
		/// <summary>
		/// Delete an blog post.
		/// </summary>
		public static EndpointEventHandler<BlogPost> BlogPostDelete;
		/// <summary>
		/// Update blog post metadata.
		/// </summary>
		public static EndpointEventHandler<BlogPostAutoForm> BlogPostUpdate;
		/// <summary>
		/// Load blog post metadata.
		/// </summary>
		public static EndpointEventHandler<BlogPost> BlogPostLoad;
		/// <summary>
		/// List blog posts.
		/// </summary>
		public static EndpointEventHandler<Filter<BlogPost>> BlogPostList;

		#endregion

	}

}
