using Api.ForumThreads;
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
		/// Just before a new forum thread is created. The given forum thread won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadBeforeCreate;

		/// <summary>
		/// Just after an forum thread has been created. The given forum thread object will now have an ID.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadAfterCreate;

		/// <summary>
		/// Just before an forum thread is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadBeforeDelete;

		/// <summary>
		/// Just after an forum thread has been deleted.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadAfterDelete;

		/// <summary>
		/// Just before updating an forum thread. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadBeforeUpdate;

		/// <summary>
		/// Just after updating an forum thread.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadAfterUpdate;

		/// <summary>
		/// Just after an forum thread was loaded.
		/// </summary>
		public static EventHandler<ForumThread> ForumThreadAfterLoad;

		/// <summary>
		/// Just before a service loads an forumThread list.
		/// </summary>
		public static EventHandler<Filter<ForumThread>> ForumThreadBeforeList;

		/// <summary>
		/// Just after an forumThread list was loaded.
		/// </summary>
		public static EventHandler<List<ForumThread>> ForumThreadAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new forum thread.
		/// </summary>
		public static EndpointEventHandler<ForumThreadAutoForm> ForumThreadCreate;
		/// <summary>
		/// Delete an forum thread.
		/// </summary>
		public static EndpointEventHandler<ForumThread> ForumThreadDelete;
		/// <summary>
		/// Update forum thread metadata.
		/// </summary>
		public static EndpointEventHandler<ForumThreadAutoForm> ForumThreadUpdate;
		/// <summary>
		/// Load forum thread metadata.
		/// </summary>
		public static EndpointEventHandler<ForumThread> ForumThreadLoad;
		/// <summary>
		/// List forum threads.
		/// </summary>
		public static EndpointEventHandler<Filter<ForumThread>> ForumThreadList;

		#endregion

	}

}
