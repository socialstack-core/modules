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
	public partial class NetworkRoomSet<T, ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// The service this set is for.
		/// </summary>
		public AutoService<T, ID> Service;

		/// <summary>
		/// The mapping of servers listening to particular IDs of things in this service.
		/// </summary>
		public MappingService<ID, uint> RemoteServers;

		/// <summary>
		/// The content sync service.
		/// </summary>
		public ContentSyncService ContentSync;

		/// <summary>
		/// An admin context to use when creating mappings.
		/// </summary>
		public Context ServerContext;

		/// <summary>
		/// Creates a network room set.
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="mapping"></param>
		/// <param name="contentSync"></param>
		public NetworkRoomSet(AutoService<T, ID> svc, MappingService<ID, uint> mapping, ContentSyncService contentSync)
		{
			Service = svc;
			ContentSync = contentSync;
			RemoteServers = mapping;
			ServerContext = new Context(1, null, 1);
		}

		private ConcurrentDictionary<ID, NetworkRoom<T, ID>> _rooms = new ConcurrentDictionary<ID, NetworkRoom<T, ID>>();

		/// <summary>
		/// Sends to the given room globally.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="writer"></param>
		public void Send(ID id, Writer writer)
		{
			// It's better to just instance the room if it's empty locally.
			// That way we don't need to handle a global only send here, 
			// which would also be slower as it wouldn't be able to cache the server index for the room.
			var room = GetOrCreateRoom(id);
			room.Send(writer, null);
		}

		/// <summary>
		/// Sends the given writer to every relevant room for the given source (local delivery only).
		/// </summary>
		/// <param name="src"></param>
		/// <param name="writer"></param>
		public void SendLocally(T src, Writer writer)
		{
			var room = GetRoom(src);

			if (room != null)
			{
				room.SendLocally(writer);
			}
		}

		/// <summary>
		/// Gets the room for the object with the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public NetworkRoom<T, ID> GetRoom(ID id)
		{
			_rooms.TryGetValue(id, out NetworkRoom<T, ID> result);
			return result;
		}

		/// <summary>
		/// Gets the room for the object with the given ID.
		/// </summary>
		/// <param name="forObject"></param>
		/// <returns></returns>
		public NetworkRoom<T, ID> GetRoom(T forObject)
		{
			_rooms.TryGetValue(forObject.Id, out NetworkRoom<T, ID> result);
			return result;
		}

		/// <summary>
		/// Gets the room for the object with the given ID.
		/// </summary>
		/// <param name="forObject"></param>
		/// <returns></returns>
		public NetworkRoom<T, ID> GetOrCreateRoom(T forObject)
		{
			return GetOrCreateRoom(forObject.Id);
		}

		/// <summary>
		/// Gets the room for the object with the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public NetworkRoom<T, ID> GetOrCreateRoom(ID id)
		{
			if (!_rooms.TryGetValue(id, out NetworkRoom<T, ID> result))
			{
				result = new NetworkRoom<T, ID>();
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