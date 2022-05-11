using System;
using Api.Startup;
using Api.Contexts;
using Api.Eventing;
using System.Threading.Tasks;
using Api.Database;
using System.Collections.Generic;
using Api.WebSockets;
using Api.NetworkNodes;

namespace Api.BlockDatabase;

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
		Events.WebSocket.SetUniqueTypeId.AddEventListener((Context context, NetworkRoomSet set) => {

			// Has network node svc started yet?
			if (networkNodeService == null)
			{
				networkNodeService = Services.Get<NetworkNodeService>();
			}

			// Lookup the blockchain definition ID for this type.
			uint roomTypeId = 0;

			set.SetRoomTypeId(roomTypeId);
			set.NodeId = networkNodeService.NodeId;

			return new ValueTask<NetworkRoomSet>(set);
		});

		Events.Service.AfterCreate.AddEventListener(async (Context ctx, AutoService svc) =>
		{
			if (svc == null || svc.ServicedType == null)
			{
				return svc;
			}

			// Has network node svc started yet?
			if (networkNodeService == null)
			{
				networkNodeService = Services.Get<NetworkNodeService>();
			}

			if (wsService == null)
			{
				wsService = Services.Get<WebSocketService>();
			}

			if (networkNodeService == null || wsService == null)
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

	private NetworkNodeService networkNodeService;
	private WebSocketService wsService;

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
		// Add metadata to the websocket system about this type:
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
					service
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
					service
				});
		}
	}


	/// <summary>
	/// Register a content type as an opcode.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="ID"></typeparam>
	/// <typeparam name="INST_T"></typeparam>
	/// <param name="svc"></param>
	public async Task AddStandardTypeInternal<T, ID, INST_T>(AutoService<T, ID> svc)
		where T : Content<ID>, new()
		where INST_T : T, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		// - Hook up the events which then builds the messages too

		// Create the room set:
		var rooms = await NetworkRoomSet<T, ID, ID>.CreateSet(svc, null);
		svc.StandardNetworkRooms = rooms;

		// Create the meta:
		var meta = new BlockNetworkRoomTypeStdMeta<T, ID, INST_T>(svc);

		var name = svc.InstanceType.Name.ToLower();

		wsService.RemoteTypes[name] = meta;
	}

	/// <summary>
	/// Register a content type as an opcode.
	/// </summary>
	/// <typeparam name="SRC_ID"></typeparam>
	/// <typeparam name="TARG_ID"></typeparam>
	/// <typeparam name="INST_T"></typeparam>
	/// <param name="svc"></param>
	public async Task AddMappingTypeInternal<SRC_ID, TARG_ID, INST_T>(MappingService<SRC_ID, TARG_ID> svc)
		where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
		where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
		where INST_T : Mapping<SRC_ID, TARG_ID>, new()
	{
		// - Hook up the events which then builds the messages too

		// Create the room set:
		var rooms = await NetworkRoomSet<Mapping<SRC_ID, TARG_ID>, uint, SRC_ID>.CreateSet(svc, null);
		svc.MappingNetworkRooms = rooms;

		// Create the meta:
		var meta = new BlockNetworkRoomTypeMappingMeta<SRC_ID, TARG_ID, INST_T>(svc);

		var name = svc.InstanceType.Name.ToLower();

		wsService.RemoteTypes[name] = meta;
	}
}