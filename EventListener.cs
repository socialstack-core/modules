using Api.Configuration;
using Api.WebSockets;
using System;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Api.Startup;
using System.Text;
using Api.Eventing;
using Api.Contexts;
using Microsoft.Extensions.Configuration;

namespace Api.VersionChecker
{

	/// <summary>
	/// Listens for websocket clients, then tells them the versions of things.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// The constructed hello message.
		/// </summary>
		private byte[] HelloMessage;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			var configuration = AppSettings.GetSection("UiVersions").Get<VersionConfig>();
			
			if(configuration == null || configuration.web == 0){
				return;
			}
			
			var hello = new {
				type = "hello",
				all = true,
				versions = configuration
			};
			
			HelloMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(hello));
			
            Events.WebSocketClientConnected.AddEventListener(async (Context ctx, WebSocketClient client) => 
            {
				
				if(HelloMessage != null){
					await client.Send(HelloMessage);
				}

				return client;
            });
		}
	}
}
