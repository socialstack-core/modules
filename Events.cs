using Api.WebSockets;
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
		/// Called when a client connects.
		/// </summary>
		public static EventHandler<WebSocketClient> WebSocketClientConnected;
		
	}
}