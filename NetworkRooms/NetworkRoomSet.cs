using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;


namespace Api.ContentSync
{

	/// <summary>
	/// Used to find network rooms based on type ID.
	/// </summary>
	public static class NetworkRoomLookup
	{
		/// <summary>
		/// Network room sets by type ID.
		/// </summary>
		public static NetworkRoomSet[] NetworkRoomSets = new NetworkRoomSet[50];	

	}

	/// <summary>
	/// A set of network rooms.
	/// </summary>
	public class NetworkRoomSet
	{

		/// <summary>
		/// The content sync service.
		/// </summary>
		public ContentSyncService ContentSync;

		/// <summary>
		/// An admin context to use when creating mappings.
		/// </summary>
		public Context ServerContext;

		/// <summary>
		/// A role 3 context for live messages.
		/// </summary>
		public Context SharedGuestContext;

		/// <summary>
		/// Forwards the given complete message to a room in this set.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="completeMessage"></param>
		public virtual void ForwardToRoom(ulong roomId, Writer completeMessage)
		{
		}
	}

	/// <summary>
	/// Set of network rooms for a given service.
	/// </summary>
	public partial class NetworkRoomSet<T, ID, ROOM_ID> : NetworkRoomSet
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		where ROOM_ID : struct, IConvertible, IEquatable<ROOM_ID>, IComparable<ROOM_ID>
	{
		/// <summary>
		/// The service this set is for.
		/// </summary>
		public AutoService<T, ID> Service;

		/// <summary>
		/// Converts IDs to and from ROOM_ID. It's most often the same as the services IdConverter.
		/// </summary>
		public IDConverter<ROOM_ID> IdConverter;

		/// <summary>
		/// The mapping of servers listening to particular IDs of things in this service.
		/// Can be null if this is itself a mapping, which are always cached.
		/// </summary>
		public MappingService<ROOM_ID, uint> RemoteServers;

		/// <summary>
		/// A room used for hosting "any" updates on this type. Not used by mapping network room sets.
		/// </summary>
		public NetworkRoom<T, ID, ROOM_ID> AnyUpdateRoom;

		/// <summary>
		/// Creates a network room set.
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="mapping"></param>
		/// <param name="contentSync"></param>
		/// <returns></returns>
		public static async ValueTask<NetworkRoomSet<T, ID, ROOM_ID>> CreateSet(AutoService<T, ID> svc, MappingService<ROOM_ID, uint> mapping, ContentSyncService contentSync)
		{
			var s = new NetworkRoomSet<T, ID, ROOM_ID>(svc, mapping, contentSync);
			await s.SetupTypeId(new Context(), contentSync._nrts);
			return s;
		}

		/// <summary>
		/// Forwards the given complete message to a room in this set.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="completeMessage"></param>
		public override void ForwardToRoom(ulong roomId, Writer completeMessage)
		{
			var typedRoomId = IdConverter.Convert(roomId);

			// get the room:
			var roomToSendTo = GetRoom(typedRoomId);

			if (roomToSendTo == null)
			{
				return;
			}

			// Send to the room:
			roomToSendTo.SendLocally(completeMessage);
		}

		/// <summary>
		/// Creates a network room set.
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="mapping"></param>
		/// <param name="contentSync"></param>
		private NetworkRoomSet(AutoService<T, ID> svc, MappingService<ROOM_ID, uint> mapping, ContentSyncService contentSync)
		{
			Service = svc;
			ContentSync = contentSync;
			RemoteServers = mapping;
			ServerContext = new Context(1, null, 1);
			SharedGuestContext = new Context(1, null, 3);

			AnyUpdateRoom = GetOrCreateRoom(default(ROOM_ID));

			if (typeof(ROOM_ID) == typeof(uint))
			{
				IdConverter = new UInt32IDConverter() as IDConverter<ROOM_ID>;
			}
			else if (typeof(ROOM_ID) == typeof(ulong))
			{
				IdConverter = new UInt64IDConverter() as IDConverter<ROOM_ID>;
			}
			else
			{
				throw new ArgumentException("Currently unrecognised ID type: ", nameof(ROOM_ID));
			}

		}

		/// <summary>
		/// Sets up the network room type ID.
		/// </summary>
		/// <returns></returns>
		public async ValueTask SetupTypeId(Context context, NetworkRoomTypeService nrtService)
		{
			// Unique name is the mapping name, unless its null, in which case use svc name_M (because svc is a mapping).
			string uniqueName;

			if (RemoteServers == null)
			{
				uniqueName = Service.InstanceType.Name + "_m";
			}
			else
			{
				uniqueName = RemoteServers.InstanceType.Name;
			}

			// Get type ID:
			var tn = await nrtService
				.Where("TypeName=?", DataOptions.IgnorePermissions)
				.Bind(uniqueName)
				.First(context);

			if (tn == null)
			{
				// Create it:
				var entry = await nrtService.Create(context, new () {
					TypeName = uniqueName
				}, DataOptions.IgnorePermissions);

				RoomTypeId = entry.Id;
			}
			else
			{
				RoomTypeId = tn.Id;
			}

			if (RoomTypeId > 2000)
			{
				throw new Exception("Your _networkroomtype table needs tidying up as it has assigned an abnormally large ID. Truncate it and restart this server.");
			}

			if (RoomTypeId >= NetworkRoomLookup.NetworkRoomSets.Length)
			{
				// Resize:
				Array.Resize(ref NetworkRoomLookup.NetworkRoomSets, (int)RoomTypeId + 10);
			}

			// Add:
			NetworkRoomLookup.NetworkRoomSets[RoomTypeId] = this;
		}

		/// <summary>
		/// The globally unique room type ID.
		/// </summary>
		public uint RoomTypeId;

		private ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>> _rooms = new ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>>();

		/// <summary>
		/// Gets the room for the object with the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public NetworkRoom<T, ID, ROOM_ID> GetRoom(ROOM_ID id)
		{
			_rooms.TryGetValue(id, out NetworkRoom<T, ID, ROOM_ID> result);
			return result;
		}

		/// <summary>
		/// Gets the room for the object with the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public NetworkRoom<T, ID, ROOM_ID> GetOrCreateRoom(ROOM_ID id)
		{
			if (!_rooms.TryGetValue(id, out NetworkRoom<T, ID, ROOM_ID> result))
			{
				result = new NetworkRoom<T, ID, ROOM_ID>();
				result.Id = id;
				result.ParentSet = this;
				if (!_rooms.TryAdd(id, result))
				{
					// Get it again - we'll abandon the one we just made:
					_rooms.TryGetValue(id, out result);
				}
			}

			return result;
		}
	}
	
}