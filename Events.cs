using Api.Channels;
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
		/// Just before a new channel is created. The given channel won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<Channel> ChannelBeforeCreate;

		/// <summary>
		/// Just after an channel has been created. The given channel object will now have an ID.
		/// </summary>
		public static EventHandler<Channel> ChannelAfterCreate;

		/// <summary>
		/// Just before an channel is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<Channel> ChannelBeforeDelete;

		/// <summary>
		/// Just after an channel has been deleted.
		/// </summary>
		public static EventHandler<Channel> ChannelAfterDelete;

		/// <summary>
		/// Just before updating an channel. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<Channel> ChannelBeforeUpdate;

		/// <summary>
		/// Just after updating an channel.
		/// </summary>
		public static EventHandler<Channel> ChannelAfterUpdate;

		/// <summary>
		/// Just after an channel was loaded.
		/// </summary>
		public static EventHandler<Channel> ChannelAfterLoad;

		/// <summary>
		/// Just before a service loads an channel list.
		/// </summary>
		public static EventHandler<Filter<Channel>> ChannelBeforeList;

		/// <summary>
		/// Just after an channel list was loaded.
		/// </summary>
		public static EventHandler<List<Channel>> ChannelAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new channel.
		/// </summary>
		public static EndpointEventHandler<ChannelAutoForm> ChannelCreate;
		/// <summary>
		/// Delete an channel.
		/// </summary>
		public static EndpointEventHandler<Channel> ChannelDelete;
		/// <summary>
		/// Update channel metadata.
		/// </summary>
		public static EndpointEventHandler<ChannelAutoForm> ChannelUpdate;
		/// <summary>
		/// Load channel metadata.
		/// </summary>
		public static EndpointEventHandler<Channel> ChannelLoad;
		/// <summary>
		/// List channels.
		/// </summary>
		public static EndpointEventHandler<Filter<Channel>> ChannelList;

		#endregion

	}

}
