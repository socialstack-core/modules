using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.WebSockets;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Startup
{
	
	/// <summary>
	/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
	/// Not required to use these - you can also just directly use ControllerBase if you want.
	/// Like AutoService this isn't in a namespace due to the frequency it's used.
	/// </summary>
	public partial class StdOutController : AutoController
	{
		
		/// <summary>
		/// Gets the latest number of websocket clients.
		/// </summary>
		[HttpGet("clients")]
		public WebsocketClientInfo GetWsClientCount(Context context)
		{
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("monitoring_stdout", context);
			}

			int count = Services.Get<WebSocketService>().GetClientCount();

			return new WebsocketClientInfo() {
				Clients = count
			};
		}
		
		
	}

	/// <summary>
	/// WS client info.
	/// </summary>
	public struct WebsocketClientInfo
	{
		/// <summary>
		/// Client count.
		/// </summary>
		public int Clients;
	}

}