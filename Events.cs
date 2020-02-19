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
		/// Set of events for a BlogPost.
		/// </summary>
		public static EventGroup<BlogPost> BlogPost;
		
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
