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
	/// ContentSync manages cache invalidations and related live messaging between a cluster of servers.
	/// It operates based on simply the amount of servers which start and register themselves in to the database.
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

			Events.Service.AfterStart.AddEventListener(async (Context ctx, object s) => {

				// Start:
				await Startup();
				return s;
			});

		}

		/// <summary>
		/// True if the sync file is active.
		/// </summary>
		private bool SyncFileMode = false;

		/// <summary>
		/// The name of this ContentSync host
		/// </summary>
		public string HostName;

		/// <summary>
		/// Sets up the config required to connect to other servers.
		/// </summary>
		/// <returns></returns>
		public async Task Startup()
		{
			if (Self != null)
			{
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

				return;
			}

			var ctx = new Context();

			// Get all servers:
			var allServers = await _clusteredServerService.Where(DataOptions.IgnorePermissions).ListAll(ctx);

			ClusteredServer self = null;

			uint maxId = 0;
			var anyDeleted = false;

			foreach (var server in allServers)
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
					"When copying data between environments, don't include this table as it wastes server IDs.");
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

			Self = self;
		}

		/// <summary>
		/// The clustered server representing this specific server. Has IP addresses setup and ready.
		/// </summary>
		public ClusteredServer Self;

		private object remoteServerLock = new object();

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
