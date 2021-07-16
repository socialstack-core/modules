using System;
using System.Threading.Tasks;
using Api.ContentSync;
using Api.Contexts;
using Api.Database;
using Api.NetworkRooms;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Api.WebSockets;

namespace Api.NetworkRooms
{

	/// <summary>
	/// Base class of network rooms.
	/// </summary>
	public partial class NetworkRoom
	{

		/// <summary>
		/// True if this room is empty.
		/// </summary>
		public virtual bool IsEmpty
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// True if users joining/ leaving the room should be broadcast to other users in the room.
		/// </summary>
		public virtual bool ShouldBroadcastPresence
		{
			get
			{
				return true;
			}
		}

	}

	/// <summary>
	/// Stores information for a particular network room for this particular server only.
	/// You can also add e.g. room state by creating a parent class.
	/// </summary>
	public partial class NetworkRoom<T, ID> : NetworkRoom
		where T:Content<ID> 
		where ID : struct, IEquatable<ID>, IConvertible
	{

		/// <summary>
		/// The content that this room is for.
		/// </summary>
		public T For;

		/// <summary>
		/// The ID of the content this room is for. Can be default(ID), often a 0, if there isn't any.
		/// </summary>
		public ID Id
		{
			get {
				return For == null ? default(ID) : For.Id;
			}
		}

		/// <summary>
		/// Context to use when creating/ removing server mappings.
		/// </summary>
		private static Context serverContext;

		/// <summary>
		/// A local copy of this servers ID.
		/// </summary>
		private static uint serverId;

		/// <summary>
		/// Mapping from the source object -> cluster servers. The mapping is called NetworkRoomServers.
		/// </summary>
		private static MappingService<ID, uint> mappingService;

		/// <summary>
		/// First local user in this room.
		/// </summary>
		public UserInRoom<T, ID> First { get; set; }

		/// <summary>
		/// Last local user in this room.
		/// </summary>
		public UserInRoom<T, ID> Last { get; set; }

		/// <summary>
		/// True if this room is empty.
		/// </summary>
		public override bool IsEmpty
		{
			get {
				return First == null;
			}
		}

		/// <summary>
		/// A weak reference to the cached list of server IDs.
		/// </summary>
		private WeakReference<IndexLinkedList<Mapping<ID, uint>>> cacheServerList;

#warning todo: Servers that have some part of the network room. This is via a mapping from {the type} -> ClusteredServer called "NetworkRoomServers".
		// When one of these mapping services is done loading (and its cache is loaded up), a server MUST delete all entries from it that reference its own server ID.
		// Server can add its entry to that mapping automatically, whenever the first person is added to the room. 
		// Similarly when the last person leaves and the room is then empty, it should delete the server record.
		// NetworkRoom also has some config, e.g. should broadcast arrival/ leave messages. Can vary within a type, e.g. huddlecast huddles do not broadcast join/ depart.

		/// <summary>
		/// Gets a non-alloc enumeration tracker.
		/// </summary>
		/// <returns></returns>
		public NetworkRoomEnum<T, ID> GetNonAllocEnumerator()
		{
			return new NetworkRoomEnum<T,ID>()
			{
				Block = First
			};
		}

		/// <summary>
		/// Typically use client.GetInNetworkRoom(thisRoom) instead. True if the given client is in this room. Checks by iterating everything.
		/// </summary>
		public bool IsInRoomSlow(WebSocketClient client)
		{
			var iterator = GetNonAllocEnumerator();
			
			while(iterator.HasMore()){
				var current = iterator.Current();
				
				if(current == client)
				{
					return true;
				}
			}
			
			return false;
		}
		
		/// <summary>
		/// Add if the given client is not in the room.
		/// </summary>
		public async ValueTask<UserInRoom> Add(WebSocketClient client)
		{
			var roomEntry = client.GetInNetworkRoom(this);

			if (roomEntry != null)
			{
				return roomEntry;
			}
			
			return await AddUnchecked(client);
		}

		private IndexLinkedList<Mapping<ID, uint>> GetRemoteServers()
		{
			IndexLinkedList<Mapping<ID, uint>> serverList = null;

			if (cacheServerList == null)
			{
				serverList = mappingService.GetRawCacheList(Id);
				cacheServerList = new WeakReference<IndexLinkedList<Mapping<ID, uint>>>(serverList);
			}
			else
			{
				if (!cacheServerList.TryGetTarget(out serverList))
				{
					serverList = mappingService.GetRawCacheList(Id);

					if (serverList != null)
					{
						cacheServerList.SetTarget(serverList);
					}
				}
			}

			return serverList;
		}

		/// <summary>
		/// Local delivery only.
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public async ValueTask SendLocally(WebSocketClient sender = null)
		{
			// Loop through locals
			var current = First;

			while (current != null)
			{
				// Send to current.
				// current.Client.Send()

				current = current.Next;
			}
			
		}

		/// <summary>
		/// Sends a message to all users in this room except the given sender.
		/// </summary>
		/// <returns></returns>
		public async ValueTask Send(WebSocketClient sender = null)
		{
			await SendLocally(sender);
			
			// Loop through mapping
			if (mappingService == null)
			{
				serverId = Services.Get<ContentSyncService>().ServerId;
				serverContext = new Context(1, null, 1);
				mappingService = await MappingTypeEngine.GetOrGenerate(Services.GetByContentType(typeof(T)), Services.Get<ClusteredServerService>(), "NetworkRoomServers") as MappingService<ID, uint>;
			}

			var remotes = GetRemoteServers();

			if (remotes != null)
			{
				var currentRemoteServer = remotes.First;
				while (currentRemoteServer != null)
				{
					var mapping = currentRemoteServer.Current;

					if (mapping != null && mapping.TargetId != serverId)
					{
						
						// Send to this server by ID. Must also prepend type T and Id 
						// such that the other server can identify the target room.
					}
					currentRemoteServer = currentRemoteServer.Next;
				}
			}

		}

		/// <summary>
		/// Adds the given client to the set. Does not check if the client is already in the set.
		/// </summary>
		public async ValueTask<UserInRoom> AddUnchecked(WebSocketClient client)
		{
			if (mappingService == null)
			{
				serverId = Services.Get<ContentSyncService>().ServerId;
				serverContext = new Context(1, null, 1);
				mappingService = await MappingTypeEngine.GetOrGenerate(Services.GetByContentType(typeof(T)), Services.Get<ClusteredServerService>(), "NetworkRoomServers") as MappingService<ID, uint>;
			}

			var block = UserInRoom<T,ID>.GetPooled();
			block.Room = this;
			block.Client = client;
			
			// Add to rooms user chain:
			block.Next = null;

			bool wasEmpty = false;

			lock(this){
				if(First == null)
				{
					wasEmpty = true;
					First = block;
				}
				block.Previous = Last;
				Last = block;
			}

			if (wasEmpty)
			{
				// Add this server to the mapping:
				await mappingService.CreateIfNotExists(serverContext, Id, serverId);
			}

			// Add to clients room chain:
			block.NextForClient = null;
			
			if(client.FirstRoom == null)
			{
				client.FirstRoom = block;
			}
			
			block.PreviousForClient = client.LastRoom;
			client.LastRoom = block;
			
			return block;
		}

	}
	
	/// <summary>
	/// Network room enumeration cursor.
	/// </summary>
	public struct NetworkRoomEnum<T,ID>
		where T : Content<ID>
		where ID : struct, IEquatable<ID>, IConvertible
	{
		/// <summary>
		/// Current block.
		/// </summary>
		public UserInRoom<T,ID> Block;
		
		/// <summary>
		/// True if there's more.
		/// </summary>
		/// <returns></returns>
		public bool HasMore()
		{
			return Block != null;
		}
		
		/// <summary>
		/// Reads the current value and advances by one.
		/// </summary>
		/// <returns></returns>
		public WebSocketClient Current()
		{
			var c = Block;
			Block = Block.Next;
			return c.Client;
		}
	}

	/// <summary>
	/// A particular user in a particular networkRoom as seen by this server.
	/// </summary>
	public class UserInRoom
	{
		/// <summary>
		/// The client in this room
		/// </summary>
		public WebSocketClient Client;

		/// <summary>
		/// Previous in the chain of entires for a particular client.
		/// </summary>
		public UserInRoom PreviousForClient;

		/// <summary>
		/// Next in the chain of entires for a particular client.
		/// </summary>
		public UserInRoom NextForClient;

		/// <summary>
		/// The room the user is in (without its concrete type)
		/// </summary>
		public virtual NetworkRoom RoomBase
		{
			get {
				return null;
			}
		}

	}

	/// <summary>
	/// A particular user in a particular networkRoom as seen by this server.
	/// </summary>
	public class UserInRoom<T, ID>: UserInRoom
		where T : Content<ID>
		where ID : struct, IEquatable<ID>, IConvertible
	{
		private const int MaxPoolCount = 10000;
		
		private static int PoolCount;
		
		private static UserInRoom<T, ID> FirstPooled;
		
		private static object PoolLock = new object();
		
		/// <summary>
		/// Gets a pooled UserInRoom.
		/// </summary>
		public static UserInRoom<T, ID> GetPooled()
		{
			UserInRoom<T, ID> node = null;
			
			lock(PoolLock)
			{
				node = FirstPooled;
				if(node != null)
				{
					PoolCount--;
					FirstPooled = node.Next;
				}
			}
			
			if(node == null)
			{
				node = new UserInRoom<T, ID>();
			}
			
			return node;
		}

		/// <summary>
		/// The room the user is in (without its concrete type)
		/// </summary>
		public override NetworkRoom RoomBase
		{
			get
			{
				return Room;
			}
		}

		/// <summary>
		/// The room they're in.
		/// </summary>
		public NetworkRoom<T, ID> Room;
		
		/// <summary>
		/// Previous in the chain of user entries in the room.
		/// </summary>
		public UserInRoom<T, ID> Previous;

		/// <summary>
		/// Next in the chain of user entries in the room.
		/// </summary>
		public UserInRoom<T, ID> Next;

		/// <summary>
		/// Removes this client from the room. Returns this UserInRoom node to the pool.
		/// </summary>
		public void Remove()
		{
			// Remove from room chain.
			if(Next == null)
			{
				lock(Room)
				{
					Room.Last = Previous;
				}
			}
			else
			{
				Next.Previous = Previous;
			}
			
			if(Previous == null)
			{
				lock(Room)
				{
					Room.First = Next;
				}
			}
			else
			{
				Previous.Next = Next;
			}
			
			// Remove from client chain.
			if(NextForClient == null)
			{
				Client.LastRoom = PreviousForClient;
			}
			else
			{
				NextForClient.PreviousForClient = PreviousForClient;
			}
			
			if(PreviousForClient == null)
			{
				Client.FirstRoom = NextForClient;
			}
			else
			{
				PreviousForClient.NextForClient = NextForClient;
			}
			
			
			// Return to pool.
			lock(PoolLock)
			{
				if(PoolCount >= MaxPoolCount)
				{
					return;
				}
				
				PoolCount++;
				Next = FirstPooled;
				FirstPooled = this;
			}
		}
	}
	
}

namespace Api.WebSockets
{
	
	/// <summary>
	/// </summary>
	public partial class WebSocketClient
	{
		
		/// <summary>
		/// Linked list of rooms that this client is currently in.
		/// </summary>
		public UserInRoom FirstRoom;
		/// <summary>
		/// Linked list of rooms that this client is currently in.
		/// </summary>
		public UserInRoom LastRoom;


		/// <summary>
		/// Gets the userInRoom for this client in the given room.
		/// Null if this user is not in the given room.
		/// </summary>
		/// <param name="room"></param>
		/// <returns></returns>
		public UserInRoom GetInNetworkRoom(NetworkRoom room)
		{
			if (room.IsEmpty)
			{
				// The room is known to be empty. The user can't be in it.
				return null;
			}

			var current = FirstRoom;

			// Expects that users will be in very few rooms at once.
			while (current != null)
			{
				if (current.RoomBase == room)
				{
					return current;
				}

				current = current.NextForClient;
			}

			return null;
		}

	}
	
}