using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Api.Contexts;
using System.Threading;
using Api.Eventing;
using System.Threading.Tasks;
using Api.SocketServerLibrary;
using Api.WebSockets;

namespace Api.Huddles
{

	/// <summary>
	/// Listens for websocket events such that it can add its customised set of opcodes.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{

			Events.WebSocket.BeforeStart.AddEventListener((Context ctx, Server<WebSocketClient> server) => {
				if (server == null)
				{
					return new ValueTask<Server<WebSocketClient>>(server);
				}

				// WS service is definitely available at this point:
				var websocketService = Services.Get<WebSocketService>();

				server.RegisterOpCode(40, (Client client, HuddleRing ring) => {
					// huddle ring
					// Forward to target user, adding this users ID (if they have one).
					if (client.Context != null && client.Context.UserId != 0 && ring.UserId != 0)
					{
						var callee = ring.UserId;

						// We'll largely forward the ring message, but replace the userID with the caller.
						ring.UserId = client.Context.UserId;

						var writer = ring.Write(41);

						websocketService.SendToUser(callee, writer);

						writer.Release();
					}

				});

				return new ValueTask<Server<WebSocketClient>>(server);
			});

		}

	}

	/// <summary>
	/// Pooled ring messages.
	/// </summary>
	public class HuddleRing : Message<HuddleRing>
	{
        /// <summary>
        /// The slug for the huddle
        /// </summary>
        public ustring HuddleSlug;
        
        /// <summary>
        /// Used to indicate the mode of this particular message.
        /// [1 = ring, 2 = accept, 3 = decline]
		/// </summary>
        public byte Mode;
       
		/// <summary>
		/// ID of the user to ring.
		/// </summary>
		public uint UserId;

	}

}