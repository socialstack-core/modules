using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Presence;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Api.WebSockets;

namespace Api.ContentSync
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
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{
		/// <summary>
		/// The ID of the content this room is for. Can be default(ID), often a 0, if there isn't any.
		/// </summary>
		public ID Id;

		/// <summary>
		/// The set that this room is in. Always exists.
		/// </summary>
		public NetworkRoomSet<T, ID> ParentSet;

		/// <summary>
		/// Mapping from the source object -> cluster servers. The mapping is called NetworkRoomServers.
		/// </summary>
		private MappingService<ID, uint> MappingService
		{
			get {
				return ParentSet.RemoteServers;
			}
		}
		
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
			IndexLinkedList<Mapping<ID, uint>> serverList;

			if (cacheServerList == null)
			{
				serverList = MappingService.GetRawCacheList(Id);
				cacheServerList = new WeakReference<IndexLinkedList<Mapping<ID, uint>>>(serverList);
			}
			else
			{
				if (!cacheServerList.TryGetTarget(out serverList))
				{
					serverList = MappingService.GetRawCacheList(Id);

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
		/// <param name="writer"></param>
		/// <param name="sender"></param>
		/// <returns></returns>
		public void SendLocally(Writer writer, WebSocketClient sender = null)
		{
			// Loop through locals
			var current = First;

			while (current != null)
			{
				// Send to current.
				var next = current.Next;

				if (current.Client == sender)
				{
					// Skip the sender.
				}
				else if (!current.Client.Send(writer))
				{
					// client is invalid - remove it from this room.
					current.Remove();
				}

				current = next;
			}
			
		}

		/// <summary>
		/// Sends a message to all users in this room except the given sender.
		/// </summary>
		/// <returns></returns>
		public void Send(Writer message, WebSocketClient sender = null)
		{
			SendLocally(message, sender);
			
			var remotes = GetRemoteServers();

			if (remotes != null)
			{
				var currentRemoteServer = remotes.First;
				while (currentRemoteServer != null)
				{
					var mapping = currentRemoteServer.Current;

					if (mapping != null)
					{
						// Send to this server by ID. Must also prepend something that allows the other server to identify the target room.
						var server = ParentSet.ContentSync.GetServer(mapping.TargetId);

						if (server != null)
						{
							server.Send(message);
						}
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
				await MappingService.CreateIfNotExists(ParentSet.ServerContext, Id, ParentSet.ContentSync.ServerId);
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
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
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
		/// A specified ID. Often 0.
		/// </summary>
		public uint CustomId;

		/// <summary>
		/// The room the user is in (without its concrete type)
		/// </summary>
		public virtual NetworkRoom RoomBase
		{
			get {
				return null;
			}
		}

		/// <summary>
		/// Removes this userInRoom, returning it to its pool.
		/// </summary>
		public virtual void Remove()
		{
			
		}

	}

	/// <summary>
	/// A particular user in a particular networkRoom as seen by this server.
	/// </summary>
	public class UserInRoom<T, ID>: UserInRoom
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
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
		public override void Remove()
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