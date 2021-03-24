using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Microsoft.Extensions.Configuration;
using Api.Configuration;
using Api.StackTools;
using System.Diagnostics;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Users;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Net;
using Api.Signatures;
using Api.AutoForms;

namespace Api.ContentSync
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ContentSyncService
	{
		/// <summary>
		/// This server's ID from the ContentSync config.
		/// </summary>
		public int ServerId { get; set; }

		/// <summary>
		/// Handshake opcode
		/// </summary>
		public OpCode<SyncServerHandshake> HandshakeOpCode { get; set; }

		/// <summary>
		/// True if sync should be in verbose mode.
		/// </summary>
		public bool Verbose = true;
		
		static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
		private ContentSyncConfig _configuration;
		private DatabaseService _database;
		/// <summary>
		/// Private LAN sync server.
		/// </summary>
		private Server<ContentSyncServer> SyncServer;
		/// <summary>
		/// Servers connected to this one.
		/// </summary>
		private Dictionary<int, ContentSyncServer> RemoteServers = new Dictionary<int, ContentSyncServer>();
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ContentSyncService(DatabaseService database)
		{
			// The content sync service is used to keep content created by multiple instances in sync.
			// (which can be a cluster of servers, or a group of developers)
			// It does this by setting up 'stripes' of IDs which are assigned to particular users.
			// A user is identified by the computer hostname.
			
			_database = database;

			// Load config:
			var isActive = SetupConfig();

			if (isActive)
			{
				// Must happen after services start otherwise the page service isn't necessarily available yet.
				// Notably this happens immediately after services start in the first group
				// (that's before any e.g. system pages are created).
				Events.Service.AfterStart.AddEventListener(async (Context ctx, object src) =>
				{
					await Start();
					return src;
				}, 1);
			}
		}
		
		/// <summary>
		/// True if the sync file is active.
		/// </summary>
		private bool SyncFileMode = false;

		/// <summary>
		/// The name of this ContentSync host
		/// </summary>
		private string HostName;

		private bool SetupConfig()
		{
			var section = AppSettings.GetSection("ContentSync");
			if (section == null)
			{
				_configuration = null;
				return false;
			}

			_configuration = section.Get<ContentSyncConfig>();

			if (_configuration == null || _configuration.Users == null || _configuration.Users.Count == 0)
			{
				Console.WriteLine("[WARN] Content sync is installed but not configured.");
				return false;
			}

			if (_configuration.SyncFileMode.HasValue)
			{
				SyncFileMode = _configuration.SyncFileMode.Value;
			}
			else
			{
				// Devs have sync file turned on whenever sync is:
#if DEBUG
				SyncFileMode = true;
#endif
			}

			Verbose = _configuration.Verbose;

			if (Verbose)
			{
				Console.WriteLine("Content sync is in verbose mode - it will tell you each thing it syncs over your network.");
			}

			// Get system name:
			var name = System.Environment.MachineName.ToString();
			HostName = name;

			/*
			 * We need the ServerId to be available sooner than this so a custom name is now obsolete.
			 * 
			var taskCompletionSource = new TaskCompletionSource<bool>();
			try {
				// Get the user:
				var stackTools = new NodeProcess("socialstack sync whoami", true);
				var errored = false;
				string name = null;

				stackTools.Process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
				{
					if (string.IsNullOrEmpty(e.Data))
					{
						return;
					}

					name = e.Data.Trim();
				});

				stackTools.Process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
				{
					if (string.IsNullOrEmpty(e.Data))
					{
						return;
					}

					errored = true;
				});

				stackTools.OnStateChange += (NodeProcessState state) =>
				{

					if (state == NodeProcessState.EXITING)
					{
						if (errored || name == null)
						{
							name = System.Environment.MachineName.ToString();
						}
						Console.WriteLine("Content sync starting with config for '" + name + "'");

						Task.Run(async () =>
						{
							await StartFor(name);
							taskCompletionSource.SetResult(true);
						});

					}

				};

				stackTools.Start();
			}
			catch(Exception e)
			{
				taskCompletionSource.SetException(e);
			}

			return taskCompletionSource.Task;
			*/

			_configuration.Users.TryGetValue(name, out List<StripeRange> myRanges);

			if (myRanges == null)
			{
				Console.WriteLine("[WARN]: Content sync disabled. This instance (" + name + ") has no allocation in the project appsettings.json ContentSync config.");
				return false;
			}
			else if (myRanges.Count == 0)
			{
				Console.WriteLine("[WARN]: Content sync disabled. This instance (" + name + ") is in the contentsync appsettings.json, but it's setup wrong. It should be an array, like \"" + name + "\": [{..}]");
				return false;
			}

			MyRanges = myRanges;
			ServerId = myRanges[0].ServerId;
			
			return true;
		}

		private List<StripeRange> MyRanges;

		private string FileSafeName(string name)
		{
			return new string(name.Where(ch => !InvalidFileNameChars.Contains(ch)).ToArray());
		}

		/// <summary>
		/// When in local dev, this is "this" user's sync table set.
		/// </summary>
		public SyncTableFileSet LocalTableSet;

		/// <summary>
		/// Adds ID assigners to the given event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="assigner"></param>
		/// <param name="evtGroup"></param>
		public void AddIdAssigner<T>(IdAssigner assigner, EventGroup<T> evtGroup) where T:DatabaseRow<int>
		{
			if (assigner is IdAssignerSigned)
			{
				var signedAssigner = assigner as IdAssignerSigned;

				evtGroup.BeforeCreate.AddEventListener((Context context, T content) =>
				{
					if (content == null)
					{
						return new ValueTask<T>(content);
					}

					// Assign an ID now!
					content.Id = (int)signedAssigner.Assign();

					return new ValueTask<T>(content);
				});
			}
			else
			{
				var unsignedAssigner = assigner as IdAssignerUnsigned;

				evtGroup.BeforeCreate.AddEventListener((Context context, T content) =>
				{
					if (content == null)
					{
						return new ValueTask<T>(content);
					}

					// Assign an ID now!
					content.Id = (int)unsignedAssigner.Assign();

					return new ValueTask<T>(content);
				});
			}
		}

		/// <summary>
		/// Sets up a particular content type with e.g. ID assign handlers.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void SetupForType<T>(StripeTable table) where T: new()
		{
			// Invoked by reflection

			var evtGroup = Events.GetGroup<T>();

			if (evtGroup == null)
			{
				return;
			}

			// Db table name is..
			var tableName = typeof(T).TableName();

			// Get the assigner for this table:
			if (!table.DataTables.TryGetValue(tableName, out IdAssigner assigner))
			{
				// If this ever happens, it signals an internal issue with Socialstack.
				// Content types drive the table schema. ID assigners come from the table schema.
				// If a content type was somehow skipped, or its name is mangled, then this would happen.
				Console.WriteLine("[WARN] Content sync integrity issue. The content type '" + typeof(T) + "' does not have an ID assigner.");
				return;
			}

			// ID assigner is currently only available on int tables:
			if (typeof(DatabaseRow<int>).IsAssignableFrom(typeof(T)))
			{
				var methodInfo = GetType().GetMethod("AddIdAssigner");

				// Add ID assigner:
				var addIdAssigner = methodInfo.MakeGenericMethod(new Type[] {
					typeof(T)
				});

				addIdAssigner.Invoke(this, new object[] {
					assigner, evtGroup
				});
			}
			
			if (SyncFileMode && LocalTableSet != null)
			{
				// Add handlers to Create, Delete and Update events, and track these in a syncfile for this user.

				// Attempt to get the sync file for this type:
				LocalTableSet.Files.TryGetValue(tableName, out SyncTableFile localSyncFile);

				if (localSyncFile != null)
				{
					// Hook up create/ update/ delete - we want to track modding of objects:
					evtGroup.AfterCreate.AddEventListener((Context context, T content) =>
					{
						if (content == null)
						{
							return new ValueTask<T>(content);
						}

						// Write creation to sync file:
						localSyncFile.Write(content, 'C', context == null ? 0 : context.LocaleId);

						return new ValueTask<T>(content);
					});

					evtGroup.AfterUpdate.AddEventListener((Context context, T content) =>
					{
						if (content == null)
						{
							return new ValueTask<T>(content);
						}

						// Write update to sync file:
						localSyncFile.Write(content, 'U', context == null ? 0 : context.LocaleId);

						return new ValueTask<T>(content);
					});

					evtGroup.AfterDelete.AddEventListener((Context context, T content) =>
					{
						if (content == null)
						{
							return new ValueTask<T>(content);
						}

						// Write delete to sync file:
						localSyncFile.Write(content, 'D', context == null ? 0 : context.LocaleId);

						return new ValueTask<T>(content);
					});
				}
			}
		}

		/// <summary>
		/// Starts the content sync service.
		/// Must run after all other services have loaded.
		/// </summary>
		public async Task Start()
		{
			Console.WriteLine("Content sync starting with config for '" + HostName + "' as Server #" + ServerId);

			// Find the biggest max value:
			var overallMax = 0;

			foreach (var kvp in _configuration.Users)
			{
				if (kvp.Value == null || kvp.Value.Count == 0)
				{
					continue;
				}

				foreach (var range in kvp.Value)
				{
					if (range.Max > overallMax)
					{
						overallMax = range.Max;
					}
				}
			}

			if (overallMax != 0)
			{

				// Load the allocations:
				StripeTable table = new StripeTable(MyRanges, overallMax);

				await table.Setup(_database);

				Console.WriteLine("Content sync ID information obtained");

				if(SyncFileMode){
					
					// Create syncfile object for each known table and for each user.

					Console.WriteLine("Content sync now checking for changes");

					// Make sure a sync dir exists for this user.
					// Syncfiles go in as Database/FILENAME_SAFE_USERNAME/tableName.txt
					var dirName = FileSafeName(HostName);
					Directory.CreateDirectory("Database/" + dirName);

					try
					{
						// Load them:
						Dictionary<string, SyncTableFileSet> loadedSyncSets = new Dictionary<string, SyncTableFileSet>();

						foreach (var kvp in _configuration.Users)
						{
							if (kvp.Value == null || kvp.Value.Count == 0)
							{
								continue;
							}

							// Create the table set:
							var syncSet = new SyncTableFileSet("Database/" + FileSafeName(kvp.Key));

							// Set it up:
							// (for "my" files, I'm going to instance them all - regardless of if the actual file exists or not).
							var mine = kvp.Key == HostName;

							if (mine)
							{
								LocalTableSet = syncSet;
							}

							syncSet.Setup(!mine);
							loadedSyncSets[kvp.Key] = syncSet;

							if (!mine)
							{
								// Apply the sync set:
								await syncSet.Sync(_database);
							}
						}

					}
					catch (Exception e)
					{
						Console.WriteLine("ContentSync failed to handle other user's updates with error: " + e.ToString());
					}
				}
				
				var methodInfo = GetType().GetMethod("SetupForType");
				
				// Next, add Create handlers to all types.
				// When the handler fires, we simply assign an ID from our pool.
				// DatabaseService internally handles predefined IDs already.
				foreach (var kvp in ContentTypes.TypeMap)
				{
					// Invoke setup for type:
					var setupType = methodInfo.MakeGenericMethod(new Type[] {
						kvp.Value
					});

					setupType.Invoke(this, new object[] {
						table
					});
				}
			}
			// Add event handlers to all caching enabled types, *if* there are any with remote addresses.
			// If a change (update, delete, create) happens, broadcast a cache remove message to all remote addresses.
			// If the link drops, poll until remote is back again.
			var servers = new List<ContentSyncServerInfo>();
			ContentSyncServerInfo myServerInfo = new ContentSyncServerInfo();

			// Instance a sync server if remote addresses are present in the config:
			foreach (var kvp in _configuration.Users)
			{
				if (kvp.Value == null)
				{
					continue;
				}

				// Remote address?
				var serverId = 0;
				string remoteAddr = null;
				var port = 0;
				IPAddress bindAddress = IPAddress.Loopback;

				foreach (var range in kvp.Value)
				{
					if (!string.IsNullOrWhiteSpace(range.RemoteAddress))
					{
						remoteAddr = range.RemoteAddress;
					}

					if (range.ServerId != 0)
					{
						serverId = range.ServerId;
					}

					if (!string.IsNullOrEmpty(range.BindAddress))
					{
						bindAddress = range.BindAddress == "*" ? IPAddress.Any : IPAddress.Parse(range.BindAddress);
					}

					if (range.Port != 0)
					{
						port = range.Port;
					}
				}

				if (!string.IsNullOrWhiteSpace(remoteAddr))
				{
					// Remote server - We'll sync up with this server by sending it any relevant changes that we generate.
					// A relevant change is anything cached, or anything that is e.g. live on the websocket.
					var info = new ContentSyncServerInfo()
					{
						RemoteAddress = remoteAddr,
						BindAddress = bindAddress,
						ServerId = serverId
					};

					if (port != 0)
					{
						// Override default:
						info.Port = port;
					}

					if (kvp.Key == HostName)
					{
						// This is "me!"
						myServerInfo = info;
					}
					else
					{
						servers.Add(info);
					}
				}

			}

			if (servers.Count == 0)
			{
				return;
			}
			
			// We're in a cluster.
			// Start my server now:
			SyncServer = new Server<ContentSyncServer>();
			SyncServer.Port = myServerInfo.Port;
			SyncServer.BindAddress = myServerInfo.BindAddress;

			HandshakeOpCode = SyncServer.RegisterOpCode(3, (SyncServerHandshake message) =>
			{
				// Grab the values from the context:
				var theirId = message.ServerId;
				var signature = message.Signature;

				// If the sig checks out, and they've also signed their own and our ID, then they can connect.
				var signData = theirId.ToString() + "=>" + ServerId.ToString();

				if (!Services.Get<SignatureService>().ValidateSignature(signData, signature))
				{
					// Fail there:
					message.Kill();

					// Check the serverInfo.json on the server that threw this exception 
					// and make sure its ID matches whatever is in the global server map.
					throw new Exception("Server handshake failed. This usually means the remote server is using the wrong server ID. It's ID was " + 
						theirId + " and it tried to connect to server " + ServerId);
				}

				// Ok - It's definitely a permitted server.
				var server = (message.Client as ContentSyncServer);
				server.Hello = false;

				// Add server to set of servers that have connected:
				server.ServerId = theirId;
				lock (RemoteServers)
				{
					RemoteServers[theirId] = server;
				}

				foreach (var meta in RemoteTypes)
				{
					SendMetaTo(meta, server);
				}

				// Let it know that we're happy:
				var helloResponse = Writer.GetPooled();
				helloResponse.Start(4);
				helloResponse.Write(ServerId);

				// Respond with hello response:
				server.Send(helloResponse);
				
				Console.WriteLine("[CSync] Connected to " + theirId);
				
				// That's all folks:
				message.Done();
			});

			HandshakeOpCode.IsHello = true;

			SyncServer.RegisterOpCode(4, (SyncServerHandshakeResponse message) => {
				var server = (message.Client as ContentSyncServer);

				if (!server.Hello)
				{
					// Hello other server! Add it to lookup:
					server.ServerId = message.ServerId;

					lock (RemoteServers)
					{
						RemoteServers[message.ServerId] = server;
					}

					foreach (var meta in RemoteTypes)
					{
						SendMetaTo(meta, server);
					}

				}

				Console.WriteLine("[CSync] Connected to " + message.ServerId);
				
				// That's all folks:
				message.Done();
			});

			TypeRegOpcode = SyncServer.RegisterOpCode(5, (TypeRegistration message) => {

				// Remote server is telling us that it is listening 
				// for a particular content type as a given opcode, as well as the object structure.
				var server = (message.Client as ContentSyncServer);

				var type = ContentTypes.GetType(message.ContentTypeId);

				if (type != null)
				{
					// The message type we'll receive is..
					var messageType = typeof(ContentUpdate<>).MakeGenericType(new Type[] {type});

					var ocmWriter = new OpCodeMessageWriter(message.OpCodeToListenFor, message.FieldInfo, messageType);

					// Add to OCM:
					server.OpCodeMap[type] = ocmWriter;

					// Add to type writers (may overwrite if a particular server declared twice - that's fine):
					AddTypeWriter(type, ocmWriter, server);
				}
				
				// That's all folks:
				message.Done();
			});

			foreach (var meta in RemoteTypes)
			{
				HandleType(meta, false);
			}

			// After HandleType calls so it can register some of the handlers:
			SyncServer.Start();

			// Try to explicitly connect to the other servers.
			// We might be the first one up, so some of these can outright fail.
			// That's ok though - they'll contact us instead.
			Console.WriteLine("[CSync] Started connecting to " + servers.Count + " peers");
			
			foreach (var serverInfo in servers)
			{
				Console.WriteLine("[CSync] Connect to " + serverInfo.RemoteAddress);
				SyncServer.ConnectTo(serverInfo.RemoteAddress, serverInfo.Port, serverInfo.ServerId, (ContentSyncServer s) => {
					s.ServerId = serverInfo.ServerId;
				});
			}

		}

		/// <summary>
		/// A map of each content type to a set of remote server writers.
		/// When something changes, we use the list to message the servers about it.
		/// </summary>
		private Dictionary<Type, List<ContentSyncTypeWriter>> TypeWriters = new Dictionary<Type, List<ContentSyncTypeWriter>>();

		/// <summary>
		/// Gets the set of type writers for the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private List<ContentSyncTypeWriter> GetTypeWriters(Type type)
		{
			lock (TypeWriters)
			{
				if (!TypeWriters.TryGetValue(type, out List<ContentSyncTypeWriter> set))
				{
					set = new List<ContentSyncTypeWriter>();
					TypeWriters[type] = set;
				}

				return set;
			}
		}

		/// <summary>
		/// Removes the given server from the lookups.
		/// </summary>
		/// <param name="server"></param>
		public void RemoveServer(ContentSyncServer server)
		{
			lock (RemoteServers)
			{
				RemoteServers.Remove(server.ServerId);
			}

			foreach (var kvp in TypeWriters)
			{
				var set = kvp.Value;

				ContentSyncTypeWriter toRemove = null;

				foreach (var entry in set)
				{
					if (entry.Server == server)
					{
						toRemove = entry;
						break;
					}
				}

				if (toRemove != null)
				{
					set.Remove(toRemove);
				}
			}
		}

		/// <summary>
		/// Adds a type writer. Note that a given server can only have one of the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="writer"></param>
		/// <param name="server"></param>
		private void AddTypeWriter(Type type, OpCodeMessageWriter writer, ContentSyncServer server)
		{
			var writers = GetTypeWriters(type);

			for (var i = 0; i < writers.Count; i++)
			{
				if (writers[i].Server == server)
				{
					writers[i].Writer = writer;
					return;
				}
			}

			lock (writers)
			{
				writers.Add(new ContentSyncTypeWriter()
				{
					Server = server,
					Writer = writer
				});
			}
		}

		private OpCode<TypeRegistration> TypeRegOpcode;

		private void SendMetaTo(ContentSyncTypeMeta meta, ContentSyncServer server)
		{
			var fields = meta.FieldList;

			var msg = TypeRegOpcode.Write(new TypeRegistration() {
				ContentTypeId = meta.ContentTypeId,
				OpCodeToListenFor = meta.OpCodeId,
				FieldInfo = fields
			});

			server.Send(msg);
		}

		/// <summary>
		/// Register a content type as an opcode.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="meta"></param>
		/// <param name="addListeners"></param>
		public void HandleTypeInternal<T, ID>(ContentSyncTypeMeta meta, bool addListeners)
			where T : class, IHaveId<ID>, new()
			where ID : struct, IConvertible
		{
			// NOTE: This is used by reflection by HandleType.

			// - Hook up the events which then builds the messages too

			// Get the service:
			var a = Services.GetByContentType(typeof(T));
			var svc = a as AutoService<T, ID>;

			if (svc == null)
			{
				Console.WriteLine("[ERROR] Content type " + typeof(T)+  " is marked for sync, but has no service. This means it doesn't have a cache either, so syncing it doesn't gain anything. Note that you can have an AutoService without it using the DB. Ignoring this sync request.");
				return;
			}

			// Get the event group:
			var eventGroup = svc.EventGroup;
			
			var receivedEventHandler = eventGroup.Received;
			var afterLoad = eventGroup.AfterLoad;

			if (addListeners)
			{
				var servers = GetTypeWriters(typeof(T));

				// Add the event listeners now!
				eventGroup.AfterCreate.AddEventListener((Context ctx, T src) =>
				{
					if (src == null || servers.Count == 0)
					{
						return new ValueTask<T>(src);
					}

					try
					{

						// Create the content update message:
						var message = new ContentUpdate<T>();
						message.Action = 1;
						message.User = ctx.UserId;
						message.LocaleId = ctx.LocaleId;
						message.RoleId = ctx.RoleId;
						message.Content = src;

						// Tell each server about it:
						foreach (var server in servers)
						{
							if(Verbose){
								Console.WriteLine("[Create " + typeof(T).Name + "]=>" + server.Server.ServerId);
							}
							var msg = server.Writer.Write(message);
							server.Server.Send(msg);
						}

					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}

					return new ValueTask<T>(src);
				}, 1000); // Always absolutely last

				eventGroup.AfterUpdate.AddEventListener((Context ctx, T src) =>
				{
					if (src == null || servers.Count == 0)
					{
						return new ValueTask<T>(src);
					}

					try
					{
						// Create the content update message:
						var message = new ContentUpdate<T>();
						message.Action = 2;
						message.User = ctx.UserId;
						message.LocaleId = ctx.LocaleId;
						message.RoleId = ctx.RoleId;
						message.Content = src;
						
						// Tell each server about it:
						foreach (var server in servers)
						{
							if(Verbose){
								Console.WriteLine("[Update " + typeof(T).Name + "]=>" + server.Server.ServerId);
							}
							var msg = server.Writer.Write(message);
							server.Server.Send(msg);
						}
					}
						catch (Exception e)
					{
						Console.WriteLine(e);
					}

				return new ValueTask<T>(src);
				}, 1000); // Always absolutely last

				eventGroup.AfterDelete.AddEventListener((Context ctx, T src) =>
				{
					if (src == null || servers.Count == 0)
					{
						return new ValueTask<T>(src);
					}

					try
					{
						// Create the content update message:
						var message = new ContentUpdate<T>();
						message.Action = 3;
						message.User = ctx.UserId;
						message.LocaleId = ctx.LocaleId;
						message.RoleId = ctx.RoleId;
						message.Content = src;

						// Tell each server about it:
						foreach (var server in servers)
						{
							if(Verbose){
								Console.WriteLine("[Delete " + typeof(T).Name + "]=>"+server.Server.ServerId);
							}
							var msg = server.Writer.Write(message);
							server.Server.Send(msg);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}

				return new ValueTask<T>(src);
				}, 1000); // Always absolutely last

			}

			if (SyncServer == null)
			{
				// Server isn't ready yet - it'll call this again when it is.
				return;
			}

			// Get the primary cache:
			var primaryCache = svc.GetCacheForLocale(1);
			
			meta.OpCode = SyncServer.RegisterOpCode((uint)meta.OpCodeId, (ContentUpdate<T> message) => {

				// Create the context as it was in the remote:
				var context = new Context() {
					RoleId = message.RoleId,
					LocaleId = message.LocaleId,
					UserId = message.User
				};

				// Create, Update, Delete
				var action = message.Action;

				if (message.Content == null)
				{
					// That's all folks:
					message.Done();
					return;
				}

				// Dispatch events:
				Task.Run(async () =>{
					
					if(Verbose){
						Console.WriteLine("Receive <= " + action + " of " + message.Content.GetType());
					}

					// The message content is the resolved (not raw) object.
					var entity = message.Content;

					// Update local cache next:
					var cache = svc.GetCacheForLocale(context.LocaleId);

					T raw;

					if (context.LocaleId == 1)
					{
						// Primary locale. Entity == raw entity, and no transferring needs to happen.
						raw = entity;
					}
					else
					{
						// Get the raw entity from the cache. We'll copy the fields from the raw object to it.
						raw = cache.GetRaw(entity.GetId());

						if (raw == null)
						{
							raw = new T();
						}

						// Transfer fields from raw to entity, using the primary object as a source of blank fields:
						svc.PopulateTargetEntityFromRaw(entity, raw, primaryCache.Get(raw.GetId()));
					}

					// Run afterLoad events:
					await afterLoad.Dispatch(context, entity);

					// Received the content object:
					await receivedEventHandler.Dispatch(context, entity, action);

					if (cache != null)
					{
						lock (cache)
						{
							if (action == 1 || action == 2)
							{
								// Created or updated
								cache.Add(context, entity, message.Content);

								if (context.LocaleId == 1)
								{
									// Primary locale update - must update all other caches in case they contain content from the primary locale.
									svc.OnPrimaryEntityChanged(entity);
								}
							}
							else if (action == 3)
							{
								// Deleted
								cache.Remove(context, message.Content.GetId());
							}
						}
					}
				});

				// That's all folks:
				message.Done();
			});

			// Collect the field list:
			meta.FieldList = meta.OpCode.FieldList();
		}

		private void HandleType(ContentSyncTypeMeta meta, bool addListeners)
		{
			// This is only called a handful of times during startup
			var idType = meta.Type.GetField("Id").FieldType;

			var handleTypeInternal = GetType().GetMethod("HandleTypeInternal").MakeGenericMethod(new Type[] {
				meta.Type,
				idType
			});

			handleTypeInternal.Invoke(this, new object[] {
				meta,
				addListeners
			});

			// If we've already connected to some servers, tell them about this type:
			if (RemoteServers != null)
			{
				foreach (var kvp in RemoteServers)
				{
					var server = kvp.Value;
					SendMetaTo(meta, server);
				}
			}
		}

		private List<ContentSyncTypeMeta> RemoteTypes = new List<ContentSyncTypeMeta>();

		/// <summary>
		/// Informs CSync to start syncing the given type as the given opcode.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="opcode"></param>
		public void SyncRemoteType(Type type, int opcode)
		{
			var meta = new ContentSyncTypeMeta()
			{
				Type = type,
				OpCodeId = (uint)opcode,
				ContentTypeId = ContentTypes.GetId(type)
			};

			RemoteTypes.Add(meta);
			HandleType(meta, true);
		}

	}

	/// <summary>
	/// Stores sync meta for a given type.
	/// </summary>
	public class ContentSyncTypeMeta
	{
		/// <summary>
		/// The content type
		/// </summary>
		public Type Type;

		/// <summary>
		/// The content type ID for Type.
		/// </summary>
		public int ContentTypeId;

		/// <summary>
		/// The opcode
		/// </summary>
		public uint OpCodeId;

		/// <summary>
		/// The opcode
		/// </summary>
		public OpCode OpCode;

		/// <summary>
		/// The field meta from the opcode.
		/// </summary>
		public List<string> FieldList;
	}

	/// <summary>
	/// Server/ writer combo
	/// </summary>
	public class ContentSyncTypeWriter
	{
		/// <summary>
		/// The server to send to.
		/// </summary>
		public ContentSyncServer Server;

		/// <summary>
		/// The writer that helps us build the actual msg.
		/// </summary>
		public OpCodeMessageWriter Writer;
	}

}
