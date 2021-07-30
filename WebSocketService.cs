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
		private readonly int _userContentTypeId;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public WebSocketService(ContextService contextService)
		{
			_contextService = contextService;
			_userContentTypeId = ContentTypes.GetId(typeof(User));

			Start();
		}

		private Server<WebSocketClient> wsServer;

		/// <summary>
		/// Starts the ws service.
		/// </summary>
		public void Start()
		{
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

#warning disabled

				/*
				var type = jToken.Value<string>();
				var handled = false;
				JArray jArray = null;
				string name;
				int id;
				JObject filter;

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
						// Adds a single listener with an optional filter. id required.
						name = message["n"].Value<string>();
						id = message["i"].Value<int>();
						filter = message["f"] as JObject; // Can be null, but is a complete filter incl. {where:..}

						var listeners = GetTypeListener(name);

						if (listeners != null)
						{
							// Add the listener now:
							client.AddEventListener(
								listeners,
								filter,
								id
							);
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
							name = jo["n"].Value<string>();
							listeners = GetTypeListener(name);

							if (listeners != null)
							{
								id = jo["i"].Value<int>();
								filter = jo["f"] as JObject; // Can be null, but is a complete filter incl. {where:..}

								// Add the listener now:
								client.AddEventListener(listeners, filter, id);
							}
						}

						break;
					case "-":
						// Removes a listener identified by its ID.
						handled = true;
						jToken = message["i"];

						if (jToken == null || jToken.Type != JTokenType.Integer)
						{
							// Just ignore this message.
							return;
						}

						// Get the listener by ID:
						var listener = client.GetById(jToken.Value<int>());

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
				*/

			}, jsonMessageReader);

			// Start it:
			wsServer.Start();
		}

	}
}
