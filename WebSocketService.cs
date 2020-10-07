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
using Api.Results;

namespace Api.WebSockets
{
	/// <summary>
	/// Handles creations of galleries - containers for image uploads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class WebSocketService
    {

		private readonly ContextService _contextService;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public WebSocketService(ContextService contextService)
		{
			_contextService = contextService;

			// Collect all IAmLive types.

			Events.ServicesAfterStart.AddEventListener((Context ctx, object src) =>
			{
				Setup();
				return new ValueTask<object>(src);
			});
		}

		private void Setup()
		{
			var loadEvents = Events.FindByType(typeof(IAmLive), "Create", EventPlacement.After);
			
			var methodInfo = GetType().GetMethod("SetupForType");
			
			foreach (var typeEvent in loadEvents)
			{
				// Get the actual type. We use this to avoid Revisions etc as we're not interested in those here:
				var contentType = ContentTypes.GetType(typeEvent.EntityName);

				if (contentType == null)
				{
					continue;
				}
				
				// Invoke setup for type:
				var setupType = methodInfo.MakeGenericMethod(new Type[] {
					contentType
				});
				
				setupType.Invoke(this, new object[] {
					typeEvent.EntityName
				});
			}
			
		}

		/// <summary>
		/// Sets up a particular content type with websocket handlers.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="entityName"></param>
		public void SetupForType<T>(string entityName) where T:new()
		{
			// Invoked by reflection
			
			var evtGroup = Events.GetGroup<T>();
			
			// Mark as remote synced:
			Api.Startup.RemoteSync.Add(typeof(T));
			
			// Get the listener:
			var listener = GetTypeListener(typeof(T));

			if (listener == null)
			{
				return;
			}
			
			// On received is used when something came from another server:
			evtGroup.Received.AddEventListener((Context context, T obj, int action) => {

				if (obj == null)
				{
					return new ValueTask<T>(obj);
				}

				if (action == 1)
				{
					// Create
					_=listener.Send(new WebSocketEntityMessage()
					{
						Type = entityName,
						Method = "create",
						Entity = obj,
						By = context == null ? 0 : context.UserId
					});
				}
				else if (action == 2)
				{
					// Update
					_=listener.Send(new WebSocketEntityMessage()
					{
						Type = entityName,
						Method = "update",
						Entity = obj,
						By = context == null ? 0 : context.UserId
					});
				}
				else if (action == 3)
				{
					// Delete
					_=listener.Send(new WebSocketEntityMessage()
					{
						Type = entityName,
						Method = "delete",
						Entity = obj,
						By = context == null ? 0 : context.UserId
					});
				}

				return new ValueTask<T>(obj);
			}, 50);

			evtGroup.AfterCreate.AddEventListener((Context context, T obj) => {

				// Send without waiting:
				_ = listener.Send(new WebSocketEntityMessage()
				{
					Type = entityName,
					Method = "create",
					Entity = obj,
					By = context == null ? 0 : context.UserId
				});

				return new ValueTask<T>(obj);
			}, 50);

			evtGroup.AfterUpdate.AddEventListener((Context context, T obj) => {

				_=listener.Send(new WebSocketEntityMessage()
				{
					Type = entityName,
					Method = "update",
					Entity = obj,
					By = context == null ? 0 : context.UserId
				});

				return new ValueTask<T>(obj);
			}, 50);

			evtGroup.AfterDelete.AddEventListener((Context context, T obj) => {
				
				_= listener.Send(new WebSocketEntityMessage()
				{
					Type = entityName,
					Method = "delete",
					Entity = obj,
					By = context == null ? 0 : context.UserId
				});

				return new ValueTask<T>(obj);
			}, 50);

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
		private WebSocketTypeListeners GetTypeListener(string name)
		{
			var cType = ContentTypes.GetType(name);

			if (cType == null)
			{
				// Nope! Not a valid content type.
				Console.WriteLine("Attempted to listen to '" + name + "' but it doesn't exist.");
				return null;
			}

			return GetTypeListener(cType);
		}

		/// <summary>
		/// Gets type listener by the name. Optionally creates it if it didn't exist.
		/// </summary>
		private WebSocketTypeListeners GetTypeListener(Type type)
		{
			var name = type.Name;

			if (!ListenersByType.TryGetValue(name, out WebSocketTypeListeners listener)){

				lock (ListenersByType)
				{
					var cap = Capabilities.All[type.Name.ToLower() + "_list"];

					if (cap == null)
					{
						Console.WriteLine("'" + name + "' has no _list capability.");
						return null;
					}

					listener = new WebSocketTypeListeners()
					{
						Type = type,
						Capability = cap
					};

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
			
			// Send via the websocket service:
			_=Send(
				new WebSocketEntityMessage() {
					Type = typeName,
					Method = methodName,
					Entity = entity,
					By = context == null ? 0 : context.UserId
				},
				toUserId
			);
		}
		
		/// <summary>
		/// Updates the user client set.
		/// </summary>
		private async Task ChangeUserSet(WebSocketClient client){
			
			var uId = client.Context.UserId;
			UserWebsocketLinks set = null;

			lock (ListenersByUserId){
				
				if(!ListenersByUserId.TryGetValue(uId, out set))
				{
					set = new UserWebsocketLinks(uId);
					ListenersByUserId[uId] = set;
				}
				
			}

			// Add the client to the set:
			await set.Add(client);
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
				await ChangeUserSet(client);
				
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

					if (jToken == null || jToken.Type != JTokenType.String)
					{
						// Just ignore this message.
						continue;
					}

					var type = jToken.Value<string>();
					var handled = false;
					JArray jArray = null;
					string name;
					int id;
					JObject filter;
					WebSocketTypeListeners listeners;

					switch (type)
					{
						// Depreciated
						case "Add":
						// Depreciated
						case "AddEventListener":
							handled = true;
							jToken = message["name"];

							if (jToken == null || jToken.Type != JTokenType.String)
							{
								// Just ignore this message.
								continue;
							}

							var evtName = jToken.Value<string>();

							// no-op if they're already listening to this event.
							listeners = GetTypeListener(evtName);

							if (listeners == null)
							{
								// Reject this
								continue;
							}

							// Add the listener now:
							await client.AddEventListener(listeners, null);
							break;
						case "Auth":
							handled = true;
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

							if (ctx.UserId != prevUserId)
							{
								// Update the user set it's in:
								await ChangeUserSet(client);
							}

							break;
						// Depreciated
						case "Remove":
						// Depreciated
						case "RemoveEventListener":
							handled = true;
							jToken = message["name"];

							if (jToken == null || jToken.Type != JTokenType.String)
							{
								// Just ignore this message.
								continue;
							}

							listeners = GetTypeListener(jToken.Value<string>());

							if (listeners != null)
							{
								// Remove it:
								client.RemoveEventListener(listeners);
							}
							break;
						// Depreciated
						case "AddSet":
							handled = true;
							jToken = message["names"];

							if (jToken == null || jToken.Type != JTokenType.Array)
							{
								// Just ignore this message.
								continue;
							}

							jArray = jToken as JArray;

							foreach (var entry in jArray)
							{
								var eName = entry.Value<string>();

								// Add the listener now:
								listeners = GetTypeListener(eName);

								if (listeners != null)
								{
									await client.AddEventListener(listeners, null);
								}
							}
							break;
						case "+":
							// Adds a single listener with an optional filter. id required.
							name = message["n"].Value<string>();
							id = message["i"].Value<int>();
							filter = message["f"] as JObject; // Can be null, but is a complete filter incl. {where:..}

							listeners = GetTypeListener(name);

							if (listeners != null)
							{
								// Add the listener now:
								await client.AddEventListener(
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
								continue;
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
									await client.AddEventListener(listeners, filter, id);
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
								continue;
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

				}


			}
			catch (OperationCanceledException)
			{
				// Ok - happens when the server is shut down
			}
			catch (WebSocketException)
			{
				// This is ok - happens when the remote user disconnects abruptly.
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
			finally
			{
				await Events.WebSocketClientDisconnected.Dispatch(client.Context, client);
			}
			
		}

		/// <summary>
		/// Sends the given message. Only sends to users who are actively listening out for this type.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="toUserId"></param>
		/// <returns></returns>
		public async Task Send(WebSocketMessage message, int? toUserId)
		{
			if (toUserId.HasValue)
			{
				// Sending to a specific user. These messages go out regardless of if the user was actually listening for them.
				// Note that if a capability is given, it can still reject them.
				if (ListenersByUserId.TryGetValue(toUserId.Value, out UserWebsocketLinks links))
				{
					await links.Send(message);
				}

				return;
			}

			var type = GetTypeListener(message.Type);

			if (type != null)
			{
				await type.Send(message);
			}
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

		/// <summary>Next one in the client set linked list.</summary>
		public WebSocketTypeClient NextClient;
		
		/// <summary>Previous one in the client set linked list.</summary>
		public WebSocketTypeClient PreviousClient;
		
		/// <summary>
		/// The client this is for.
		/// </summary>
		public WebSocketClient Client;

		/// <summary>
		/// True if this node should just be skipped because it can't receive these updates anyway.
		/// </summary>
		public bool Skip;

		/// <summary>
		/// Client assigned numeric ID to identify this type client for removals.
		/// </summary>
		public int Id;

		/// <summary>
		/// The 'global' set of websocket listeners that this is in.
		/// </summary>
		public WebSocketTypeListeners TypeSet;

		/// <summary>
		/// The underlying filter to use.
		/// </summary>
		public FilterNode FilterNode;

		/// <summary>
		/// The raw filter object.
		/// </summary>
		private JObject FilterObject;

		/// <summary>
		/// Resolved values for fast use with Matches calls.
		/// </summary>
		public List<ResolvedValue> ResolvedValues;

		/// <summary>
		/// Set filter
		/// </summary>
		/// <param name="filterObj"></param>
		public async Task SetFilter(JObject filterObj)
		{
			// Note that filter can be null.
			FilterObject = filterObj;
			await SetupFilter();
		}

		/// <summary>
		/// Call this to setup the filter object again, pre-calculating permissions etc.
		/// This is used directly only when the socket is reauthenticated.
		/// </summary>
		public async Task SetupFilter()
		{
			// Create the filter:
			var filter = new Filter(FilterObject, TypeSet.Type);

			if (Client.Context == null)
			{
				Skip = true;
				return;
			}

			// Next, pre-inject the permissions system for the target system type.
			// It's the *_list capability that we're using.
			var role = Client.Context.Role;

			if (role == null)
			{
				Skip = true;
				return;
			}

			// Get the grant rule (a filter) for this role + capability:
			var rawGrantRule = role.GetGrantRule(TypeSet.Capability);
			var srcFilter = role.GetSourceFilter(TypeSet.Capability);

			// If it's outright rejected..
			if (rawGrantRule == null)
			{
				Skip = true;
				return;
			}

			// Otherwise, merge the user filter with the one from the grant system (if we need to).
			// Special case for the common true always node:
			if (!(rawGrantRule is FilterTrue))
			{
				// Both are set. Must combine them safely:
				filter = filter.Combine(rawGrantRule, srcFilter?.ParamValueResolvers);
			}

			Skip = false;

			// Next, construct the filter:
			FilterNode = filter.Construct();

			// Finally, we'll precalc the param values for this context:
			ResolvedValues = await filter.ResolveValues(Client.Context);
		}

		/// <summary>
		/// Removes this websocket type client from the listener sets.
		/// Use WebSocketClient.RemoveEventListener instead.
		/// </summary>
		internal void Remove()
		{
			// Remove from the parent client:
			if (Client != null)
			{
				lock (Client)
				{
					if (NextClient == null)
					{
						Client.LastClient = PreviousClient;
					}
					else
					{
						NextClient.PreviousClient = PreviousClient;
					}

					if (PreviousClient == null)
					{
						Client.FirstClient = NextClient;
					}
					else
					{
						PreviousClient.NextClient = NextClient;
					}
				}
				Client = null;
			}

			// Remove from the type set:
			if (TypeSet != null)
			{
				lock (TypeSet)
				{

					if (Next == null)
					{
						TypeSet.Last = Previous;
					}
					else
					{
						Next.Previous = Previous;
					}

					if (Previous == null)
					{
						TypeSet.First = Next;
					}
					else
					{
						Previous.Next = Next;
					}

				}

				TypeSet = null;
			}

		}
		
	}
	
	/// <summary>
	/// Holds a block of listeners for a particular message type.
	/// </summary>
	public class WebSocketTypeListeners{

		/// <summary>
		/// The content type. This must be an IAmLive type otherwise it won't raise events.
		/// </summary>
		public Type Type;

		/// <summary>
		/// The *_list capability.
		/// </summary>
		public Capability Capability;

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
			if (typeClient.TypeSet != null)
			{
				throw new Exception("Can't add a client twice");
			}

			typeClient.TypeSet = this;
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
		public async Task Send(WebSocketMessage message)
		{

			if (_serializerSettings == null)
			{
				_serializerSettings = new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				};
			}

			var entityMessage = message as WebSocketEntityMessage;

			// Get the bytes of the message:
            var jsonMessage = JsonConvert.SerializeObject(message, _serializerSettings);
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, entityMessage == null ? null : entityMessage.Entity);
		}
		
		/// <summary>
		/// Sends a JSON message to all listeners for this type.
		/// </summary>
		public async Task Send(string jsonMessage, object entity = null)
		{
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, entity);
		}
		
		/// <summary>
		/// Sends a raw binary message to all listeners for this type.
		/// </summary>
		public async Task Send(byte[] message, object entity = null)
		{
			
			var arSegment = new ArraySegment<Byte>(message);
			
            // Send it now:
			var current = First;
			
			while(current != null){

				if (entity != null && current.FilterNode != null)
				{
					if (!current.FilterNode.Matches(current.ResolvedValues, entity))
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
		/// The type of this message e.g. "ChannelMessage".
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
	public partial class UserWebsocketLinks{

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
		/// True if this set contains exactly 1 entry.
		/// </summary>
		public bool ContainsOne
		{
			get
			{
				return First != null && First == Last;
			}
		}

		/// <summary>
		/// Adds the given client to the user set.
		/// </summary>
		public async Task Add(WebSocketClient client){
			
			if(client.UserSet != null){
				// The null here avoids the set from being removed from the overall lookup
				// we don't want it to be as we're about to add something to it.
				await client.RemoveFromUserSet(null);
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

			await Events.WebSocketUserState.Dispatch(client.Context != null ? client.Context : new Context(), Id, this);
		}
		
		/// <summary>
		/// Sends a message to all of a user's clients by encoding it as JSON.
		/// </summary>
		public async Task Send(WebSocketMessage message)
		{

			if (_serializerSettings == null)
			{
				_serializerSettings = new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				};
			}

			var entityMessage = message as WebSocketEntityMessage;

			// Get the bytes of the message:
			var jsonMessage = JsonConvert.SerializeObject(message, _serializerSettings);
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, entityMessage == null ? null : entityMessage.Entity);
		}

		/// <summary>
		/// Sends a JSON message to all of a user's clients.
		/// </summary>
		public async Task Send(string jsonMessage, object entity = null)
		{
			var data = Encoding.UTF8.GetBytes(jsonMessage);
			await Send(data, entity);
		}

		/// <summary>
		/// Sends a raw binary message to all of a user's clients.
		/// </summary>
		public async Task Send(byte[] message, object entity = null)
		{
			var arSegment = new ArraySegment<Byte>(message);

			// Send it now:
			var current = First;

			while (current != null)
			{
				#warning todo - send direct to user is disabled (but unused)

				/*
				if (entity != null && current.FilterNode != null)
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
				*/

				current = current.UserNext;
			}
		}

	}

	/// <summary>
	/// A connected websocket client.
	/// </summary>
	public partial class WebSocketClient{

		/// <summary>
		/// Not globally unique. ID to identify a particular WS client.
		/// </summary>
		public uint Id;
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
		public WebSocketTypeClient FirstClient;

		/// <summary>
		/// All the message types that this client is listening to.
		/// </summary>
		public WebSocketTypeClient LastClient;

		/// <summary>
		/// Adds a listener for messages of the given type. Returns null if it was already being listened to.
		/// </summary>
		public async Task<WebSocketTypeClient> AddEventListener(WebSocketTypeListeners type, JObject filter, int id = -1){

			// Already got a listener with this ID? If so, replace its filter.
			var client = id == -1 ? null : GetById(id);

			if (client == null)
			{
				client = new WebSocketTypeClient();
				client.Client = this;
				client.Id = id;

				// Add to the user:
				if (LastClient == null)
				{
					FirstClient = LastClient = client;
				}
				else
				{
					client.PreviousClient = LastClient;
					LastClient = LastClient.NextClient = client;
				}

				// Add this client object to the type itself:
				type.Add(client);
			}

			// Apply the filter now (must be after type.Add):
			await client.SetFilter(filter);

			return client;
		}

		/// <summary>
		/// Gets a type client by ID.
		/// </summary>
		/// <param name="id">The ID</param>
		/// <returns></returns>
		public WebSocketTypeClient GetById(int id)
		{
			var c = FirstClient;

			while (c != null)
			{
				if (c.Id == id)
				{
					return c;
				}
				c = c.NextClient;
			}

			return null;
		}

		/// <summary>
		/// Removes this client from the user set.
		/// </summary>
		public async Task RemoveFromUserSet(Dictionary<int, UserWebsocketLinks> all){
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
			
			// Is the set completely empty?
			if(UserSet.First == null && all != null){
				// Remove from the overall lookup now.
				lock(all){
					all.Remove(UserSet.Id);
				}

				// Trigger state event:
				await Events.WebSocketUserState.Dispatch(Context != null ? Context : new Context(), UserSet.Id, UserSet);
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
		public async Task OnDisconnected(WebSocketService service)
		{
			// Remove from user set:
			await RemoveFromUserSet(service.UserListeners);

			var current = FirstClient;

			while (current != null)
			{
				var next = current.NextClient;
				current.Remove();
				current = next;
			}

			FirstClient = LastClient = null;
		}

		/// <summary>
		/// Removes listener for messages of the given type.
		/// </summary>
		public void RemoveEventListener(WebSocketTypeListeners type){

			var current = FirstClient;

			while (current != null)
			{
				var next = current.NextClient;
				if (current.TypeSet == type)
				{
					current.Remove();
				}
				current = next;
			}
		}

		/// <summary>
		/// Removes listener by given ID.
		/// </summary>
		public void RemoveEventListener(int id)
		{
			var c = GetById(id);

			if (c != null)
			{
				c.Remove();
			}
		}

	}
	
}
