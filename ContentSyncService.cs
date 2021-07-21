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
	[LoadPriority(3)]
	public partial class ContentSyncService : AutoService
	{
		/// <summary>
		/// This server's ID.
		/// </summary>
		public uint ServerId
		{
			get {
				return Self.Id;
			}
		}

		/// <summary>
		/// Reverses the bits in the given number.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		private uint Reverse(uint x)
		{
			uint reversed = 0;

			for (int i = 0; i < 32; i++)
			{
				// If the ith bit of x is toggled, toggle the ith bit from the right of reversed
				reversed |= (x & ((uint)1 << i)) != 0 ? (uint)1 << (31 - i) : 0;
			}

			return reversed;
		}

		/// <summary>
		/// Handshake opcode
		/// </summary>
		public OpCode<SyncServerHandshake> HandshakeOpCode { get; set; }

		/// <summary>
		/// True if sync should be in verbose mode.
		/// </summary>
		public bool Verbose = true;
		
		static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
		private ContentSyncServiceConfig _configuration;
		private ClusteredServerService _clusteredServerService;

		/// <summary>
		/// Private LAN sync server.
		/// </summary>
		private Server<ContentSyncServer> SyncServer;
		
		/// <summary>
		/// Servers connected to this one.
		/// </summary>
		private Dictionary<uint, ContentSyncServer> RemoteServers = new Dictionary<uint, ContentSyncServer>();

		/// <summary>
		/// The port number for contentSync to use.
		/// </summary>
		public int Port {
			get {
				return _configuration.Port;
			}
		}

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ContentSyncService(DatabaseService database, ClusteredServerService clusteredServerService)
		{
			// The content sync service is used to keep content created by multiple instances in sync.
			// (which can be a cluster of servers, or a group of developers)
			// It does this by setting up 'stripes' of IDs which are assigned to particular users.
			// A user is identified by the computer hostname.
			
			_database = database;
			_clusteredServerService = clusteredServerService;

			// Load config:
			_configuration = GetConfig<ContentSyncServiceConfig>();
			
			if (_configuration.SyncFileMode.HasValue)
			{
				SyncFileMode = _configuration.SyncFileMode.Value;
			}

			Verbose = _configuration.Verbose;

			if (Verbose)
			{
				Console.WriteLine("Content sync is in verbose mode - it will tell you each thing it syncs over your network.");
			}

			// Get system name:
			var name = string.IsNullOrEmpty(_configuration.HostName) ? System.Environment.MachineName.ToString() : _configuration.HostName;
			HostName = name;

			Events.Service.AfterStart.AddEventListener((Context ctx, object s) => {

				// Start:
				ApplyRemoteDataAndStart();

				return new ValueTask<object>(s);
			});
		}

		/// <summary>
		/// A bitmask used for identifying the max ID in a given server's block.
		/// </summary>
		private uint MaxIdMask;

		/// <summary>
		/// True if the sync file is active.
		/// </summary>
		private bool SyncFileMode = false;

		/// <summary>
		/// The name of this ContentSync host
		/// </summary>
		public string HostName;

		private string FileSafeName(string name)
		{
			return new string(name.Where(ch => !InvalidFileNameChars.Contains(ch)).ToArray());
		}

		/// <summary>
		/// When in local dev, this is "this" user's sync table set.
		/// </summary>
		public SyncTableFileSet LocalTableSet;

		/// <summary>
		/// Sets up the config required to connect to other servers.
		/// </summary>
		/// <returns></returns>
		public async Task Startup()
		{
			if (AllServers != null)
			{
				// Already setup.
				return;
			}

			// Get environment name:
			var env = Services.Environment;

			if (Services.IsDevelopment())
			{
				// Dev environment always uses the same data:
				Self = new ClusteredServer()
				{
					Port = Port,
					HostName = HostName,
					Environment = env,
					Id = 1
				};

				AllServers = new List<ClusteredServer>() { Self };
				MaxIdMask = CreateMask(1);
				return;
			}

			var ctx = new Context();

			// Get all servers:
			AllServers = await _clusteredServerService.Where(DataOptions.IgnorePermissions).ListAll(ctx);

			ClusteredServer self = null;

			uint maxId = 0;
			var anyDeleted = false;

			foreach (var server in AllServers)
			{
				if (server.HostName == HostName)
				{
					self = server;
				}
				else if (server.Environment != env)
				{
					anyDeleted = true;
					await _clusteredServerService.Delete(ctx, server, DataOptions.IgnorePermissions);
					continue;
				}

				if (server.Id > maxId)
				{
					maxId = server.Id;
				}
			}

			if (anyDeleted)
			{
				Console.WriteLine("[WARN] A server has been deleted from " + typeof(ClusteredServer).TableName() + " because it was from a different environment. " +
					"When copying data between environments, don't include this table. " +
					"Doing so wastes server IDs and will in turn make your site assign large ID values unnecessarily.");
			}

			var ips = await IpDiscovery.Discover();

			if (self == null)
			{
				self = new ClusteredServer()
				{
					Port = Port,
					HostName = HostName,
					Environment = env
				};

				ips.CopyTo(self);

				AllServers.Add(self);

				await _clusteredServerService.Create(ctx, self, DataOptions.IgnorePermissions);

			}
			else if (ips.ChangedSince(self) || self.Environment != env)
			{
				// It changed - update it:
				var ipsAndEnvironmentFields = _clusteredServerService.GetChangeField("Environment")
					.And("PublicIPv4").And("PublicIPv6").And("PrivateIPv4").And("PrivateIPv6");

				await _clusteredServerService.Update(ctx, self, (Context c, ClusteredServer cs) => {

					ips.CopyTo(cs);
					cs.Environment = env;
					cs.MarkChanged(ipsAndEnvironmentFields);

				},DataOptions.IgnorePermissions);
			}

			if (self.Id > maxId)
			{
				maxId = self.Id;
			}

			MaxIdMask = CreateMask(maxId);

			Self = self;
		}

		private uint CreateMask(uint maxServerId)
		{
			int offset;

			if (maxServerId <= 8)
			{
				// Minimum blocked out is 3 bits (8 servers).
				offset = 29;
			}
			else if (maxServerId <= 16)
			{
				// 4 bits
				offset = 28;
			}
			else if (maxServerId <= 32)
			{
				offset = 27;
			}
			else if (maxServerId <= 64)
			{
				offset = 26;
			}
			else if (maxServerId <= 128)
			{
				offset = 25;
			}
			else if (maxServerId <= 256)
			{
				offset = 24;
			}
			else if (maxServerId <= 512)
			{
				offset = 23;
			}
			else if (maxServerId <= 1024)
			{
				offset = 22;
			}
			else
			{
				throw new Exception("Server ID '" + maxServerId + "' is too high. Max cluster size is 1024.");
			}

			return ((uint)1 << offset) - 1;
		}

		/// <summary>
		/// Creates an ID assigner for the given service.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
		/// <returns></returns>
		public async Task<IdAssigner<ID>> CreateAssigner<T, ID>(AutoService<T, ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			var serverId = ServerId;

			ulong thisServersIdMask = Reverse(serverId - 1);
			ulong maxIdMask = MaxIdMask;

			if (typeof(ID) == typeof(ulong))
			{
				thisServersIdMask = thisServersIdMask << 32;
				maxIdMask = (maxIdMask << 32) | 0xffffff;
			}

			// Using the filter API to collect the current max assigned ID.
			var f = service.Where("Id>=? and Id<=?", DataOptions.IgnorePermissions);
			f.Sort("Id", false);
			f.PageSize = 1;

			if (typeof(ID) == typeof(uint))
			{
				f.Bind((uint)thisServersIdMask).Bind((uint)(thisServersIdMask + maxIdMask));
			}
			else
			{
				f.Bind(thisServersIdMask).Bind(thisServersIdMask + maxIdMask);
			}

			var set = await f.ListAll(new Context());

			// Note: this will only be for rows that are actually in the database.
			// Empty tables for example - this set has 0 entries.
			ID latestIdResult = set != null && set.Count > 0 ? set[0].GetId() : default;

			if (typeof(ID) == typeof(ulong))
			{
				var latestResult = (ulong)((object)latestIdResult);

				if (latestResult == 0)
				{
					latestResult = thisServersIdMask;
				}

				// Create an ID assigner for this table:
				return new IdAssignerUInt64(latestResult) as IdAssigner<ID>;
			}
			else
			{
				var latestResult = (uint)((object)latestIdResult);

				if (latestResult == 0)
				{
					latestResult = (uint)thisServersIdMask;
				}

				// Create an ID assigner for this table:
				return new IdAssignerUInt32(latestResult) as IdAssigner<ID>;
			}
		}
		
		/// <summary>
		/// Pulls in data added by other devs, and starts the cSync server. Must occur after other services have started.
		/// </summary>
		public void ApplyRemoteDataAndStart()
		{
			/*
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
			*/

			if (Services.IsDevelopment())
			{
				// Don't set up the sync server in the dev environment.
				return;
			}

			// Add event handlers to all caching enabled types, *if* there are any with remote addresses.
			// If a change (update, delete, create) happens, broadcast a cache remove message to all remote addresses.
			// If the link drops, poll until remote is back again.
			
			// Start my server now:
			SyncServer = new Server<ContentSyncServer>();
			SyncServer.Port = Port;
			SyncServer.BindAddress = new IPAddress(Self.PrivateIPv4);

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

			// Try to explicitly connect to the other servers. Note that AllServers is setup by EventListener.
			// We might be the first one up, so some of these can outright fail.
			// That's ok though - they'll contact us instead.
			Console.WriteLine("[CSync] Started connecting to peers");

			var env = Services.Environment;

			foreach (var serverInfo in AllServers)
			{
				if (serverInfo == Self || serverInfo.Environment != env)
				{
					continue;
				}

				var ipAddress = new IPAddress(serverInfo.PrivateIPv4);

				Console.WriteLine("[CSync] Connect to " + ipAddress);

				SyncServer.ConnectTo(ipAddress, serverInfo.Port, serverInfo.Id, (ContentSyncServer s) => {
					s.ServerId = serverInfo.Id;
				});
			}

		}

		/// <summary>
		/// All servers (including "me").
		/// </summary>
		public List<ClusteredServer> AllServers;

		/// <summary>
		/// The clustered server representing this specific server. Has IP addresses setup and ready.
		/// </summary>
		public ClusteredServer Self;
		
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
		/// <typeparam name="INST_T"></typeparam>
		/// <param name="meta"></param>
		/// <param name="addListeners"></param>
		public void HandleTypeInternal<T, ID, INST_T>(ContentSyncTypeMeta meta, bool addListeners)
			where T : Content<ID>, new()
			where INST_T : T, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// NOTE: This is used by reflection by HandleType.

			// - Hook up the events which then builds the messages too

			// Get the service:
			var svc = meta.Service as AutoService<T, ID>;

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
				var context = new Context(message.LocaleId, message.User, message.RoleId);

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
							raw = new INST_T();
						}

						// Transfer fields from raw to entity, using the primary object as a source of blank fields:
						svc.PopulateTargetEntityFromRaw(entity, raw, primaryCache.Get(raw.GetId()));
					}

					// Run afterLoad events:
					var previousPermState = context.IgnorePermissions;
					context.IgnorePermissions = true;
					await afterLoad.Dispatch(context, entity);
					context.IgnorePermissions = previousPermState;

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

			var handleTypeInternal = GetType().GetMethod(nameof(HandleTypeInternal)).MakeGenericMethod(new Type[] {
				meta.Service.ServicedType,
				meta.Service.IdType,
				meta.Service.InstanceType
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
		/// <param name="service"></param>
		/// <param name="opcode"></param>
		public void SyncRemoteType(AutoService service, int opcode)
		{
			var meta = new ContentSyncTypeMeta()
			{
				Service = service,
				OpCodeId = (uint)opcode,
				ContentTypeId = ContentTypes.GetId(service.InstanceType)
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
		/// The service for the type.
		/// </summary>
		public AutoService Service;

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
