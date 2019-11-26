using Api.Forums;
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
		/// Just before a new forum is created. The given forum won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Forum> ForumBeforeCreate;

		/// <summary>
		/// Just after an forum has been created. The given forum object will now have an ID.
		/// </summary>
		public static EventHandler<Forum> ForumAfterCreate;

		/// <summary>
		/// Just before an forum is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Forum> ForumBeforeDelete;

		/// <summary>
		/// Just after an forum has been deleted.
		/// </summary>
		public static EventHandler<Forum> ForumAfterDelete;

		/// <summary>
		/// Just before updating an forum. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Forum> ForumBeforeUpdate;

		/// <summary>
		/// Just after updating an forum.
		/// </summary>
		public static EventHandler<Forum> ForumAfterUpdate;

		/// <summary>
		/// Just after an forum was loaded.
		/// </summary>
		public static EventHandler<Forum> ForumAfterLoad;

		/// <summary>
		/// Just before a service loads an forum list.
		/// </summary>
		public static EventHandler<Filter<Forum>> ForumBeforeList;

		/// <summary>
		/// Just after an forum list was loaded.
		/// </summary>
		public static EventHandler<List<Forum>> ForumAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new forum.
		/// </summary>
		public static EndpointEventHandler<ForumAutoForm> ForumCreate;
		/// <summary>
		/// Delete an forum.
		/// </summary>
		public static EndpointEventHandler<Forum> ForumDelete;
		/// <summary>
		/// Update forum metadata.
		/// </summary>
		public static EndpointEventHandler<ForumAutoForm> ForumUpdate;
		/// <summary>
		/// Load forum metadata.
		/// </summary>
		public static EndpointEventHandler<Forum> ForumLoad;
		/// <summary>
		/// List forums.
		/// </summary>
		public static EndpointEventHandler<Filter<Forum>> ForumList;

		#endregion

	}

}
