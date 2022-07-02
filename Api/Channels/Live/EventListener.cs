using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Contexts;
using System.Threading;
using Api.WebSockets;
using Api.Eventing;


namespace Api.ChannelMessages
{

	/// <summary>
	/// Hooks up live websocket support for channel messages.
	/// Separate such that the live feature can be disabled by just deleting this file.
	/// </summary>
	[EventListener]
	public class LiveEventListener
	{
		
		private IWebSocketService _websocketService;
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public LiveEventListener()
	{	
			Events.ChannelMessageAfterCreate.AddEventListener(async (Context context, ChannelMessage message) => {

				if (_websocketService == null)
				{
					_websocketService = Services.Get<IWebSocketService>();
				}

				// Send via the websocket service:
				await _websocketService.Send(
					new WebSocketMessage<ChannelMessage>() {
						Type = "ChannelMessageCreate?ChannelId=" + message.ChannelId,
						Entity = message
					}
				);

				return message;

			}, 20);
		}

	}
}
