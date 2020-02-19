using Api.Blogs;
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
		/// Set of events for a Blog.
		/// </summary>
		public static EventGroup<Blog> Blog;
		
		#endregion

		#region Controller events

		/// <summary>
		/// Create a new blog.
		/// </summary>
		public static EndpointEventHandler<BlogAutoForm> BlogCreate;
		/// <summary>
		/// Delete an blog.
		/// </summary>
		public static EndpointEventHandler<Blog> BlogDelete;
		/// <summary>
		/// Update blog metadata.
		/// </summary>
		public static EndpointEventHandler<BlogAutoForm> BlogUpdate;
		/// <summary>
		/// Load blog metadata.
		/// </summary>
		public static EndpointEventHandler<Blog> BlogLoad;
		/// <summary>
		/// List blogs.
		/// </summary>
		public static EndpointEventHandler<Filter<Blog>> BlogList;

		#endregion

	}

}
