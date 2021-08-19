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
using Api.ContentSync;

namespace Api.WebSockets
{
	/// <summary>
	/// Handles creations of galleries - containers for image uploads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(11)]
	public partial class WebSocketService : AutoService
    {

		private readonly ContextService _contextService;
		private readonly ContentSyncService _contentSync;
		private readonly int _userContentTypeId;
		/// <summary>
		/// The set of personal rooms.
		/// </summary>
		public NetworkRoomSet<User, uint, uint> PersonalRooms;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public WebSocketService(ContextService contextService, UserService userService, ContentSyncService contentSync)
		{
			_contextService = contextService;
			_contentSync = contentSync;
			_userContentTypeId = ContentTypes.GetId(typeof(User));

			Events.Service.AfterStart.AddEventListener(async (Context ctx, object s) => {

				// Start:
				await Start(userService, contentSync);
				return s;

				// After contentSync has obtained an ID.
			}, 11);
		}

		private Server<WebSocketClient> wsServer;

		/// <summary>
		/// Sends the given message to the given user, on all their connected devices.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="message"></param>
		public void SendToUser(uint id, Writer message)
		{
			// Send to the given room:
			PersonalRooms.Send(id, message);
		}

		/// <summary>
		/// Starts the ws service.
		/// </summary>
		public async ValueTask Start(UserService userService, ContentSyncService contentSync)
		{
			// Create personal room set:
			var personalRoomMap = await MappingTypeEngine.GetOrGenerate(
					userService,
					Services.Get<ClusteredServerService>(),
					"PersonalRoomServers"
				) as MappingService<uint, uint>;

			// Scan the mapping to purge any entries for this server:
			await personalRoomMap.DeleteByTarget(new Context(), contentSync.ServerId, DataOptions.IgnorePermissions);

			PersonalRooms = new NetworkRoomSet<User, uint, uint>(userService, personalRoomMap, contentSync);

			// Start public bolt server:
			var portNumber = AppSettings.GetInt32("WebsocketPort", AppSettings.GetInt32("Port", 5000) + 1);

			wsServer = new Server<WebSocketClient>();

			wsServer.Port = portNumber;

			wsServer.AcceptWebsockets(false);

			wsServer.OnConnected += async (WebSocketClient client) => {

				// Trigger connected event:
				await Events.WebSocket.Connected.Dispatch(client.Context, client);

			};

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

						await _contentSync.RegisterRoomClient(typeName, customId, roomId, client);

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

							customId = jo["ci"].Value<uint>();
							roomId = jo["id"].Value<ulong>();

							// Add the client now:
							await _contentSync.RegisterRoomClient(typeName, customId, roomId, client);
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

						// Get the listener by ID:
						var listener = client.GetRoomById(jToken.Value<uint>());

						if (listener != null)
						{
							listener.Remove();
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

	}
}
