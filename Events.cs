using Api.WebSockets;
using Api.Permissions;

namespace Api.Eventing
{
	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		/// <summary>
		/// Called when a client connects.
		/// </summary>
		[DontAddPermissions]
		public static EventHandler<WebSocketClient> WebSocketClientConnected;
		
		/// <summary>
		/// Called when a user logs in/ logs out. Note that they may have multiple 
		/// </summary>
		[DontAddPermissions]
		public static EventHandler<int, UserWebsocketLinks> WebSocketUserState;
		
	}
}