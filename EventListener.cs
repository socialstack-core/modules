using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Contexts;
using System.Threading;

namespace Api.WebSockets
{

	/// <summary>
	/// Listens for the configure event so it can add websocket support.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		private uint Id = 1;
		private WebSocketService _websocketService;
		private object connector = new object();

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up the configure app method:
			Api.Startup.WebServerStartupInfo.OnConfigureApplication += (IApplicationBuilder app) => {

				// Fire up a websocket server:
				app.UseWebSockets();
				
				app.Use(async (http, next) =>
				{
					if (!http.WebSockets.IsWebSocketRequest)
					{
						await next();
						return;
					}

					// Get the websocket:
					var websocket = await http.WebSockets.AcceptWebSocketAsync();
					
					// We've got a new websocket client.
					if(_websocketService == null){
						_websocketService = Api.Startup.Services.Get<WebSocketService>();
					}

					uint id;

					lock (connector)
					{
						id = Id++;

						if (Id == uint.MaxValue)
						{
							Id = 0;
						}
					}

					var context = http.Request.GetContext();

					var client = new WebSocketClient()
					{
						Id = id,
						Socket = websocket
					};

					if (context != null)
					{
						client.Context = context;
					}
					else
					{
						// new anon context:
						client.Context = new Context();
					}

					try
					{
						await _websocketService.ConnectedClient(client);
					}
					catch (Exception e)
					{
						throw e;
					}
					finally {
						await client.OnDisconnected(_websocketService);
					}
				});
			};
		}

	}
}
