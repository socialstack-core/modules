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
		/// Sends a message via websockets. Only sends to users who are actively listening for a message with this type.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="capability"></param>
		/// <param name="capArgs"></param>
		/// <returns></returns>
		Task Send(WebSocketMessage message, Capability capability, object[] capArgs);

	}
}
