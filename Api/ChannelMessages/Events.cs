using Api.ChannelMessages;
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
		/// Just before a new channel message is created. The given channel message won't have an ID yet. Return null to cancel the creation.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageBeforeCreate;

		/// <summary>
		/// Just after an channel message has been created. The given channel message object will now have an ID.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageAfterCreate;

		/// <summary>
		/// Just before an channel message is being deleted. Return null to cancel the deletion.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageBeforeDelete;

		/// <summary>
		/// Just after an channel message has been deleted.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageAfterDelete;

		/// <summary>
		/// Just before updating an channel message. Optionally make additional changes, or return null to cancel the update.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageBeforeUpdate;

		/// <summary>
		/// Just after updating an channel message.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageAfterUpdate;

		/// <summary>
		/// Just after an channel message was loaded.
		/// </summary>
		public static EventHandler<ChannelMessage> ChannelMessageAfterLoad;

		/// <summary>
		/// Just before a service loads an channel message list.
		/// </summary>
		public static EventHandler<Filter<ChannelMessage>> ChannelMessageBeforeList;

		/// <summary>
		/// Just after an channel message list was loaded.
		/// </summary>
		public static EventHandler<List<ChannelMessage>> ChannelMessageAfterList;

		#endregion

		#region Controller events

		/// <summary>
		/// Create a new channel message.
		/// </summary>
		public static EndpointEventHandler<ChannelMessageAutoForm> ChannelMessageCreate;
		/// <summary>
		/// Delete an channel message.
		/// </summary>
		public static EndpointEventHandler<ChannelMessage> ChannelMessageDelete;
		/// <summary>
		/// Update channel message metadata.
		/// </summary>
		public static EndpointEventHandler<ChannelMessageAutoForm> ChannelMessageUpdate;
		/// <summary>
		/// Load channel message metadata.
		/// </summary>
		public static EndpointEventHandler<ChannelMessage> ChannelMessageLoad;
		/// <summary>
		/// List channel messages.
		/// </summary>
		public static EndpointEventHandler<Filter<ChannelMessage>> ChannelMessageList;

		#endregion

	}

}
