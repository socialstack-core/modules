using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.SocketServerLibrary;
using Api.Startup;


namespace Api.WebSockets
{
	/// <summary>
	/// A set of network rooms.
	/// </summary>
	public class NetworkRoomSet
	{
		/// <summary>
		/// The cluster node that we're currently on.
		/// </summary>
		public uint NodeId;

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

		/// <summary>
		/// A unique type name for this set.
		/// </summary>
		/// <returns></returns>
		public virtual string UniqueName()
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Sets the room type ID.
		/// </summary>
		/// <returns></returns>
		public virtual void SetRoomTypeId(uint typeId)
		{
			throw new NotImplementedException();
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
		/// A unique type name for this set.
		/// </summary>
		/// <returns></returns>
		public override string UniqueName()
		{
			if (RemoteServers == null)
			{
				return Service.InstanceType.Name + "_m";
			}
			else
			{
				return RemoteServers.InstanceType.Name;
			}
		}

		/// <summary>
		/// Sets the room type ID.
		/// </summary>
		/// <returns></returns>
		public override void SetRoomTypeId(uint typeId)
		{
			RoomTypeId = typeId;
		}

		/// <summary>
		/// Creates a network room set.
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="mapping"></param>
		/// <returns></returns>
		public static async ValueTask<NetworkRoomSet<T, ID, ROOM_ID>> CreateSet(AutoService<T, ID> svc, MappingService<ROOM_ID, uint> mapping)
		{
			var s = new NetworkRoomSet<T, ID, ROOM_ID>(svc, mapping);
			await s.SetupTypeId(new Context());
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
		private NetworkRoomSet(AutoService<T, ID> svc, MappingService<ROOM_ID, uint> mapping)
		{
			Service = svc;
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
		public async ValueTask SetupTypeId(Context context)
		{
			await Events.WebSocket.SetUniqueTypeId.Dispatch(context, this);
		}

		/// <summary>
		/// The globally unique room type ID.
		/// </summary>
		public uint RoomTypeId;

		private ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>> _rooms = new ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>>();

		/// <summary>
		/// All rooms in this set.
		/// </summary>
		public ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>> AllRooms => _rooms;

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