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

			Events.Service.AfterCreate.AddEventListener(async (Context ctx, AutoService svc) =>
			{
				if (svc == null || svc.ServicedType == null)
				{
					return svc;
				}

				// Has CSync started yet?
				if (contentSyncService == null)
				{
					contentSyncService = Services.Get<ContentSyncService>();
				}

				if (contentSyncService == null)
				{
					// Still unavailable.
					pendingStartup.Add(svc);
					return svc;
				}

				// Start any that were pending startup, plus this one.
				if (pendingStartup != null)
				{
					var set = pendingStartup;
					pendingStartup = null;

					foreach (var pending in set)
					{
						await Setup(pending);
					}
				}

				await Setup(svc);

				return svc;
			}, 5); // Before most things. Ensures everything has an ID handler.
			
		}

		/// <summary>
		/// Sets up the given autoservice, potentially adding ID management etc to it.
		/// </summary>
		/// <param name="svc"></param>
		/// <returns></returns>
		private async ValueTask Setup(AutoService svc)
		{
			var setupServiceMethod = GetType().GetMethod(nameof(SetupService));
			
			// Setup network management if it is not a generic AutoService:
			var genericMethod = setupServiceMethod.MakeGenericMethod(new Type[] {
					svc.ServicedType,
					svc.IdType
				});

			var task = (Task)genericMethod.Invoke(this, new object[] { svc });
			await task;
		}

		private ContentSyncService contentSyncService;
		private ClusteredServerService clusteredServerService;

		private List<AutoService> pendingStartup = new List<AutoService>();

		/// <summary>
		/// Sets up a given non-mapping service.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="service"></param>
		/// <returns></returns>
		public async Task SetupService<T, ID>(AutoService<T,ID> service)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>

		{
			// Invoked by reflection

			/*
			 if (SyncFileMode && LocalTableSet != null)
			{
				// Add handlers to Create, Delete and Update events, and track these in a syncfile for this user.

				// Attempt to get the sync file for this type:
				LocalTableSet.Files.TryGetValue(tableName, out SyncTableFile localSyncFile);

				if (localSyncFile != null)
				{
					// Hook up create/ update/ delete - we want to track modding of objects:
					service.EventGroup.AfterCreate.AddEventListener((Context context, T content) =>
					{
						if (content == null)
						{
							return new ValueTask<T>(content);
						}

						// Write creation to sync file:
						localSyncFile.Write(content, 'C', context == null ? 0 : context.LocaleId);

						return new ValueTask<T>(content);
					});

					service.EventGroup.AfterUpdate.AddEventListener((Context context, T content) =>
					{
						if (content == null)
						{
							return new ValueTask<T>(content);
						}

						// Write update to sync file:
						localSyncFile.Write(content, 'U', context == null ? 0 : context.LocaleId);

						return new ValueTask<T>(content);
					});

					service.EventGroup.AfterDelete.AddEventListener((Context context, T content) =>
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
			 */

			if (clusteredServerService == null)
			{
				clusteredServerService = Services.Get<ClusteredServerService>();

				// Not setup yet - hook up now.
				await contentSyncService.Startup();
			}

			if (service.Synced || service.IsMapping)
			{
				// Add as a remote synced type:
				await contentSyncService.SyncRemoteType(service, true);

				// Create an ID assigner for the type next.
				var cacheConfig = service.GetCacheConfig();

				if (cacheConfig != null && cacheConfig.LowFrequencySequentialIds)
				{
					// Used by e.g. locales. The objects must be created at low frequency (to avoid collisions) but will always have exact, sequential IDs.
					if (service.IdType != typeof(uint))
					{
						throw new Exception("Type " + typeof(T).Name + " is set to use a sequential ID assigner. This is only available with an ID type of uint.");
					}

					// Now need it as an AutoService<T, uint> - we'll need to use reflection for that, as the compiler doesn't know that ID is actually typeof(uint).
					var addSequential = GetType().GetMethod(nameof(AddSequentialIdAssigner));

					var setupIdAssigner = addSequential.MakeGenericMethod(new Type[] {
						service.ServicedType
					});

					setupIdAssigner.Invoke(this, new object[] {
						service
					});
				}
				else
				{
					// Create a regular clustered ID assigner:
					var assigner = await contentSyncService.CreateAssigner(service);
					AddIdAssigner(assigner, service.EventGroup);
				}
			}
			else
			{
				// Ensure any network rooms are in sync.

				// Add as a remote synced type:
				await contentSyncService.SyncRemoteType(service, false);
			}

		}

		/// <summary>
		/// Adds a sequential ID assigner to the given service. Low frequency types only.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="service"></param>
		public void AddSequentialIdAssigner<T>(AutoService<T, uint> service)
			where T : Content<uint>, new()
		{
			service.EventGroup.BeforeCreate.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					return content;
				}

				// Assign an ID now! First get the current highest ID for this type:
				var f = service.Where(DataOptions.IgnorePermissions);
				f.Sort("Id", false);
				f.PageSize = 1;

				var latest = await f.ListAll(context);

				uint latestId = 0;

				if (latest != null && latest.Count > 0)
				{
					latestId = latest[0].GetId() + 1;
				}

				content.SetId(latestId);
				return content;
			}, 1);

		}

		/// <summary>
		/// Adds ID assigners to the given event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="assigner"></param>
		/// <param name="evtGroup"></param>
		private void AddIdAssigner<T, ID>(IdAssigner<ID> assigner, EventGroup<T, ID> evtGroup)
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			evtGroup.BeforeCreate.AddEventListener((Context context, T content) =>
			{
				if (content == null)
				{
					return new ValueTask<T>(content);
				}

				// Assign an ID now!
				if (content.Id.Equals(default(ID)))
				{
					content.Id = assigner.Assign();
				}

				return new ValueTask<T>(content);
			}, 1);
		}

	}


}