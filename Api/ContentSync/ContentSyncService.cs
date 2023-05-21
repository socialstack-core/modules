using System;
using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Configuration;
using Api.SocketServerLibrary;
using Api.Startup;
using System.Linq;
using System.IO;
using System.Net;
using Api.Signatures;
using Api.WebSockets;
using System.Net.Http;
using Newtonsoft.Json;

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
		/// Servers connected to this one, indexed by its ID. To prevent ID allocation being wasteful, servers MUST be given ascending IDs, making this array particularly efficient.
		/// </summary>
		private ContentSyncServer[] RemoteServers = new ContentSyncServer[0];

		/// <summary>
		/// The port number for contentSync to use.
		/// </summary>
		public int Port {
			get {
				return _configuration.Port;
			}
		}

		/// <summary>
		/// Network room type service.
		/// </summary>
		private readonly NetworkRoomTypeService _nrts;

		private readonly WebSocketService _websocketService;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ContentSyncService(ClusteredServerService clusteredServerService, NetworkRoomTypeService nrts, WebSocketService websocketService)
		{
			// The content sync service is used to keep content created by multiple instances in sync.
			// (which can be a cluster of servers, or a group of developers)
			// It does this by setting up 'stripes' of IDs which are assigned to particular users.
			// A user is identified by the computer hostname.
			_nrts = nrts;
			_websocketService = websocketService;
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
				Log.Info(LogTag, "Content sync is in verbose mode - it will tell you each thing it syncs over your network.");
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

		/// <summary>
		/// Generate a diffset with the configured remote host.
		/// </summary>
		/// <param name="subfolder"></param>
		/// <returns></returns>
		public async Task<ContentFileDiffSet> Diff(string subfolder = null)
		{
			// Local file set:
			var fileList = new List<ContentFileInfo>();
			CollectFiles(subfolder, fileList);

			// Remote file set:
			var remoteFileList = await GetRemoteFileList(_configuration.UpstreamCookie, _configuration.UpstreamHost);

			return new ContentFileDiffSet(fileList, remoteFileList);
		}

		/// <summary>
		/// Sync a video from the upstream host.
		/// </summary>
		/// <param name="videoId"></param>
		/// <param name="firstChunk"></param>
		/// <param name="lastChunkId"></param>
		/// <returns></returns>
		public async Task<bool> VideoSync(int videoId, int firstChunk, int lastChunkId)
		{
			// Ends with /
			var localContentPath = AppSettings.Configuration["Content"];

			// m3u8:
			var path = "video/" + videoId + "/manifest.m3u8";
			var client = new HttpClient();
			var fileBytes = await client.GetByteArrayAsync(_configuration.UpstreamHost + "/content/" + path);

			var localPath = localContentPath + path;
			(new FileInfo(localPath)).Directory.Create();
			System.IO.File.WriteAllBytes(localPath, fileBytes);

			for (var i = firstChunk; i<= lastChunkId; i++)
			{
				path = "video/" + videoId + "/chunk" + i + ".ts";

				client = new HttpClient();
				fileBytes = await client.GetByteArrayAsync(_configuration.UpstreamHost + "/content/" + path);

				localPath = localContentPath + path;
				(new FileInfo(localPath)).Directory.Create();
				System.IO.File.WriteAllBytes(localPath, fileBytes);

				Log.Info(LogTag, "Chunk " + i + "/" + lastChunkId);
			}

			return true;
		}

		/// <summary>
		/// Get local files
		/// </summary>
		/// <param name="subfolder"></param>
		/// <returns></returns>
		public List<ContentFileInfo> GetLocalFiles(string subfolder = null)
		{
			var fileList = new List<ContentFileInfo>();

			CollectFiles(subfolder, fileList);

			return fileList;
		}

		/// <summary>
		/// Performs a sync with the configured upstream host.
		/// </summary>
		/// <param name="subfolder"></param>
		/// <returns></returns>
		public async Task<SyncStats> SyncContentFiles(string subfolder = null)
		{
			var diffset = await Diff(subfolder);

			// Ends with /
			var localContentPath = AppSettings.Configuration["Content"];

			// Download all the remote files next.
			var i = 0;

			foreach (var file in diffset.RemoteOnly)
			{
				i++;
				var client = new HttpClient();
				var fileBytes = await client.GetByteArrayAsync(_configuration.UpstreamHost + "/content/" + file.Path);

				var localPath = localContentPath + file.Path;
				(new FileInfo(localPath)).Directory.Create();
				System.IO.File.WriteAllBytes(localPath, fileBytes);
				System.IO.File.SetLastWriteTimeUtc(localPath, new DateTime(file.ModifiedTicksUtc));

				Log.Info(LogTag, i + "/" + diffset.RemoteOnly.Count);
			}

			return new SyncStats()
			{
				Downloaded = diffset.RemoteOnly.Count
			};
		}

		/// <summary>
		/// Gets a remote file list.
		/// </summary>
		/// <param name="remoteCookieValue"></param>
		/// <param name="remoteHost"></param>
		/// <returns></returns>
		private async Task<List<ContentFileInfo>> GetRemoteFileList(string remoteCookieValue, string remoteHost) // Remote host includes http(s)
		{
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Cookie", "user=" + remoteCookieValue);
			HttpResponseMessage tokenResponse = await client.GetAsync(remoteHost + "/v1/contentsync/fileset");
			var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<ContentFileInfo>>(jsonContent);
		}

		private void CollectFiles(string subfolder, List<ContentFileInfo> list)
		{

			// Ends with /
			var baseDir = AppSettings.Configuration["Content"];

			if (subfolder != null)
			{
				// Must not start with /
				baseDir += subfolder;
			}

			DirectoryInfo di = new DirectoryInfo(baseDir);
			var fnLength = di.FullName.Length;

			foreach (var file in di.EnumerateFiles("*", SearchOption.AllDirectories))
			{
				var cfi = new ContentFileInfo()
				{
					Size = file.Length,
					ModifiedTicksUtc = file.LastWriteTimeUtc.Ticks,
					Path = file.FullName.Substring(fnLength).Replace('\\', '/')
				};

				list.Add(cfi);
			}

		}
		
		private string FileSafeName(string name)
		{
			return new string(name.Where(ch => !InvalidFileNameChars.Contains(ch)).ToArray());
		}

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
                Log.Warn(LogTag, "A server has been deleted from the " + nameof(ClusteredServer) + " table because it was from a different environment. " +
					"When copying data between environments, don't include this table. " +
					"Doing so wastes server IDs and will in turn make your site assign large ID values unnecessarily.");
			}

			uint regionId = 0;
			uint serverTypeId = 0;
			uint hostPlatformId = 0;

			// Check for platform.json to obtain region etc:
			var root = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());

			var platformJson = root + "platform.json";

			try
			{
				var rawJson = File.ReadAllText(platformJson);

				var platformJsonContent = JsonConvert.DeserializeObject<PlatformJsonFile>(rawJson);

				regionId = platformJsonContent.Region;
				serverTypeId = platformJsonContent.Type;
				hostPlatformId = platformJsonContent.Host;
			}
			catch
			{
				// File didn't exist or we can't read it - this is fine to just completely ignore.
			}

			var ips = await IpDiscovery.Discover();

			if (self == null)
			{
				self = new ClusteredServer()
				{
					Port = Port,
					HostName = HostName,
					Environment = env,
					RegionId = regionId,
					ServerTypeId = serverTypeId,
					HostPlatformId = hostPlatformId
				};

				ips.CopyTo(self);

				AllServers.Add(self);

				await _clusteredServerService.Create(ctx, self, DataOptions.IgnorePermissions);

			}
			else if (ips.ChangedSince(self) || self.Environment != env || self.RegionId != regionId || self.ServerTypeId != serverTypeId || self.HostPlatformId != hostPlatformId)
			{
				// It changed - update it:
				await _clusteredServerService.Update(ctx, self, (Context c, ClusteredServer cs, ClusteredServer orig) => {

					ips.CopyTo(cs);
					cs.Environment = env;
					cs.RegionId = regionId;
					cs.ServerTypeId = serverTypeId;
					cs.HostPlatformId = hostPlatformId;

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
		/// Starts the cSync server. Must occur after other services have started.
		/// </summary>
		public void ApplyRemoteDataAndStart()
		{
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

			if (_configuration.GlobalCluster)
			{
				Log.Info(LogTag, "ContentSync running as global cluster.");
			}
			else
			{
				SyncServer.BindAddress = new IPAddress(Self.PrivateIPv4);
			}

			SyncServer.RegisterOpCode(8, (Client client, Writer entireMessage) => {

				// Get the referenced network room.

				// The message contains the following:
				// [1 byte, "8"]
				// [4 byte size]
				// [Network room type, compressed number]
				// [Network room ID, compressed number]

				int index = 5;
				int roomType = (int)entireMessage.ReadCompressedAt(ref index);
				ulong roomId = entireMessage.ReadCompressedAt(ref index);

				if (NetworkRoomLookup.NetworkRoomSets.Length > roomType)
				{
					var networkRoomSet = NetworkRoomLookup.NetworkRoomSets[roomType];

					if (networkRoomSet != null)
					{
						networkRoomSet.ForwardToRoom(roomId, entireMessage);
					}
				}

				// Done:
				entireMessage.Release();

			});

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
				server.Id = theirId;
				AddRemote(server);
				
				// Let it know that we're happy:
				var msg = SyncServerHandshakeResponse.Get();
				msg.ServerId = ServerId;
				var response = msg.Write(4);

				// Respond with hello response:
				server.Send(response);
				response.Release();
				msg.Release();

				Log.Info(LogTag, "Connected to " + theirId);
			});

			HandshakeOpCode.IsHello = true;
			
			SyncServer.RegisterOpCode(4, (Client client, SyncServerHandshakeResponse message) => {
				var server = (client as ContentSyncServer);

				if (!server.Hello)
				{
					// Hello other server! Add it to lookup:
					server.Id = message.ServerId;
					AddRemote(server);

				}

				Log.Info(LogTag, "Connected to " + message.ServerId);
			});

			var reader = new SyncServerRemoteReader(1, _websocketService);
			reader.OpCode = SyncServer.RegisterOpCode(21, (Client client, SyncServerRemoteType message) => {
				// Note: this callback is never run. The remoteReader does all the work, as it can identify the concrete types of things.
			}, reader);

			reader = new SyncServerRemoteReader(2, _websocketService);
			reader.OpCode = SyncServer.RegisterOpCode(22, (Client client, SyncServerRemoteType message) => {
				// Note: this callback is never run. The remoteReader does all the work, as it can identify the concrete types of things.
			}, reader);

			reader = new SyncServerRemoteReader(3, _websocketService);
			reader.OpCode = SyncServer.RegisterOpCode(23, (Client client, SyncServerRemoteType message) => {
				// Note: this callback is never run. The remoteReader does all the work, as it can identify the concrete types of things.
			}, reader);

			// After HandleType calls so it can register some of the handlers:
			SyncServer.Start();

			if (Services.HostMapping != null && !Services.HostMapping.ShouldSync)
			{
				Log.Info(LogTag, "ContentSync disabled on this node because appsettings declares the hostType '" + Services.HostType + "' should not sync.");
				return;
			}

			// Try to explicitly connect to the other servers. Note that AllServers is setup by EventListener.
			// We might be the first one up, so some of these can outright fail.
			// That's ok though - they'll contact us instead.
			Log.Info(LogTag, "Started connecting to peers");

			var env = Services.Environment;

			foreach (var serverInfo in AllServers)
			{
				if (serverInfo == Self || serverInfo.Environment != env)
				{
					continue;
				}

				var hostMapping = Services.GetHostMapping(serverInfo.HostName);

				if (!hostMapping.ShouldSync)
				{
					continue;
				}

				// Is it the same datacenter?
				// If not, use the public IP.
				IPAddress ipAddress;

				if (serverInfo.HostPlatformId != Self.HostPlatformId || serverInfo.RegionId != Self.RegionId)
				{
					ipAddress = new IPAddress(serverInfo.PublicIPv4);
				}
				else
				{
					ipAddress = new IPAddress(serverInfo.PrivateIPv4);
				}

				Log.Info(LogTag, "Connect to " + ipAddress);

				SyncServer.ConnectTo(ipAddress, serverInfo.Port, serverInfo.Id, (ContentSyncServer s) => {
					s.Id = serverInfo.Id;
				});
			}

		}

		/// <summary>
		/// Gets the given server. Returns null if it is "this".
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ContentSyncServer GetServer(uint id)
		{
			if (id == Self.Id || id == 0 || id>RemoteServers.Length)
			{
				return null;
			}
			return RemoteServers[id - 1];
		}

		private void AddRemote(ContentSyncServer server)
		{
			lock (remoteServerLock)
			{
				if (server.Id > RemoteServers.Length)
				{
					// Must resize it.
					// This array benefits most from not having any spaces. 
					// I.e. if it resizes a bunch of times during startup, it's less of a drain than constantly iterating over gaps.
					Array.Resize(ref RemoteServers, (int)server.Id);
				}
			}

			RemoteServers[server.Id - 1] = server;
		}

		/// <summary>
		/// All servers (including "me").
		/// </summary>
		public List<ClusteredServer> AllServers;

		/// <summary>
		/// The clustered server representing this specific server. Has IP addresses setup and ready.
		/// </summary>
		public ClusteredServer Self;

		private object remoteServerLock = new object();

		/// <summary>
		/// Removes the given server from the lookups.
		/// </summary>
		/// <param name="server"></param>
		public void RemoveServer(ContentSyncServer server)
		{
			lock (remoteServerLock)
			{
				if (RemoteServers.Length >= server.Id)
				{
					RemoteServers[server.Id - 1] = null;
				}
			}
		}

		/// <summary>
		/// Register a content type as an opcode.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <typeparam name="INST_T"></typeparam>
		/// <param name="svc"></param>
		/// <param name="sendToEveryServer"></param>
		public async Task AddStandardTypeInternal<T, ID, INST_T>(AutoService<T, ID> svc, bool sendToEveryServer)
			where T : Content<ID>, new()
			where INST_T : T, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			// - Hook up the events which then builds the messages too

			// Get the network room mapping.
			// This is done on type startup to ensure that it is cached and syncing.
			var mapping = await MappingTypeEngine.GetOrGenerate(
					svc,
					Services.Get<ClusteredServerService>(),
					"NetworkRoomServers",
					"host"
				) as MappingService<ID, uint>;

			// Scan the mapping to purge any entries for this server:
			await mapping.DeleteByTarget(new Context(), ServerId, DataOptions.IgnorePermissions);

			// Create the room set:
			var rooms = await NetworkRoomSet<T, ID, ID>.CreateSet(svc, mapping);
			svc.StandardNetworkRooms = rooms;

			// Create the meta:
			var meta = new ContentSyncStandardMeta<T, ID, INST_T>(svc);

			var name = svc.InstanceType.Name.ToLower();

			_websocketService.RemoteTypes[name] = meta;

			// Get the event group:
			var eventGroup = svc.EventGroup;
			
			// Same as the generic message handler - opcode + 4 byte size.
			var basicHeader = new byte[5];
			var boltIO = meta.ReaderWriter;

			// Type name buffer:
			var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);

			// Create is unlike update and delete for a non-mapping type.
			eventGroup.AfterCreate.AddEventListener(async (Context ctx, T src) =>
			{
				if (src == null)
				{
					return src;
				}

				// Start creating the sync message. This is much the same as what Message does internally when sending generic messages.
				// We just require some custom handling here to handle the type-within-a-message scenario.
				NetworkRoom<T, ID, ID> globalMsgRoom = rooms.AnyUpdateRoom;

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

				if (globalMsgRoom != null)
				{
					await globalMsgRoom.SendLocallyIfPermitted(src, 21);
				}
				
				try
				{
					if (sendToEveryServer)
					{
						// Tell every server about it:
						var set = RemoteServers;

						for (var i = 0; i < set.Length; i++)
						{
							var server = set[i];

							if (server == null)
							{
								continue;
							}

							server.Send(writer);
						}
					}
					else
					{
						// Send only to room relevant servers:
						globalMsgRoom.SendRemote(writer);
					}
					
					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				return src;
			}, 1000); // Always absolutely last
			
			eventGroup.AfterUpdate.AddEventListener(async (Context ctx, T src) =>
			{
				if (src == null)
				{
					return src;
				}

				// Get the network room:
				// It's created just in case there is no local clients but there is remote ones.
				NetworkRoom<T, ID, ID> netRoom;
				NetworkRoom<T, ID, ID> globalMsgRoom = rooms.AnyUpdateRoom;

				if (sendToEveryServer)
				{
					// Local room delivery only - get the room only if it exists; no need to instance it.
					netRoom = rooms.GetRoom(src.Id);
				}
				else
				{
					// Room required in this scenario.
					// We need to know if it has remote servers to send to.
					netRoom = rooms.GetOrCreateRoom(src.Id);

					if (netRoom.IsEmpty && globalMsgRoom.IsEmpty)
					{
						// Not sending to every server and the network room is empty. Do nothing.
						return src;
					}
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

				// Send to network room:
				if (netRoom != null)
				{
					await netRoom.SendLocallyIfPermitted(src, 22);
				}

				if (globalMsgRoom != null)
				{
					await globalMsgRoom.SendLocallyIfPermitted(src, 22);
				}

				try
				{
					if (sendToEveryServer)
					{
						// Tell every server about it:
						var set = RemoteServers;

						for (var i = 0; i < set.Length; i++)
						{
							var server = set[i];

							if (server == null)
							{
								continue;
							}

							server.Send(writer);
						}
					}
					else
					{
						// Send only to room relevant servers:
						netRoom.SendRemote(writer);
						globalMsgRoom.SendRemote(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				return src;
			}, 1000); // Always absolutely last

			eventGroup.AfterDelete.AddEventListener(async (Context ctx, T src) =>
			{
				if (src == null)
				{
					return src;
				}

				// Get the network room:
				// It's created just in case there is no local clients but there is remote ones.
				NetworkRoom<T, ID, ID> netRoom;
				NetworkRoom<T, ID, ID> globalMsgRoom = rooms.AnyUpdateRoom;

				if (sendToEveryServer)
				{
					// Local room delivery only - get the room only if it exists; no need to instance it.
					netRoom = rooms.GetRoom(src.Id);
				}
				else
				{
					// Room required in this scenario.
					// We need to know if it has remote servers to send to.
					netRoom = rooms.GetOrCreateRoom(src.Id);

					if (netRoom.IsEmpty && globalMsgRoom.IsEmpty)
					{
						// Not sending to every server and the network room is empty. Do nothing.
						return src;
					}
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

				// Send to network room:
				if (netRoom != null)
				{
					await netRoom.SendLocallyIfPermitted(src, 23);
				}

				if (globalMsgRoom != null)
				{
					await globalMsgRoom.SendLocallyIfPermitted(src, 23);
				}

				try
				{
					if (sendToEveryServer)
					{
						// Tell every server about it:
						var set = RemoteServers;

						for (var i = 0; i < set.Length; i++)
						{
							var server = set[i];

							if (server == null)
							{
								continue;
							}

							server.Send(writer);
						}
					}
					else
					{
						// Send only to room relevant servers:
						netRoom.SendRemote(writer);
						globalMsgRoom.SendRemote(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				return src;
			}, 1000); // Always absolutely last

		}

		/// <summary>
		/// Register a content type as an opcode.
		/// </summary>
		/// <typeparam name="SRC_ID"></typeparam>
		/// <typeparam name="TARG_ID"></typeparam>
		/// <typeparam name="INST_T"></typeparam>
		/// <param name="svc"></param>
		/// <param name="sendToEveryServer"></param>
		public async Task AddMappingTypeInternal<SRC_ID, TARG_ID, INST_T>(MappingService<SRC_ID, TARG_ID> svc, bool sendToEveryServer)
			where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
			where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
			where INST_T : Mapping<SRC_ID, TARG_ID>, new()
		{
			// - Hook up the events which then builds the messages too

			// Create the room set:
			var rooms = await NetworkRoomSet<Mapping<SRC_ID, TARG_ID>, uint, SRC_ID>.CreateSet(svc, null);
			svc.MappingNetworkRooms = rooms;

			// Create the meta:
			var meta = new ContentSyncMappingdMeta<SRC_ID, TARG_ID, INST_T>(svc);

			var name = svc.InstanceType.Name.ToLower();

			_websocketService.RemoteTypes[name] = meta;

			// Get the event group:
			var eventGroup = svc.EventGroup;

			// Same as the generic message handler - opcode + 4 byte size.
			var basicHeader = new byte[5];
			var boltIO = meta.ReaderWriter;

			// Type name buffer:
			var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);

			// Add the event listeners now!
			eventGroup.AfterCreate.AddEventListener(async (Context ctx, Mapping<SRC_ID, TARG_ID> src) =>
			{
				if (src == null)
				{
					return src;
				}

				// Get the network room:
				// It's created just in case there is no local clients but there is remote ones.
				NetworkRoom<Mapping<SRC_ID, TARG_ID>, uint, SRC_ID> netRoom;

				if (sendToEveryServer)
				{
					// Local room delivery only - get the room only if it exists; no need to instance it.
					netRoom = rooms.GetRoom(src.SourceId);
				}
				else
				{
					// Room required in this scenario.
					// We need to know if it has remote servers to send to.
					netRoom = rooms.GetOrCreateRoom(src.SourceId);

					if (netRoom.IsEmpty)
					{
						// Not sending to every server and the network room is empty. Do nothing.
						return src;
					}
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

				// Send to network room:
				if (netRoom != null)
				{
					await netRoom.SendLocallyIfPermitted(src, 21);
				}

				try
				{
					if (sendToEveryServer)
					{
						// Tell every server about it:
						var set = RemoteServers;

						for (var i = 0; i < set.Length; i++)
						{
							var server = set[i];

							if (server == null)
							{
								continue;
							}

							server.Send(writer);
						}
					}
					else
					{
						// Send only to room relevant servers:
						netRoom.SendRemote(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				return src;
			}, 1000); // Always absolutely last

			eventGroup.AfterUpdate.AddEventListener(async (Context ctx, Mapping<SRC_ID, TARG_ID> src) =>
			{
				if (src == null)
				{
					return src;
				}

				// Get the network room:
				// It's created just in case there is no local clients but there is remote ones.
				NetworkRoom<Mapping<SRC_ID, TARG_ID>, uint, SRC_ID> netRoom;

				if (sendToEveryServer)
				{
					// Local room delivery only - get the room only if it exists; no need to instance it.
					netRoom = rooms.GetRoom(src.SourceId);
				}
				else
				{
					// Room required in this scenario.
					// We need to know if it has remote servers to send to.
					netRoom = rooms.GetOrCreateRoom(src.SourceId);

					if (netRoom.IsEmpty)
					{
						// Not sending to every server and the network room is empty. Do nothing.
						return src;
					}
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

				// Send to network room:
				if (netRoom != null)
				{
					await netRoom.SendLocallyIfPermitted(src, 22);
				}

				try
				{
					if (sendToEveryServer)
					{
						// Tell every server about it:
						var set = RemoteServers;

						for (var i = 0; i < set.Length; i++)
						{
							var server = set[i];

							if (server == null)
							{
								continue;
							}

							server.Send(writer);
						}
					}
					else
					{
						// Send only to room relevant servers:
						netRoom.SendRemote(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				return src;
			}, 1000); // Always absolutely last

			eventGroup.AfterDelete.AddEventListener(async (Context ctx, Mapping<SRC_ID, TARG_ID> src) =>
			{
				if (src == null)
				{
					return src;
				}

				// Get the network room:
				// It's created just in case there is no local clients but there is remote ones.
				NetworkRoom<Mapping<SRC_ID, TARG_ID>, uint, SRC_ID> netRoom;

				if (sendToEveryServer)
				{
					// Local room delivery only - get the room only if it exists; no need to instance it.
					netRoom = rooms.GetRoom(src.SourceId);
				}
				else
				{
					// Room required in this scenario.
					// We need to know if it has remote servers to send to.
					netRoom = rooms.GetOrCreateRoom(src.SourceId);

					if (netRoom.IsEmpty)
					{
						// Not sending to every server and the network room is empty. Do nothing.
						return src;
					}
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

				// Send to network room:
				if (netRoom != null)
				{
					await netRoom.SendLocallyIfPermitted(src, 23);
				}

				try
				{
					if (sendToEveryServer)
					{
						// Tell every server about it:
						var set = RemoteServers;

						for (var i = 0; i < set.Length; i++)
						{
							var server = set[i];

							if (server == null)
							{
								continue;
							}

							server.Send(writer);
						}
					}
					else
					{
						// Send only to room relevant servers:
						netRoom.SendRemote(writer);
					}

					writer.Release();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				return src;
			}, 1000); // Always absolutely last

		}

		/// <summary>
		/// Informs CSync to start syncing the given type as the given opcode.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="sendToEveryServer"></param>
		public async ValueTask SyncRemoteType(AutoService service, bool sendToEveryServer)
		{
			if (service.IsMapping)
			{
				// Mappings are handled slightly differently due to the way how they use the SourceId as the network room ID.

				// Mapping<SRC_ID, TARG_ID>
				var mappingFullArgs = service.GetType().BaseType.GetGenericArguments();

				var handleTypeInternal = GetType().GetMethod(nameof(AddMappingTypeInternal)).MakeGenericMethod(new Type[] {
					mappingFullArgs[0],
					mappingFullArgs[1],
					service.InstanceType
				});

				await (Task)handleTypeInternal.Invoke(this, new object[] {
					service,
					sendToEveryServer
				});

			}
			else
			{
				var handleTypeInternal = GetType().GetMethod(nameof(AddStandardTypeInternal)).MakeGenericMethod(new Type[] {
					service.ServicedType,
					service.IdType,
					service.InstanceType
				});

				await (Task)handleTypeInternal.Invoke(this, new object[] {
					service,
					sendToEveryServer
				});
			}
		}

	}

	/// <summary>
	/// Stores sync meta for a given type.
	/// </summary>
	public class ContentSyncStandardMeta<T, ID, INST_T> : NetworkRoomTypeMeta
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
		public AutoService<T, ID> Service;

		private ServiceCache<T, ID> _primaryCache;

		private EventHandler<T, int> _receivedEventHandler;

		private Capability _loadCapability;

		/// <summary>
		/// Gets the load capability from the host service.
		/// </summary>
		public override Capability LoadCapability
		{
			get
			{
				if (_loadCapability == null)
				{
					_loadCapability = Service.EventGroup.GetLoadCapability();
				}

				return _loadCapability;
			}
		}

		/// <summary>
		/// Creates new type meta.
		/// </summary>
		/// <param name="svc"></param>
		public ContentSyncStandardMeta(AutoService<T, ID> svc)
		{
			Service = svc;
			ReaderWriter = BoltReaderWriter.Get<INST_T>();

			_primaryCache = Service.GetCacheForLocale(1);
			_receivedEventHandler = Service.EventGroup.Received;
		}

		/// <summary>
		/// Gets or creates the network room of the given ID.
		/// </summary>
		/// <param name="roomId"></param>
		public override NetworkRoom GetOrCreateRoom(ulong roomId)
		{
			return Service.StandardNetworkRooms.GetOrCreateRoom(Service.ConvertId(roomId));
		}

		private async ValueTask OnReceiveUpdate(int action, uint localeId, T entity)
		{
			try {
				// Update local cache next:
				var cache = Service.GetCacheForLocale(localeId);

				// The cache will be null if this is a non-cached type.
				// That can happen if it was identified that an object needed to be synced specifically for network rooms.
				if (cache != null)
				{
					// Create the context using role 1:
					var context = new Context(localeId, null, 1);

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

				// Network room update next.
				// The 20 is because action 1 (create) maps to opcode 21. Action 2 => 22 and action 3 => 23.
				var room = Service.StandardNetworkRooms.GetRoom(entity.Id);

				NetworkRoom<T, ID, ID> globalMsgRoom = Service.StandardNetworkRooms.AnyUpdateRoom;

				if (room != null)
				{
					await room.SendLocallyIfPermitted(entity, (byte)(action + 20));
				}

				if (globalMsgRoom != null)
				{
					await globalMsgRoom.SendLocallyIfPermitted(entity, (byte)(action + 20));
				}

			}
			catch(Exception e)
			{
				Log.Warn("contentsyncservice", e, "Sync encountered non-fatal error.");
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


	/// <summary>
	/// Stores sync meta for a given type.
	/// </summary>
	public class ContentSyncMappingdMeta<SRC_ID, TARG_ID, INST_T> : NetworkRoomTypeMeta
		where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
		where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
		where INST_T : Mapping<SRC_ID, TARG_ID>, new()
	{
		/// <summary>
		/// The reader/writer.
		/// </summary>
		public BoltReaderWriter<INST_T> ReaderWriter;

		/// <summary>
		/// The service for the type.
		/// </summary>
		public MappingService<SRC_ID, TARG_ID> Service;

		private ServiceCache<Mapping<SRC_ID, TARG_ID>, uint> cache;

		private EventHandler<Mapping<SRC_ID, TARG_ID>, int> _receivedEventHandler;

		private Context context;

		private Capability _loadCapability;

		/// <summary>
		/// True if this is a mapping type.
		/// </summary>
		public override bool IsMapping
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Gets the load capability from the host service.
		/// </summary>
		public override Capability LoadCapability
		{
			get
			{
				if (_loadCapability == null)
				{
					_loadCapability = Service.EventGroup.GetLoadCapability();
				}

				return _loadCapability;
			}
		}
		
		/// <summary>
		/// Creates new type meta.
		/// </summary>
		/// <param name="svc"></param>
		public ContentSyncMappingdMeta(MappingService<SRC_ID, TARG_ID> svc)
		{
			Service = svc;
			ReaderWriter = BoltReaderWriter.Get<INST_T>();

			cache = Service.GetCacheForLocale(1);
			_receivedEventHandler = Service.EventGroup.Received;

			// Mappings are always locale free.
			context = new Context(1, null, 1);


			if (typeof(SRC_ID) == typeof(uint))
			{
				_srcIdConverter = new UInt32IDConverter() as IDConverter<SRC_ID>;
			}
			else if (typeof(SRC_ID) == typeof(ulong))
			{
				_srcIdConverter = new UInt64IDConverter() as IDConverter<SRC_ID>;
			}
			else
			{
				throw new ArgumentException("Currently unrecognised ID type: ", nameof(SRC_ID));
			}

		}

		private IDConverter<SRC_ID> _srcIdConverter;

		private async ValueTask OnReceiveUpdate(int action, uint localeId, INST_T entity)
		{
			try
			{
				// Update local cache next.

				// The cache will be null if this is a non-cached type.
				// That can happen if it was identified that an object needed to be synced specifically for network rooms.
				if (cache != null)
				{
					Mapping<SRC_ID, TARG_ID> raw = entity;

					// Received the content object:
					await _receivedEventHandler.Dispatch(context, entity, action);
					
					lock (cache)
					{
						if (action == 1 || action == 2)
						{
							// Created or updated
							cache.Add(context, entity, raw);
									
							// Primary locale update - must update all other caches in case they contain content from the primary locale.
							Service.OnPrimaryEntityChanged(entity);
						}
						else if (action == 3)
						{
							// Deleted
							cache.Remove(context, entity.Id);
						}
					}
				}

				// Network room update next.
				// The 20 is because action 1 (create) maps to opcode 21. Action 2 => 22 and action 3 => 23.
				var room = Service.MappingNetworkRooms.GetRoom(entity.SourceId);

				if (room != null)
				{
					await room.SendLocallyIfPermitted(entity, (byte)(action + 20));
				}

			}
			catch (Exception e)
			{
				Log.Warn("contentsyncservice", e, "Sync encountered a non-fatal error.");
			}
		}

		/// <summary>
		/// Gets or creates the network room of the given ID.
		/// </summary>
		/// <param name="roomId"></param>
		public override NetworkRoom GetOrCreateRoom(ulong roomId)
		{
			return Service.MappingNetworkRooms.GetOrCreateRoom(_srcIdConverter.Convert(roomId));
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

	/// <summary>
	/// The platform JSON file.
	/// </summary>
	public class PlatformJsonFile
	{

		/// <summary>
		/// The server region ID.
		/// </summary>
		public uint Region { get; set; }

		/// <summary>
		/// The server host ID.
		/// </summary>
		public uint Host { get; set; }

		/// <summary>
		/// The server type ID.
		/// </summary>
		public uint Type { get; set; }

	}
}
