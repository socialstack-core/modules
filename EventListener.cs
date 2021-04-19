
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.ContentSync
{

	/// <summary>
	/// Listens for the remote sync type added event which indicates syncable types.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		/// <summary>
		/// Instanced automatically
		/// </summary>
		public EventListener()
		{

			var methodInfo = GetType().GetMethod(nameof(SetupForType));

			// Must happen after services start otherwise the page service isn't necessarily available yet.
			// Notably this happens immediately after services start in the first group
			// (that's before any e.g. system pages are created).
			Events.Service.AfterCreate.AddEventListener(async (Context ctx, AutoService svc) =>
			{
				if (svc == null)
				{
					return svc;
				}

				// Does this service require sync or ID allocation? SetupForType checks internally.

				// Add Create handler.
				// When the handler fires, we simply assign an ID from our pool.
				// DatabaseService internally handles predefined IDs already.
				if (svc.ServicedType != null)
				{
					// Invoke setup for type:
					var setupType = methodInfo.MakeGenericMethod(new Type[] {
						svc.ServicedType,
						svc.IdType
					});

					await (setupType.Invoke(this, new object[] {
						svc
					}) as Task);
				}

				return svc;
			}, 5); // Before most things, but after db diff has setup the schema and ensured the tables exist.
		}

		private DatabaseService databaseService;
		private ContentSyncService contentSyncService;
		private ClusteredServerService clusteredServerService;

		/// <summary>
		/// Opcode to use when talking with other servers.
		/// </summary>
		private int OpCode;

		/// <summary>
		/// Sets up a particular content type with e.g. ID assign handlers.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		public async Task SetupForType<T, ID>(AutoService<T, ID> service) 
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			// Invoked by reflection

			if (!service.Synced)
			{
				// No need for contentSync on this one.
				return;
			}

			if (contentSyncService == null)
			{
				contentSyncService = Services.Get<ContentSyncService>();
				databaseService = Services.Get<DatabaseService>();
				clusteredServerService = Services.Get<ClusteredServerService>();

				// Not setup yet - hook up now.
				await contentSyncService.Startup();
			}

			var opcode = OpCode + 10;
			OpCode++;

			// Add as a remote synced type:
			contentSyncService.SyncRemoteType(service.ServicedType, opcode);

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
		}

		/// <summary>
		/// Adds a sequential ID assigner to the given service. Low frequency types only.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="service"></param>
		public void AddSequentialIdAssigner<T>(AutoService<T, uint> service)
			where T : Content<uint>, new()
		{
			var highestIdFilter = new Filter<T>();
			highestIdFilter.Sort("Id", "desc");
			highestIdFilter.PageSize = 1;

			service.EventGroup.BeforeCreate.AddEventListener(async (Context context, T content) =>
			{
				if (content == null)
				{
					return content;
				}

				// Assign an ID now! First get the current highest ID for this type:
				var latest = await service.List(context, highestIdFilter, DataOptions.IgnorePermissions);

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
			where ID : struct
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