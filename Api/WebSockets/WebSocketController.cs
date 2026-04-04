using Api.Contexts;
using Api.ErrorLogging;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Api.WebSockets;

/// <summary>
/// The websocket controller which receives a Kestrel ws connection and then forwards it on.
/// </summary>
public partial class WebSocketController : AutoController
{

	/// <summary>
	/// The main live ws connection.
	/// </summary>
	[HttpGet("live-websocket")]
	[HttpPost("live-websocket")]
	[HttpConnect("live-websocket")]
	public async ValueTask LiveWebSocket(HttpContext httpContext)
	{
		if (!httpContext.WebSockets.IsWebSocketRequest)
		{
			return;
		}

		var wsService = Services.Get<WebSocketService>();

		if (wsService == null || wsService.Server == null)
		{
			return;
		}

		var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();

		var wsServer = wsService.Server;
		var client = new KestrelWebSocketClient(webSocket) {
			Server = wsServer
		};

		var context = new Context(0, 0, 0);
		await client.SetContext(context);
		await wsServer.OnConnected(client);

		// Kestrel requires that the initial request task
		// must not complete unless the WS is done with.
		await client.ReceiveAsync();
	}
}