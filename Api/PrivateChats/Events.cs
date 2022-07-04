using Api.PrivateChats;
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
		/// <summary>
		/// Set of events for a privateChatMessage.
		/// </summary>
		public static EventGroup<PrivateChatMessage> PrivateChatMessage;
		
		/// <summary>
		/// Set of events for a privateChat.
		/// </summary>
		public static EventGroup<PrivateChat> PrivateChat;
	}
}