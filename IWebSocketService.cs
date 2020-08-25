using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.WebSockets
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IWebSocketService
	{
		/// <summary>
		/// Called when a new client has connected and it's time to add them.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		Task ConnectedClient(WebSocketClient client);

		/// <summary>
		/// Sends a message via websockets. Only sends to users who are actively listening for a message with this type (provided they're able to do so), or to a specific user if given.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="userId">Optional user to send to.</param>
		/// <returns></returns>
		Task Send(WebSocketMessage message, int? userId);

		/// <summary>
		/// Sends the given entity and the given method name which states what has happened with this object. Typically its 'update', 'create' or 'delete'.
		/// It's sent to users who are listening to (and permitted to do so) objects of this type, unless you give a specific userId.
		/// </summary>
		void Send(Context context, object entity, string methodName, int? userId = null);
		
		
		/// <summary>
		/// Websocket clients by user ID.
		/// </summary>
		Dictionary<int, UserWebsocketLinks> UserListeners { get; }
		
		
	}
}
