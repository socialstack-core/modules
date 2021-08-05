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
	/// Set of network rooms for a given service.
	/// </summary>
	public partial class NetworkRoomSet<T, ID, ROOM_ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		where ROOM_ID : struct, IConvertible, IEquatable<ROOM_ID>, IComparable<ROOM_ID>
	{
		/// <summary>
		/// The service this set is for.
		/// </summary>
		public AutoService<T, ID> Service;

		/// <summary>
		/// The mapping of servers listening to particular IDs of things in this service.
		/// Can be null if this is itself a mapping, which are always cached.
		/// </summary>
		public MappingService<ROOM_ID, uint> RemoteServers;

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
		/// A room used for hosting "any" updates on this type. Not used by mapping network room sets.
		/// </summary>
		public NetworkRoom<T, ID, ROOM_ID> AnyUpdateRoom;

		/// <summary>
		/// Creates a network room set.
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="mapping"></param>
		/// <param name="contentSync"></param>
		public NetworkRoomSet(AutoService<T, ID> svc, MappingService<ROOM_ID, uint> mapping, ContentSyncService contentSync)
		{
			Service = svc;
			ContentSync = contentSync;
			RemoteServers = mapping;
			ServerContext = new Context(1, null, 1);
			SharedGuestContext = new Context(1, null, 3);

			AnyUpdateRoom = GetOrCreateRoom(default(ROOM_ID));
		}

		private ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>> _rooms = new ConcurrentDictionary<ROOM_ID, NetworkRoom<T, ID, ROOM_ID>>();

		/// <summary>
		/// Sends to the given room globally.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="writer"></param>
		public void Send(ROOM_ID id, Writer writer)
		{
			// It's better to just instance the room if it's empty locally.
			// That way we don't need to handle a global only send here, 
			// which would also be slower as it wouldn't be able to cache the server index for the room.
			var room = GetOrCreateRoom(id);
			room.Send(writer, null);
		}

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