using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Users;
using System.Threading;
using System;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Api.Startup;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using Api.SocketServerLibrary;
using Api.Configuration;
using System.Collections.Concurrent;

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
		/// The set of personal rooms.
		/// </summary>
		public NetworkRoomSet<User, uint, uint> PersonalRooms;

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

		private Server<WebSocketClient> wsServer;

		private ConcurrentDictionary<uint, WebSocketClient> _allClients;

		/// <summary>
		/// A set of all clients (only available if configured, or in debug mode).
		/// </summary>
		public ConcurrentDictionary<uint, WebSocketClient> AllClients => _allClients;

		/// <summary>
		/// Starts the ws service.
		/// </summary>
		public async ValueTask Start(UserService userService)
		{
			// Start public bolt server:
			var portNumber = AppSettings.GetInt32("WebsocketPort", AppSettings.GetInt32("Port", 5000) + 1);

			wsServer = new Server<WebSocketClient>();

			var unixFileIsActive = AppSettings.GetInt32("WebSocketUnixFileActive", 1);
			var wsFileName = AppSettings.GetString("WebSocketUnixFileName", "ws.sock");
			wsServer.UnixSocketFileName = unixFileIsActive == 0 || string.IsNullOrEmpty(wsFileName) ? null : wsFileName;
			wsServer.Port = portNumber;

			wsServer.AcceptWebsockets(false);

			wsServer.OnConnected += async (WebSocketClient client) => {

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
				JArray jArray = null;
				string typeName;
				JObject filt;
				uint customId;
				ulong roomId;

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
						var ctx = await _contextService.Get(authToken);

						if (ctx == null)
						{
							ctx = new Context();
						}
						await client.SetContext(ctx);
						break;
					case "+":
						// Adds a single listener with an optional filter. custom id required.
						typeName = message["n"].Value<string>();
						customId = message["ci"].Value<uint>();
						roomId = message["id"].Value<ulong>();
						filt = message["f"] as JObject;

						if (customId != 0)
						{
							await RegisterRoomClient(typeName, customId, roomId, client, filt);
						}

						break;
					case "+*":
						// Add a set of listeners with filters. Usually happens after the websocket disconnected. id for each required.

						handled = true;
						jToken = message["set"];

						if (jToken == null || jToken.Type != JTokenType.Array)
						{
							// Just ignore this message.
							return;
						}

						jArray = jToken as JArray;

						foreach (var entry in jArray)
						{
							var jo = entry as JObject;
							typeName = jo["n"].Value<string>();
							filt = jo["f"] as JObject;

							customId = jo["ci"].Value<uint>();
							roomId = jo["id"].Value<ulong>();

							if (customId != 0)
							{
								// Add the client now:
								await RegisterRoomClient(typeName, customId, roomId, client, filt);
							}
						}

						break;
					case "-":
						// Removes a listener identified by its ID.
						handled = true;
						jToken = message["ci"];

						if (jToken == null || jToken.Type != JTokenType.Integer)
						{
							// Just ignore this message.
							return;
						}

						var cId = jToken.Value<uint>();

						if (cId != 0)
						{
							// Get the listener by ID:
							var listener = client.GetRoomById(cId);

							if (listener != null)
							{
								listener.Remove();
							}
						}

						break;
				}

				if (!handled)
				{
					await Events.WebSocketMessage.Dispatch(client.Context, message, client, type);
				}

			}, jsonMessageReader);

			// Add any other events:
			await Events.WebSocket.BeforeStart.Dispatch(new Context(), wsServer);

			// Start it:
			wsServer.Start();
		}

		/// <summary>
		/// Gets meta by the given lowercase name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public NetworkRoomTypeMeta GetMeta(string name)
		{
			RemoteTypes.TryGetValue(name, out NetworkRoomTypeMeta meta);
			return meta;
		}

		/// <summary>
		/// The registered remote types in this websocket service. These types are things that end users can tune into updates from.
		/// </summary>
		public ConcurrentDictionary<string, NetworkRoomTypeMeta> RemoteTypes = new ConcurrentDictionary<string, NetworkRoomTypeMeta>();

		/// <summary>
		/// Forcefully empties the room of the given type.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="roomId"></param>
		public void EmptyRoomLocally(string typeName, ulong roomId)
		{
			// First, get the type meta:
			if (RemoteTypes.TryGetValue(typeName, out NetworkRoomTypeMeta meta))
			{
				// Ok - the type exists.
				// Which room are we going for?
				var room = meta.GetOrCreateRoom(roomId);

				if (room != null)
				{
					room.EmptyLocally();
				}
			}
		}

		/// <summary>
		/// Adds a network room client.
		/// </summary>
		/// <param name="typeName"></param>
		/// <param name="customId"></param>
		/// <param name="roomId"></param>
		/// <param name="client"></param>
		/// <param name="filter"></param>
		public async ValueTask<UserInRoom> RegisterRoomClient(string typeName, uint customId, ulong roomId, WebSocketClient client, JObject filter = null)
		{
			// First, get the type meta:
			if (RemoteTypes.TryGetValue(typeName, out NetworkRoomTypeMeta meta))
			{
				// Ok - the type exists.
				// Which room are we going for?
				var room = meta.GetOrCreateRoom(roomId);

				if (room != null)
				{
					FilterBase perm = null;

					if (!meta.IsMapping)
					{
						// Get perm:
						perm = client.Context.Role.GetGrantRule(meta.LoadCapability);

						if (perm == null)
						{
							// They have no visibility of this type - do nothing.
							return null;
						}

						// Otherwise, ensure the cap is ready:
						if (perm.RequiresSetup)
						{
							await perm.Setup();
						}
					}

					return await room.Add(client, customId, perm, filter);
				}
			}
			
			return null;
		}

	}
}
