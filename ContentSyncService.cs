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
using System.Text;
using System.Collections.Concurrent;

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

			HandshakeOpCode = SyncServer.RegisterOpCode(3, (Client client, SyncServerHandshake message) =>
			{
				// Grab the values from the context:
				var theirId = message.ServerId;
				var signature = message.Signature;

				// If the sig checks out, and they've also signed their own and our ID, then they can connect.
				var signData = theirId.ToString() + "=>" + ServerId.ToString();

				if (!Services.Get<SignatureService>().ValidateSignature(signData, signature))
				{
					// Fail there:
					client.Close();
					return;

					// Check the serverInfo.json on the server that threw this exception 
					// and make sure its ID matches whatever is in the global server map.
					throw new Exception("Server handshake failed. This usually means the remote server is using the wrong server ID. It's ID was " + 
						theirId + " and it tried to connect to server " + ServerId);
				}

				// Ok - It's definitely a permitted server.
				var server = client as ContentSyncServer;
				server.Hello = false;

				// Add server to set of servers that have connected:
				server.ServerId = theirId;
				lock (RemoteServers)
				{
					RemoteServers[theirId] = server;
				}

				// Let it know that we're happy:
				var msg = SyncServerHandshakeResponse.Get();
				msg.ServerId = ServerId;
				var response = msg.Write(4);

				// Respond with hello response:
				server.Send(response);
				response.Release();
				msg.Release();

				Console.WriteLine("[CSync] Connected to " + theirId);
			});

			HandshakeOpCode.IsHello = true;
			
			SyncServer.RegisterOpCode(4, (Client client, SyncServerHandshakeResponse message) => {
				var server = (client as ContentSyncServer);

				if (!server.Hello)
				{
					// Hello other server! Add it to lookup:
					server.ServerId = message.ServerId;

					lock (RemoteServers)
					{
						RemoteServers[message.ServerId] = server;
					}
				}

				Console.WriteLine("[CSync] Connected to " + message.ServerId);
			});

			var reader = new SyncServerRemoteReader(1, this);
			reader.OpCode = SyncServer.RegisterOpCode(21, (Client client, SyncServerRemoteType message) => {
				// Note: this callback is never run. The remoteReader does all the work, as it can identify the concrete types of things.
			}, reader);

			reader = new SyncServerRemoteReader(2, this);
			reader.OpCode = SyncServer.RegisterOpCode(22, (Client client, SyncServerRemoteType message) => {
				// Note: this callback is never run. The remoteReader does all the work, as it can identify the concrete types of things.
			}, reader);

			reader = new SyncServerRemoteReader(3, this);
			reader.OpCode = SyncServer.RegisterOpCode(23, (Client client, SyncServerRemoteType message) => {
				// Note: this callback is never run. The remoteReader does all the work, as it can identify the concrete types of things.
			}, reader);

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
		/// Removes the given server from the lookups.
		/// </summary>
		/// <param name="server"></param>
		public void RemoveServer(ContentSyncServer server)
		{
			lock (RemoteServers)
			{
				RemoteServers.Remove(server.ServerId);
			}
		}

		/// <summary>
		/// Register a content type as an opcode.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <typeparam name="INST_T"></typeparam>
		/// <param name="svc"></param>
		public void HandleTypeInternal<T, ID, INST_T>(AutoService<T, ID> svc)
			where T : Content<ID>, new()
			where INST_T : T, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// - Hook up the events which then builds the messages too

			if (svc == null)
			{
				Console.WriteLine("[ERROR] Content type " + typeof(T)+  " is marked for sync, but has no service. " +
					"This means it doesn't have a cache either, so syncing it doesn't gain anything. Note that you can have an AutoService without it using the DB. Ignoring this sync request.");
				return;
			}

			// Create the meta:
			var meta = new ContentSyncTypeMeta<T, ID, INST_T>(svc);

			var name = svc.InstanceType.Name.ToLower();

			RemoteTypes[name] = meta;

			// Get the event group:
			var eventGroup = svc.EventGroup;
			
			// Same as the generic message handler - opcode + 4 byte size.
			var basicHeader = new byte[5];
			var boltIO = meta.ReaderWriter;

			// Type name buffer:
			var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);

			// Add the event listeners now!
			eventGroup.AfterCreate.AddEventListener((Context ctx, T src) =>
			{
				if (src == null)
				{
					return new ValueTask<T>(src);
				}

				// Start creating the sync message. This is much the same as what Message does internally when sending generic messages.
				// We just require some custom handling here to handle the type-within-a-message scenario.

				var writer = Writer.GetPooled();
				writer.Start(basicHeader);
				var firstBuffer = writer.FirstBuffer.Bytes;
				firstBuffer[0] = 21;
				writer.WriteCompressed(ctx.LocaleId);
				writer.Write(nameBytes);
				boltIO.Write((INST_T)src, writer);

				// Write the length of the JSON to the 3 bytes at the start:
				var msgLength = (uint)(writer.Length - 5);
				firstBuffer[1] = (byte)msgLength;
				firstBuffer[2] = (byte)(msgLength >> 8);
				firstBuffer[3] = (byte)(msgLength >> 16);
				firstBuffer[4] = (byte)(msgLength >> 24);

				// Mappings don't have a network room.
				if (svc.NetworkRooms != null)
				{
					var room = svc.NetworkRooms.GetRoom(src);

					if (room != null)
					{
						// Also send it to any network rooms (locally).
						room.SendLocally(writer);
					}
				}
				
				try
				{
					// Tell every server about it:
					foreach (var kvp in RemoteServers)
					{
						var server = kvp.Value;
							
						if (Verbose){
							Console.WriteLine("[Create " + typeof(T).Name + "]=>" + server.ServerId);
						}

						server.Send(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				return new ValueTask<T>(src);
			}, 1000); // Always absolutely last

			eventGroup.AfterUpdate.AddEventListener((Context ctx, T src) =>
			{
				if (src == null)
				{
					return new ValueTask<T>(src);
				}

				// Start creating the sync message. This is much the same as what Message does internally when sending generic messages.
				// We just require some custom handling here to handle the type-within-a-message scenario.

				var writer = Writer.GetPooled();
				writer.Start(basicHeader);
				var firstBuffer = writer.FirstBuffer.Bytes;
				firstBuffer[0] = 22;
				writer.WriteCompressed(ctx.LocaleId);
				writer.Write(nameBytes);
				boltIO.Write((INST_T)src, writer);

				// Write the length of the JSON to the 3 bytes at the start:
				var msgLength = (uint)(writer.Length - 5);
				firstBuffer[1] = (byte)msgLength;
				firstBuffer[2] = (byte)(msgLength >> 8);
				firstBuffer[3] = (byte)(msgLength >> 16);
				firstBuffer[4] = (byte)(msgLength >> 24);

				// Mappings don't have a network room.
				if (svc.NetworkRooms != null)
				{
					var room = svc.NetworkRooms.GetRoom(src);

					if (room != null)
					{
						// Also send it to any network rooms (locally).
						room.SendLocally(writer);
					}
				}

				try
				{
					// Tell every server about it:
					foreach (var kvp in RemoteServers)
					{
						var server = kvp.Value;

						if (Verbose)
						{
							Console.WriteLine("[Update " + typeof(T).Name + "]=>" + server.ServerId);
						}

						server.Send(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				return new ValueTask<T>(src);
			}, 1000); // Always absolutely last

			eventGroup.AfterDelete.AddEventListener((Context ctx, T src) =>
			{
				if (src == null)
				{
					return new ValueTask<T>(src);
				}

				// Start creating the sync message. This is much the same as what Message does internally when sending generic messages.
				// We just require some custom handling here to handle the type-within-a-message scenario.

				var writer = Writer.GetPooled();
				writer.Start(basicHeader);
				var firstBuffer = writer.FirstBuffer.Bytes;
				firstBuffer[0] = 23;
				writer.WriteCompressed(ctx.LocaleId);
				writer.Write(nameBytes);
				boltIO.Write((INST_T)src, writer);

				// Write the length of the JSON to the 3 bytes at the start:
				var msgLength = (uint)(writer.Length - 5);
				firstBuffer[1] = (byte)msgLength;
				firstBuffer[2] = (byte)(msgLength >> 8);
				firstBuffer[3] = (byte)(msgLength >> 16);
				firstBuffer[4] = (byte)(msgLength >> 24);

				// Mappings don't have a network room.
				if (svc.NetworkRooms != null)
				{
					var room = svc.NetworkRooms.GetRoom(src);

					if (room != null)
					{
						// Also send it to any network rooms (locally).
						room.SendLocally(writer);
					}
				}

				try
				{
					// Tell every server about it:
					foreach (var kvp in RemoteServers)
					{
						var server = kvp.Value;

						if (Verbose)
						{
							Console.WriteLine("[Delete " + typeof(T).Name + "]=>" + server.ServerId);
						}

						server.Send(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				return new ValueTask<T>(src);
			}, 1000); // Always absolutely last

		}

		/// <summary>
		/// Gets meta by the given lowercase name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ContentSyncTypeMeta GetMeta(string name)
		{
			RemoteTypes.TryGetValue(name, out ContentSyncTypeMeta meta);
			return meta;
		}

		private ConcurrentDictionary<string, ContentSyncTypeMeta> RemoteTypes = new ConcurrentDictionary<string, ContentSyncTypeMeta>();

		/// <summary>
		/// Informs CSync to start syncing the given type as the given opcode.
		/// </summary>
		/// <param name="service"></param>
		public void SyncRemoteType(AutoService service)
		{
			var handleTypeInternal = GetType().GetMethod(nameof(HandleTypeInternal)).MakeGenericMethod(new Type[] {
				service.ServicedType,
				service.IdType,
				service.InstanceType
			});

			handleTypeInternal.Invoke(this, new object[] {
				service
			});
		}

	}

	/// <summary>
	/// Stores sync meta for a given type.
	/// </summary>
	public class ContentSyncTypeMeta
	{
		/// <summary>
		/// The opcode
		/// </summary>
		public OpCode OpCode;

		/// <summary>
		/// Reads an object of this type from the given client.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="client"></param>
		/// <param name="remoteType"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public virtual void Handle(OpCode<SyncServerRemoteType>  opcode, Client client, SyncServerRemoteType remoteType, int action)
		{
		}
	}

	/// <summary>
	/// Stores sync meta for a given type.
	/// </summary>
	public class ContentSyncTypeMeta<T, ID, INST_T> : ContentSyncTypeMeta
		where T : Content<ID>, new()
		where INST_T : T, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// The reader/writer.
		/// </summary>
		public BoltReaderWriter<INST_T> ReaderWriter;

		/// <summary>
		/// The service for the type.
		/// </summary>
		public AutoService<T,ID> Service;

		private ServiceCache<T, ID> _primaryCache;

		private EventHandler<T, int> _receivedEventHandler;

		/// <summary>
		/// Creates new type meta.
		/// </summary>
		/// <param name="svc"></param>
		public ContentSyncTypeMeta(AutoService<T,ID> svc)
		{
			Service = svc;
			ReaderWriter = TypeIOEngine.GetBolt<INST_T>();

			_primaryCache = Service.GetCacheForLocale(1);
			_receivedEventHandler = Service.EventGroup.Received;
		}

		private async ValueTask OnReceiveUpdate(int action, uint localeId, T entity)
		{
			// Create the context using role 1:
			var context = new Context(localeId, null, 1);

			// Update local cache next:
			var cache = Service.GetCacheForLocale(context.LocaleId);

			T raw;

			if (context.LocaleId == 1)
			{
				// Primary locale. Entity == raw entity, and no transferring needs to happen.
				raw = entity;
			}
			else
			{
				// Get the raw entity from the cache. We'll copy the fields from the raw object to it.
				raw = cache.GetRaw(entity.Id);

				if (raw == null)
				{
					raw = new INST_T();
				}

				// Transfer fields from entity to raw, using the primary object as a source of blank fields.
				// If fields on the raw object were changed, this makes sure they're up to date.
				Service.PopulateRawEntityFromTarget(entity, raw, _primaryCache.Get(raw.Id));
			}

			// Received the content object:
			await _receivedEventHandler.Dispatch(context, entity, action);

			if (cache != null)
			{
				lock (cache)
				{
					if (action == 1 || action == 2)
					{
						// Created or updated
						cache.Add(context, entity, raw);

						if (context.LocaleId == 1)
						{
							// Primary locale update - must update all other caches in case they contain content from the primary locale.
							Service.OnPrimaryEntityChanged(entity);
						}
					}
					else if (action == 3)
					{
						// Deleted
						cache.Remove(context, entity.Id);
					}
				}
			}
		}

		/// <summary>
		/// Reads an object of this type from the given client.
		/// </summary>
		/// <param name="opcode"></param>
		/// <param name="client"></param>
		/// <param name="remoteType"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public override void Handle(OpCode<SyncServerRemoteType> opcode, Client client, SyncServerRemoteType remoteType, int action)
		{
			// Read with bolt IO from the stream:
			var obj = ReaderWriter.Read(client);

			_ = OnReceiveUpdate(action, remoteType.LocaleId, obj);

			// Release the message:
			remoteType.Release();
		}
	}

}
