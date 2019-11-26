using Api.ChannelUsers;
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
		/// Just before a new channel user is created. The given channel user won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserBeforeCreate;

		/// <summary>
		/// Just after an channel user has been created. The given channel user object will now have an ID.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserAfterCreate;

		/// <summary>
		/// Just before an channel user is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserBeforeDelete;

		/// <summary>
		/// Just after an channel user has been deleted.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserAfterDelete;

		/// <summary>
		/// Just before updating an channel user. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserBeforeUpdate;

		/// <summary>
		/// Just after updating an channel user.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserAfterUpdate;

		/// <summary>
		/// Just after an channel user was loaded.
		/// </summary>
		public static EventHandler<ChannelUser> ChannelUserAfterLoad;

		/// <summary>
		/// Just before a service loads an channel user list.
		/// </summary>
		public static EventHandler<Filter<ChannelUser>> ChannelUserBeforeList;

		/// <summary>
		/// Just after an channel user list was loaded.
		/// </summary>
		public static EventHandler<List<ChannelUser>> ChannelUserAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new channel user.
		/// </summary>
		public static EndpointEventHandler<ChannelUserAutoForm> ChannelUserCreate;
		/// <summary>
		/// Delete an channel user.
		/// </summary>
		public static EndpointEventHandler<ChannelUser> ChannelUserDelete;
		/// <summary>
		/// Update channel user metadata.
		/// </summary>
		public static EndpointEventHandler<ChannelUserAutoForm> ChannelUserUpdate;
		/// <summary>
		/// Load channel user metadata.
		/// </summary>
		public static EndpointEventHandler<ChannelUser> ChannelUserLoad;
		/// <summary>
		/// List channel users.
		/// </summary>
		public static EndpointEventHandler<Filter<ChannelUser>> ChannelUserList;

		#endregion

	}

}
