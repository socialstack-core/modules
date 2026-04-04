using Api.Configuration;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.WebSockets
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(10)]
	public partial class WebSocketService : AutoService
    {

		private readonly ContextService _contextService;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public WebSocketService(ContextService contextService, UserService userService)
		{
			_contextService = contextService;

			var _config = GetConfig<WebSocketServiceConfig>();

			Events.Service.AfterStart.AddEventListener(async (Context ctx, object s) => {

				var trackClients = false;

				if (_config.TrackAllClients.HasValue)
				{
					trackClients = _config.TrackAllClients.Value;
				}
				#if DEBUG
				else
				{
					trackClients = true;
				}
				#endif

				if (trackClients)
				{
					_allClients = new ConcurrentDictionary<uint, WebSocketClient>();
				}

				// Start:
				await Start(userService);
				return s;

				// After contentSync has obtained an ID.
			}, 11);
		}

		private int _clientCount;

		/// <summary>
		/// Current client count.
		/// </summary>
		/// <returns></returns>
		public int GetClientCount()
		{
			return _clientCount;
		}

		/// <summary>
		/// True if all connected clients are being retained in a lookup.
		/// </summary>
		public bool TrackingAllClients => _allClients != null;

		private Server<WebSocketClient> wsServer;

		private ConcurrentDictionary<uint, WebSocketClient> _allClients;

		/// <summary>
		/// A set of all clients (only available if configured, or in debug mode).
		/// </summary>
		public ConcurrentDictionary<uint, WebSocketClient> AllClients => _allClients;

		/// <summary>
		/// The current websocket server.
		/// </summary>
		public Server<WebSocketClient> Server => wsServer;

		/// <summary>
		/// Sends the given message to all clients (requires track all to be enabled).
		/// </summary>
		/// <param name="message"></param>
		public async ValueTask SendToAll(Writer message)
		{
			var clients = AllClients;

			if (clients == null)
			{
				Log.Warn(LogTag, "You've specifically disabled TrackAllClients on websocket service. Unable to send a given message to all WS clients.");
				return;
			}

			foreach (var kvp in clients)
			{
				var client = kvp.Value;
				if (client != null)
				{
					await client.SendAsync(message);
				}
			}
		}

		/// <summary>
		/// Starts the ws service.
		/// </summary>
		public async ValueTask Start(UserService userService)
		{
			// In Edge mode, the ws server only acts as an opcode container.
			// It does not actually listen for requests itself - kestrel does that.
			var edgeMode = AppSettings.IsEdgeMode();

			wsServer = new Server<WebSocketClient>();

			if (!edgeMode)
			{
				// Otherwise start a public bolt server:
				var portNumber = AppSettings.GetInt32("WebsocketPort", AppSettings.GetInt32("Port", 5000) + 1);

				var unixFileIsActive = AppSettings.GetInt32("WebSocketUnixFileActive", 1);
				var wsFileName = AppSettings.GetString("WebSocketUnixFileName", "ws.sock");
				wsServer.UnixSocketFileName = unixFileIsActive == 0 || string.IsNullOrEmpty(wsFileName) ? null : wsFileName;
				wsServer.Port = portNumber;

				wsServer.AcceptWebsockets(false);
			}

			wsServer.OnConnected += async (WebSocketClient client) =>
			{

				if (_allClients != null)
				{
					_allClients[client.Id] = client;
				}

				_clientCount++;

				// Trigger connected event:
				await Events.WebSocket.Connected.Dispatch(client.Context, client);

			};

			Events.WebSocket.Disconnected.AddEventListener((Context context, WebSocketClient c) => {

				if (_allClients != null)
				{
					_allClients.Remove(c.Id, out _);
				}
	
				_clientCount--;

				return new ValueTask<WebSocketClient>(c);
			});

			/*
			wsServer.RegisterOpCode(5, async (Client client, GetMessage get) => {
				var context = client.Context;

				if (context == null)
				{
					return;
				}

				// Get the user:
				var uSvc = Services.Get<UserService>();
				var user = await uSvc.Get(context, 1, DataOptions.IgnorePermissions);

				var writer = Writer.GetPooled();
				writer.Start(null);

				await uSvc.ToJson(context, user, writer);

				client.Send(writer);
				writer.Release();
			});
			*/

			// Heartbeat from client
			wsServer.RegisterOpCode(1);

			// Wrapped JSON:
			var jsonMessageReader = new JsonMessageReader();
			jsonMessageReader.OpCode = wsServer.RegisterOpCode(2, async (Client c, JsonMessage msg) =>
			{
				var message = JsonConvert.DeserializeObject(msg.Json) as JObject;
				var client = c as WebSocketClient;

				var jToken = message["type"];

				if (jToken == null || jToken.Type != JTokenType.String)
				{
					// Just ignore this message.
					return;
				}

				var type = jToken.Value<string>();
				var handled = false;
				
				switch (type)
				{
					case "Auth":
						handled = true;
						jToken = message["token"];

						if (jToken == null || jToken.Type != JTokenType.String)
						{
							// Just ignore this message.
							return;
						}

						var authToken = jToken.Value<string>();

						// Load the context:
						var ctx = new Context();
						await _contextService.Get(authToken, ctx);
						await client.SetContext(ctx);
						break;
				}

				if (!handled)
				{
					await Events.WebSocketMessage.Dispatch(client.Context, message, client, type);
				}

			}, jsonMessageReader);

			// Add any other events:
			await Events.WebSocket.BeforeStart.Dispatch(new Context(), wsServer);

			if (edgeMode)
			{
				// Opcodes only.
				wsServer.StartOpcodes();
			}
			else
			{
				// Start it:
				wsServer.Start();
			}
		}

	}
}
