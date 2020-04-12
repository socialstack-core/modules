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

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public WebSocketService(){
			
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
		public Dictionary<int, WebSocketClient> ListenersByUserId = new Dictionary<int, WebSocketClient>();
		
		
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
		/// It's sent to everyone who can view entities of this type.
		/// </summary>
		public void Send(Context context, object entity, string methodName)
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
					new object[] {
						entity
					}
				);
				
			});
			
		}
		
		/// <summary>
		/// Called when a new client has connected and it's time to add them.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public async Task ConnectedClient(WebSocketClient client){

			// Add to user lookup (may be multiple)
			// ListenersByUserId[client.UserId] = client;

			var token = CancellationToken.None;
			var buffer = new ArraySegment<byte>(new byte[4096]);
			var websocket = client.Socket;

			try
			{
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
		/// <param name="capArgs"></param>
		/// <returns></returns>
		public async Task Send(WebSocketMessage message, Capability capability, object[] capArgs)
		{
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
	/// A connected websocket client.
	/// </summary>
	public class WebSocketClient{

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
		/// Called when this client disconnects. Removes all their type listeners.
		/// </summary>
		public void RemoveAll()
		{
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
