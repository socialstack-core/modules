using Api.WebSockets;
using Api.Permissions;
using Newtonsoft.Json.Linq;

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
		public static EventHandler<WebSocketClient> WebSocketClientConnected;

		/// <summary>
		/// Called when a client disconnects.
		/// </summary>
		public static EventHandler<WebSocketClient> WebSocketClientDisconnected;

		/// <summary>
		/// Called when a user logs in/ logs out. Note that they may have multiple 
		/// </summary>
		public static EventHandler<uint, UserWebsocketLinks> WebSocketUserState;
		
		/// <summary>
		/// Called when a message is received of a non-core type.
		/// </summary>
		public static EventHandler<JObject, WebSocketClient, string> WebSocketMessage;
		
	}
}