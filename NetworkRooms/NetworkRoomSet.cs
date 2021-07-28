using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Api.Database;
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
		/// Creates a network room set.
		/// </summary>
		/// <param name="svc"></param>
		/// <param name="mapping"></param>
		public NetworkRoomSet(AutoService<T, ID> svc, MappingService<ID, uint> mapping)
		{
			Service = svc;
			RemoteServers = mapping;
		}

		private ConcurrentDictionary<ID, NetworkRoom<T, ID>> _rooms = new ConcurrentDictionary<ID, NetworkRoom<T, ID>>();

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