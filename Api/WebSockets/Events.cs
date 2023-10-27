using Api.WebSockets;
using Api.Permissions;
using Newtonsoft.Json.Linq;
using Api.SocketServerLibrary;
using Api.Users;

namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		/// <summary>
		/// Called when a wrapped JSON message is received of a non-core type.
		/// </summary>
		public static EventHandler<JObject, WebSocketClient, string> WebSocketMessage;

		/// <summary>
		/// Event group for a bundle of events on AutoServices.
		/// </summary>
		public static Api.WebSockets.WebSocketEventGroup WebSocket;

	}

}

namespace Api.WebSockets
{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public class WebSocketEventGroup : Eventing.EventGroupCore<AutoService, uint>
	{

		/// <summary>
		/// Before a WS server is about to start. Use this to add custom opcodes.
		/// </summary>
		public Api.Eventing.EventHandler<Server<WebSocketClient>> BeforeStart;
		
		/// <summary>
		/// Called when a network room is being loaded. The RoomTypeId must be set on it.
		/// </summary>
		public Api.Eventing.EventHandler<NetworkRoomSet> SetUniqueTypeId;

		/// <summary>
		/// A WS connected. This is before their identity is known. The context is always an anonymous one here.
		/// </summary>
		public Api.Eventing.EventHandler<WebSocketClient> Connected;

		/// <summary>
		/// WebSocket user login.
		/// </summary>
		public Api.Eventing.EventHandler<WebSocketClient, NetworkRoom<User, uint, uint>> AfterLogin;

		/// <summary>
		/// WebSocket user logout.
		/// </summary>
		public Api.Eventing.EventHandler<WebSocketClient, NetworkRoom<User, uint, uint>> AfterLogout;

		/// <summary>
		/// A WS disconnected.
		/// </summary>
		public Api.Eventing.EventHandler<WebSocketClient> Disconnected;

	}

}