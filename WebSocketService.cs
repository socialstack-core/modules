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

namespace Api.WebSockets
{
	/// <summary>
	/// Handles creations of galleries - containers for image uploads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class WebSocketService : IWebSocketService
    {

		private readonly IContextService _contextService;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public WebSocketService(IContextService contextService)
		{
			_contextService = contextService;

			// Collect all IAmLive types.

			var loadEvents = Events.FindByType(typeof(IAmLive), null, EventPlacement.After);

			foreach (var typeEvent in loadEvents)
			{
				if(typeEvent.Verb != "Create" && typeEvent.Verb != "Update" && typeEvent.Verb != "Delete"){
					continue;
				}

				var method = typeEvent.Verb.ToLower();

				// We'll send this event to particular users *if* they have the load capability.
				string capName = (typeEvent.EntityName + "_load").ToLower();
				Capabilities.All.TryGetValue(capName, out Capability capability);
				
				typeEvent.AddEventListener(async (Context context, object[] args) => {
					
					if(args == null || args.Length == 0){
						return null;
					}

					// Send via the websocket service:
					Task.Run(async () =>
					{

						await Send(
							new WebSocketEntityMessage() {
								Type = typeEvent.EntityName,
								Method = method,
								Entity = args[0],
								By = context == null ? 0 : context.UserId
							},
							capability,
							null,
							args
						);
						
					});
					
					return args[0];
				
				// The 50 means every other handler has a chance to run before this does.
				}, 50);
				
			}
			
		}
		
		/// <summary>
		/// Websocket clients listening by event type.
		/// Type is typically of the form EventName?query&amp;encoded&amp;filter.
		/// For example, if someone is listening to chat messages in a particular channel, it's:
		/// ChannelMessageCreate?ChannelId=123
		/// </summary>
		public Dictionary<string, WebSocketTypeListeners> ListenersByType = new Dictionary<string, WebSocketTypeListeners>();
		
		/// <summary>
		/// Websocket clients by user ID.
		/// </summary>
		public Dictionary<int, UserWebsocketLinks> ListenersByUserId = new Dictionary<int, UserWebsocketLinks>();
		
		/// <summary>
		/// Websocket clients by user ID.
		/// </summary>
		public Dictionary<int, UserWebsocketLinks> UserListeners => ListenersByUserId;
		
		/// <summary>
		/// Gets type listener by the name. Optionally creates it if it didn't exist.
		/// </summary>
		private WebSocketTypeListeners GetTypeListener(string name, bool create){
			
			if(!ListenersByType.TryGetValue(name, out WebSocketTypeListeners listener) && create){
				
				lock(ListenersByType){
					listener = new WebSocketTypeListeners();
					ListenersByType[name] = listener;
				}
			}
			
			return listener;
		}
		
		/// <summary>
		/// Sends the given entity and the given method name which states what has happened with this object. Typically its 'update', 'create' or 'delete'.
		/// It's sent to everyone who can view entities of this type, unless you give a specific userId.
		/// </summary>
		public void Send(Context context, object entity, string methodName, int? toUserId = null)
		{
			
			if(entity == null)
			{
				return;
			}
			
			var typeName = entity.GetType().Name;
			
			// We'll send this event to particular users *if* they have the load capability.
			string capName = (typeName + "_load").ToLower();
			if(!Capabilities.All.TryGetValue(capName, out Capability capability))
			{
				// Can't send this entity.
				return;
			}
			
			// Send via the websocket service:
			Task.Run(async () =>
			{

				await Send(
					new WebSocketEntityMessage() {
						Type = typeName,
						Method = methodName,
						Entity = entity,
						By = context == null ? 0 : context.UserId
					},
					capability,
					toUserId,
					new object[] {
						entity
					}
				);
				
			});
			
		}
		
		/// <summary>
		/// Updates the user client set.
		/// </summary>
		private void ChangeUserSet(WebSocketClient client){
			
			var uId = client.Context.UserId;
			
			lock(ListenersByUserId){
				
				if(!ListenersByUserId.TryGetValue(uId, out UserWebsocketLinks set))
				{
					set = new UserWebsocketLinks(uId);
				}
				
				// Add the client to the set:
				set.Add(client, ListenersByUserId);
			}
			
		}
		
		/// <summary>
		/// Called when a new client has connected and it's time to add them.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public async Task ConnectedClient(WebSocketClient client){

			// Add to user lookup (may be multiple)
			
			if(client.Context != null && client.Context.UserId != 0){
				
				// Add to user lookup.
				ChangeUserSet(client);
				
			}
			
			var token = CancellationToken.None;
			var buffer = new ArraySegment<byte>(new byte[4096]);
			var websocket = client.Socket;

			try
			{
				// Send connected event:
				await Events.WebSocketClientConnected.Dispatch(client.Context, client);
			
				// Whilst it's open, receive a message:
				while (websocket.State == WebSocketState.Open)
				{
					var received = await websocket.ReceiveAsync(buffer, token);
					
					if (received == null || received.MessageType != WebSocketMessageType.Text)
					{
						// Ignore binary messages
						continue;
					}
					
					// Get the payload:
					var requestJson = Encoding.UTF8.GetString(buffer.Array,
						buffer.Offset,
						received.Count);
				
					JObject message = JsonConvert.DeserializeObject(requestJson) as JObject;

					var jToken = message["type"];

					if (jToken == null || jToken.Type != JTokenType.String) {
						// Just ignore this message.
						continue;
					}

					var type = jToken.Value<string>();
					
					switch(type){
						case "Add":
						case "AddEventListener":
							jToken = message["name"];

							if (jToken == null || jToken.Type != JTokenType.String)
							{
								// Just ignore this message.
								continue;
							}
							
							var evtName = jToken.Value<string>();

							// no-op if they're already listening to this event.
							var typeToListenTo = GetTypeListener(evtName, true);
							
							// Add the listener now:
							client.AddEventListener(typeToListenTo);
						break;
						case "Auth":
							jToken = message["token"];

							if (jToken == null || jToken.Type != JTokenType.String)
							{
								// Just ignore this message.
								continue;
							}

							var authToken = jToken.Value<string>();

							// Load the context:
							var ctx = _contextService.Get(authToken);

							if (ctx == null)
							{
								ctx = new Context();
							}
							
							var prevUserId = client.Context != null ? client.Context.UserId : 0;
							
							client.Context = ctx;
							
							if(ctx.UserId != prevUserId){
								// Update the user set it's in:
								ChangeUserSet(client);
							}
							
						break;
						case "Remove":
						case "RemoveEventListener":
							jToken = message["name"];

							if (jToken == null || jToken.Type != JTokenType.String)
							{
								// Just ignore this message.
								continue;
							}
							
							var typeToRemove = GetTypeListener(jToken.Value<string>(), false);

							if (typeToRemove != null)
							{
								// Remove it:
								client.RemoveEventListener(typeToRemove);
							}
						break;
						case "AddSet":
							jToken = message["names"];

							if (jToken == null || jToken.Type != JTokenType.Array)
							{
								// Just ignore this message.
								continue;
							}
							
							var jArray = jToken as JArray;
							
							foreach(var entry in jArray){
								var eName = entry.Value<string>();
								
								// Add the listener now:
								client.AddEventListener(GetTypeListener(eName, true));
							}
						break;
					}

				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
			
			
		}

		/// <summary>
		/// Sends the given message. Only sends to users who are actively listening out for this type.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="capability"></param>
		/// <param name="toUserId"></param>
		/// <param name="capArgs"></param>
		/// <returns></returns>
		public async Task Send(WebSocketMessage message, Capability capability, int? toUserId, object[] capArgs)
		{
			if (toUserId.HasValue)
			{
				// Sending to a specific user. These messages go out regardless of if the user was actually listening for them.
				// Note that if a capability is given, it can still reject them.
				if (ListenersByUserId.TryGetValue(toUserId.Value, out UserWebsocketLinks links))
				{
					await links.Send(message, capability, capArgs);
				}

				return;
			}

			var type = GetTypeListener(message.Type, false);

			if (type == null)
			{
				return;
			}

			await type.Send(message, capability, capArgs);
		}

	}
    
	/// <summary>
	/// A listener for a particular client in a particular message type.
	/// </summary>
	public class WebSocketTypeClient{
		
		/// <summary>Next one in the linked list.</summary>
		public WebSocketTypeClient Next;
		
		/// <summary>Previous one in the linked list.</summary>
		public WebSocketTypeClient Previous;
		
		/// <summary>
		/// The client this is for.
		/// </summary>
		public WebSocketClient Client;
		
		/// <summary>
		/// Removes this websocket type client from the listener set.
		/// Use WebSocketClient.RemoveEventListener instead.
		/// </summary>
		internal void RemoveFrom(WebSocketTypeListeners listeners){
			
			lock(listeners){
			
				if(Next == null){
					listeners.Last = Previous;
				}else{
					Next.Previous = Previous;
				}
				
				if(Previous == null){
					listeners.First = Next;
				}else{
					Previous.Next = Next;
				}
				
			}
		}
		
	}
	
	/// <summary>
	/// Holds a block of listeners for a particular message type.
	/// </summary>
	public class WebSocketTypeListeners{

		private static JsonSerializerSettings _serializerSettings;

		/// <summary>First one in the linked list.</summary>
		public WebSocketTypeClient First;
		
		/// <summary>Last one in the linked list.</summary>
		public WebSocketTypeClient Last;
		
		
		/// <summary>
		/// Adds a type client to this type listener set. 
		/// Use WebSocketClient.AddEventListener instead.
		/// </summary>
		internal void Add(WebSocketTypeClient typeClient){
			
			typeClient.Next = null;
			
			lock(this){
				if(Last == null){
					typeClient.Previous = null;
					First = typeClient;
					Last = typeClient;
				}else{
					typeClient.Previous = Last;
					Last.Next = typeClient;
					Last = typeClient;
				}
			}
			
		}
		
		/// <summary>
		/// Sends a message to all listeners for this type by encoding it as JSON.
		/// </summary>
		public async Task Send(WebSocketMessage message, Capability capability, object[] capArgs)
		{

			if (_serializerSettings == null)
			{
				_serializerSettings = new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				};
			}

			// Get the bytes of the message:
            var jsonMessage = JsonConvert.SerializeObject(message, _serializerSettings);
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, capability, capArgs);
		}
		
		/// <summary>
		/// Sends a JSON message to all listeners for this type.
		/// </summary>
		public async Task Send(string jsonMessage, Capability capability, object[] capArgs)
		{
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, capability, capArgs);
		}
		
		/// <summary>
		/// Sends a raw binary message to all listeners for this type.
		/// </summary>
		public async Task Send(byte[] message, Capability capability, object[] capArgs){
			
			var arSegment = new ArraySegment<Byte>(message);
			
            // Send it now:
			var current = First;
			
			while(current != null){

				if (capability != null)
				{
					var ctx = current.Client.Context;

					if (!await ctx.Role.IsGranted(capability, ctx, capArgs))
					{
						// Skip this user
						current = current.Next;
						continue;
					}
				}

				await current.Client.Socket.SendAsync(
					arSegment,
					WebSocketMessageType.Text,
					true,
					CancellationToken.None
				);
				
				current = current.Next;
			}
		}

	}
	
	/// <summary>
	/// A message to send via websockets.
	/// </summary>
	public class WebSocketMessage{
		/// <summary>
		/// The type of this message. Usually the same as a nearby event, e.g. "ChannelMessageCreate".
		/// </summary>
		public string Type;
	}

	/// <summary>
	/// A message to send via websockets with an entity of a particular type.
	/// </summary>
	public class WebSocketEntityMessage : WebSocketMessage{
		
		/// <summary>
		/// User ID of the person who raised this event.
		/// </summary>
		public int By;
		/// <summary>
		/// The entity to send in this message.
		/// E.g. a newly created chat message.
		/// </summary>
		public object Entity;
		/// <summary>
		/// Lowercase, "update", "delete" or "create".
		/// </summary>
		public string Method;
	}
	
	/// <summary>
	/// All a particular user's websocket links.
	/// </summary>
	public class UserWebsocketLinks{

		private static JsonSerializerSettings _serializerSettings;

		/// <summary>
		/// User ID.
		/// </summary>
		public int Id;
		
		/// <summary>
		/// First link in their set.
		/// </summary>
		public WebSocketClient First;
		
		/// <summary>
		/// Last link in their set.
		/// </summary>
		public WebSocketClient Last;
		
		/// <summary>
		/// Creates a new set of user specific clients for the given user ID.
		/// </summary>
		/// <param name="id"></param>
		public UserWebsocketLinks(int id){
			Id = id;
		}
		
		/// <summary>
		/// Adds the given client to the user set.
		/// </summary>
		public void Add(WebSocketClient client, Dictionary<int, UserWebsocketLinks> all){
			
			if(client.UserSet != null){
				client.RemoveFromUserSet(all);
			}
			
			client.UserNext = null;
			client.UserSet = this;
			
			lock(this){
				if(Last == null){
					client.UserPrevious = null;
					First = client;
					Last = client;
				}else{
					client.UserPrevious = Last;
					Last.UserNext = client;
					Last = client;
				}
			}
			
		}
		
		/// <summary>
		/// Sends a message to all of a user's clients by encoding it as JSON.
		/// </summary>
		public async Task Send(WebSocketMessage message, Capability capability, object[] capArgs)
		{

			if (_serializerSettings == null)
			{
				_serializerSettings = new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				};
			}

			// Get the bytes of the message:
			var jsonMessage = JsonConvert.SerializeObject(message, _serializerSettings);
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, capability, capArgs);
		}

		/// <summary>
		/// Sends a JSON message to all of a user's clients.
		/// </summary>
		public async Task Send(string jsonMessage, Capability capability, object[] capArgs)
		{
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, capability, capArgs);
		}

		/// <summary>
		/// Sends a raw binary message to all of a user's clients.
		/// </summary>
		public async Task Send(byte[] message, Capability capability, object[] capArgs)
		{
			var arSegment = new ArraySegment<Byte>(message);

			// Send it now:
			var current = First;

			while (current != null)
			{

				if (capability != null)
				{
					var ctx = current.Context;

					if (!await ctx.Role.IsGranted(capability, ctx, capArgs))
					{
						// Skip this client
						current = current.UserNext;
						continue;
					}
				}

				await current.Socket.SendAsync(
					arSegment,
					WebSocketMessageType.Text,
					true,
					CancellationToken.None
				);

				current = current.UserNext;
			}
		}

	}

	/// <summary>
	/// A connected websocket client.
	/// </summary>
	public class WebSocketClient{
		
		/// <summary>
		/// If this client is in a user set, the set it is in.
		/// </summary>
		public UserWebsocketLinks UserSet;
		/// <summary>
		/// Next in the user's set of websocket clients.
		/// </summary>
		public WebSocketClient UserNext;
		/// <summary>
		/// Previous in the user's set of websocket clients.
		/// </summary>
		public WebSocketClient UserPrevious;
		
		/// <summary>
		/// The underlying socket.
		/// </summary>
		public System.Net.WebSockets.WebSocket Socket;
		
		/// <summary>
		/// The context for this user. Survives much longer than a typical context does.
		/// </summary>
		public Context Context;
		
		/// <summary>
		/// All the message types that this client is listening to.
		/// </summary>
		public Dictionary<WebSocketTypeListeners, WebSocketTypeClient> TypeListeners = new Dictionary<WebSocketTypeListeners, WebSocketTypeClient>();
		
		/// <summary>
		/// Adds a listener for messages of the given type. Returns null if it was already being listened to.
		/// </summary>
		public WebSocketTypeClient AddEventListener(WebSocketTypeListeners type){
			
			if(TypeListeners.TryGetValue(type, out WebSocketTypeClient client)){
				// Already listening to this one.
				return client;
			}
			
			client = new WebSocketTypeClient();
			client.Client = this;
			
			// Add to set:
			TypeListeners[type] = client;
			
			// Add this client object to the type itself:
			type.Add(client);
			
			return client;
		}
		
		/// <summary>
		/// Removes this client from the user set.
		/// </summary>
		public void RemoveFromUserSet(Dictionary<int, UserWebsocketLinks> all){
			if(UserSet == null){
				return;
			}
			
			lock(UserSet){
			
				if(UserNext == null){
					UserSet.Last = UserPrevious;
				}else{
					UserNext.UserPrevious = UserPrevious;
				}
				
				if(UserPrevious == null){
					UserSet.First = UserNext;
				}else{
					UserPrevious.UserNext = UserNext;
				}
				
			}
			
			if(UserSet.First == null){
				// Remove from the overall lookup now.
				lock(all){
					all.Remove(UserSet.Id);
				}
			}
			
			UserSet = null;
		}
		
		/// <summary>
		/// Convenience one off send method.
		/// </summary>
		public async Task Send(byte[] message){
			var arSegment = new ArraySegment<Byte>(message);
			
			await Socket.SendAsync(
				arSegment,
				WebSocketMessageType.Text,
				true,
				CancellationToken.None
			);
		}
		
		/// <summary>
		/// Called when this client disconnects. Removes all their type listeners.
		/// </summary>
		public void OnDisconnected(IWebSocketService service)
		{
			// Remove from user set:
			RemoveFromUserSet(service.UserListeners);
			
			// Remove all:
			foreach (var kvp in TypeListeners)
			{
				// Remove it:
				kvp.Value.RemoveFrom(kvp.Key);
			}
			TypeListeners.Clear();
		}

		/// <summary>
		/// Removes listener for messages of the given type.
		/// </summary>
		public void RemoveEventListener(WebSocketTypeListeners type){
			
			if(!TypeListeners.TryGetValue(type, out WebSocketTypeClient client)){
				return;
			}
			
			// Remove from lookup:
			TypeListeners.Remove(type);
			
			// Remove this client value from the type too:
			client.RemoveFrom(type);
		}
		
	}
	
}
