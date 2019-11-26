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
		/// Just before a new blog is created. The given blog won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Blog> BlogBeforeCreate;

		/// <summary>
		/// Just after an blog has been created. The given blog object will now have an ID.
		/// </summary>
		public static EventHandler<Blog> BlogAfterCreate;

		/// <summary>
		/// Just before an blog is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Blog> BlogBeforeDelete;

		/// <summary>
		/// Just after an blog has been deleted.
		/// </summary>
		public static EventHandler<Blog> BlogAfterDelete;

		/// <summary>
		/// Just before updating an blog. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Blog> BlogBeforeUpdate;

		/// <summary>
		/// Just after updating an blog.
		/// </summary>
		public static EventHandler<Blog> BlogAfterUpdate;

		/// <summary>
		/// Just after an blog was loaded.
		/// </summary>
		public static EventHandler<Blog> BlogAfterLoad;

		/// <summary>
		/// Just before a service loads an blog list.
		/// </summary>
		public static EventHandler<Filter<Blog>> BlogBeforeList;

		/// <summary>
		/// Just after an blog list was loaded.
		/// </summary>
		public static EventHandler<List<Blog>> BlogAfterList;

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
