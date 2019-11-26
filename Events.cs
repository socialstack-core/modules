using Api.Followers;
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
		/// Just before a new follower is created. The given follower won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Follower> FollowerBeforeCreate;

		/// <summary>
		/// Just after an follower has been created. The given follower object will now have an ID.
		/// </summary>
		public static EventHandler<Follower> FollowerAfterCreate;

		/// <summary>
		/// Just before an follower is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Follower> FollowerBeforeDelete;

		/// <summary>
		/// Just after an follower has been deleted.
		/// </summary>
		public static EventHandler<Follower> FollowerAfterDelete;

		/// <summary>
		/// Just before updating an follower. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Follower> FollowerBeforeUpdate;

		/// <summary>
		/// Just after updating an follower.
		/// </summary>
		public static EventHandler<Follower> FollowerAfterUpdate;

		/// <summary>
		/// Just after an follower was loaded.
		/// </summary>
		public static EventHandler<Follower> FollowerAfterLoad;

		/// <summary>
		/// Just before a service loads an follower list.
		/// </summary>
		public static EventHandler<Filter<Follower>> FollowerBeforeList;

		/// <summary>
		/// Just after an follower list was loaded.
		/// </summary>
		public static EventHandler<List<Follower>> FollowerAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new follower.
		/// </summary>
		public static EndpointEventHandler<FollowerAutoForm> FollowerCreate;
		/// <summary>
		/// Delete an follower.
		/// </summary>
		public static EndpointEventHandler<Follower> FollowerDelete;
		/// <summary>
		/// Update follower metadata.
		/// </summary>
		public static EndpointEventHandler<FollowerAutoForm> FollowerUpdate;
		/// <summary>
		/// Load follower metadata.
		/// </summary>
		public static EndpointEventHandler<Follower> FollowerLoad;
		/// <summary>
		/// List followers.
		/// </summary>
		public static EndpointEventHandler<Filter<Follower>> FollowerList;

		#endregion

	}

}
