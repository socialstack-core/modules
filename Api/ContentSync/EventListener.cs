using System;
using Api.Startup;
using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;
using Api.Database;
using System.Collections.Generic;
using Api.Users;
using Api.WebSockets;
using Api.SocketServerLibrary;

namespace Api.ContentSync
{

	/// <summary>
	/// Listens for service starts so it can start syncing it.
	/// </summary>
	[EventListener]
	public class EventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Setup network rooms
			Events.WebSocket.BeforeStart.AddEventListener(async (Context ctx, Server<WebSocketClient> server) => {

				var contentSync = Services.Get<ContentSyncService>();
				var userService = Services.Get<UserService>();

				// Create personal room set:
				var personalRoomMap = await MappingTypeEngine.GetOrGenerate(
						userService,
						Services.Get<ClusteredServerService>(),
						"PersonalRoomServers",
						"host"
					) as MappingService<uint, uint>;

				// Scan the mapping to purge any entries for this server:
				await personalRoomMap.DeleteByTarget(new Context(), contentSync.ServerId, DataOptions.IgnorePermissions);

				Services.Get<WebSocketService>().PersonalRooms = await NetworkRoomSet<User, uint, uint>.CreateSet(userService, personalRoomMap);

				return server;
			});

			Events.WebSocket.SetUniqueTypeId.AddEventListener(async (Context context, NetworkRoomSet set) => {

				if (contentSyncService == null)
				{
					contentSyncService = Services.Get<ContentSyncService>();
				}

				// Unique name is the mapping name, unless its null, in which case use svc name_M (because svc is a mapping).
				string uniqueName = set.UniqueName();

				var nrtService = Services.Get<NetworkRoomTypeService>();

				// Get type ID:
				var tn = await nrtService
					.Where("TypeName=?", DataOptions.IgnorePermissions)
					.Bind(uniqueName)
					.First(context);

				uint roomTypeId = 0;

				if (tn == null)
				{
					// Create it:
					var entry = await nrtService.Create(context, new()
					{
						TypeName = uniqueName
					}, DataOptions.IgnorePermissions);

					roomTypeId = entry.Id;
				}
				else
				{
					roomTypeId = tn.Id;
				}

				if (roomTypeId > 2000)
				{
					throw new Exception("Your _networkroomtype table needs tidying up as it has assigned an abnormally large ID. Truncate it and restart this server.");
				}

				set.SetRoomTypeId(roomTypeId);

				if (roomTypeId >= NetworkRoomLookup.NetworkRoomSets.Length)
				{
					// Resize:
					Array.Resize(ref NetworkRoomLookup.NetworkRoomSets, (int)roomTypeId + 10);
				}

				// Add to csync's lookup:
				NetworkRoomLookup.NetworkRoomSets[roomTypeId] = set;
				set.NodeId = contentSyncService.ServerId;

				return set;
			});

		}

		private ContentSyncService contentSyncService;
		private ClusteredServerService clusteredServerService;

	}


}