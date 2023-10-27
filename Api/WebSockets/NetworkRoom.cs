using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Newtonsoft.Json.Linq;

namespace Api.WebSockets
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
		/// Add if the given client is not in the room.
		/// </summary>
		public virtual ValueTask<UserInRoom> Add(WebSocketClient client, uint customId, FilterBase permFilter = null, JObject filter = null)
		{
			return new ValueTask<UserInRoom>();
		}

		/// <summary>
		/// True if this room is empty locally
		/// </summary>
		public virtual bool IsEmptyLocally
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

		/// <summary>
		/// Empties the network room.
		/// </summary>
		public virtual void EmptyLocally()
		{ }
	}

	/// <summary>
	/// Stores information for a particular network room for this particular server only.
	/// You can also add e.g. room state by creating a parent class.
	/// </summary>
	public partial class NetworkRoom<T, ID, ROOM_ID> : NetworkRoom
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		where ROOM_ID : struct, IConvertible, IEquatable<ROOM_ID>, IComparable<ROOM_ID>
	{
		/// <summary>
		/// The ID of the content this room is for. Can be default(ID), often a 0, if there isn't any.
		/// </summary>
		public ROOM_ID Id;

		/// <summary>
		/// The set that this room is in. Always exists.
		/// </summary>
		public NetworkRoomSet<T, ID, ROOM_ID> ParentSet;

		/// <summary>
		/// Mapping from the source object -> cluster servers. The mapping is called NetworkRoomServers.
		/// </summary>
		private MappingService<ROOM_ID, uint> MappingService
		{
			get {
				return ParentSet.RemoteServers;
			}
		}

		/// <summary>
		/// Empties the network room forcefully.
		/// </summary>
		public override void EmptyLocally()
		{
			var current = First;
			while (current != null)
			{
				var next = current.Next;
				current.Remove();
				current = next;
			}

			First = null;
			Last = null;
		}

		/// <summary>
		/// Local user count.
		/// </summary>
		public int LocalCount
		{
			get {
				var result = 0;
				var iterator = First;

				while (iterator != null)
				{
					iterator = iterator.Next;
					result++;
				}

				return result;
			}
		}

		/// <summary>
		/// First local user in this room.
		/// </summary>
		public UserInRoom<T, ID, ROOM_ID> First { get; set; }

		/// <summary>
		/// Last local user in this room.
		/// </summary>
		public UserInRoom<T, ID, ROOM_ID> Last { get; set; }

		/// <summary>
		/// True if this room is empty.
		/// </summary>
		public override bool IsEmpty
		{
			get {
				if (First != null)
				{
					return false;
				}

				var remotes = GetRemoteServers();
				return remotes == null || remotes.First == null;
			}
		}

		/// <summary>
		/// Don't call this - it's used specifically by the last leaving client to mark this room as being empty.
		/// </summary>
		public void MarkEmpty()
		{
			var ms = MappingService;
			if (ms != null)
			{
				_ = ms.DeleteByIds(ParentSet.ServerContext, Id, ParentSet.NodeId);
			}
		}

		/// <summary>
		/// True if this room is empty locally.
		/// </summary>
		public override bool IsEmptyLocally
		{
			get {
				return First == null;
			}
		}

		/// <summary>
		/// A weak reference to the cached list of server IDs.
		/// </summary>
		private WeakReference<IndexLinkedList<Mapping<ROOM_ID, uint>>> cacheServerList;

		/// <summary>
		/// Gets a non-alloc enumeration tracker.
		/// </summary>
		/// <returns></returns>
		public NetworkRoomEnum<T, ID, ROOM_ID> GetNonAllocEnumerator()
		{
			return new NetworkRoomEnum<T,ID, ROOM_ID>()
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
		public override async ValueTask<UserInRoom> Add(WebSocketClient client, uint customId, FilterBase permFilter = null, JObject filter = null)
		{
			var roomEntry = client.GetInNetworkRoom(this);

			if (roomEntry != null)
			{
				return roomEntry;
			}
			
			return await AddUnchecked(client, customId, permFilter, filter);
		}

		/// <summary>
		/// Gets the list of remote servers to to send to.
		/// </summary>
		/// <returns></returns>
		public IndexLinkedList<Mapping<ROOM_ID, uint>> GetRemoteServers()
		{
			var ms = MappingService;

			if (ms == null)
			{
				return null;
			}

			IndexLinkedList<Mapping<ROOM_ID, uint>> serverList;

			if (cacheServerList == null)
			{
				serverList = ms.GetRawCacheList(Id);
				cacheServerList = new WeakReference<IndexLinkedList<Mapping<ROOM_ID, uint>>>(serverList);
			}
			else
			{
				if (!cacheServerList.TryGetTarget(out serverList))
				{
					serverList = ms.GetRawCacheList(Id);

					if (serverList != null)
					{
						cacheServerList.SetTarget(serverList);
					}
				}
			}

			return serverList;
		}

		/// <summary>
		/// Sends the given writer to every relevant room for the given source (local delivery only).
		/// </summary>
		/// <param name="src"></param>
		/// <param name="opcode"></param>
		/// <param name="sender"></param>
		public async ValueTask SendLocallyIfPermitted(T src, byte opcode, WebSocketClient sender = null)
		{
			if (First == null)
			{
				return;
			}

			// Loop through locals
			var current = First;

			while (current != null)
			{
				// Send to current.
				var next = current.Next;

				if (current.Client == sender)
				{
					// Skip the sender.
					current = next;
					continue;
				}

				var ctx = current.Client.Context;

				var isIncluded = false;

				if (current.UserFilter != null)
				{
					isIncluded = current.UserFilter.IsIncluded;

					if (!current.UserFilter.Match(ctx, src, isIncluded))
					{
						// Skip this user - they can't receive the message.
						current = next;
						continue;
					}
				}

				// Perm check:
				if (current.LoadPermission != null && !current.LoadPermission.Match(ctx, src, isIncluded))
				{
					// Skip this user - they can't receive the message.
					current = next;
					continue;
				}

				// Build the JSON:
				var writer = Writer.GetPooled();
				writer.Start(null);
				writer.Write(opcode);
				writer.Write((uint)0); // Temporary size

				if (ParentSet.Service.IsMapping)
				{
					await ParentSet.Service.ToJson(ctx, src, writer);
				}
				else
				{
					await ParentSet.Service.ToJson(ctx, src, writer, null, "*");
				}

				// msg length:
				var firstBuffer = writer.FirstBuffer.Bytes;

				// Write the length of the JSON to the 4 bytes at the start:
				var msgLength = (uint)(writer.Length - 5);
				firstBuffer[1] = (byte)msgLength;
				firstBuffer[2] = (byte)(msgLength >> 8);
				firstBuffer[3] = (byte)(msgLength >> 16);
				firstBuffer[4] = (byte)(msgLength >> 24);

				if (!current.Client.Send(writer))
				{
					// client is invalid - remove it from this room.
					current.Remove();
				}

				writer.Release();

				current = next;
			}

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

		private static byte[] basicHeader = new byte[5]; // Opcode followed by 4 byte size.

		/// <summary>
		/// Starts a writer setup for this particular room. Use .Send(writer) when you've written your payload to it.
		/// </summary>
		/// <returns></returns>
		public Writer StartSend()
		{
			var writer = Writer.GetPooled();
			writer.Start(basicHeader);
			writer.FirstBuffer.Bytes[0] = 8; // Netroom opcode is 8
			writer.WriteCompressed(ParentSet.RoomTypeId);
			writer.WriteCompressed(ParentSet.IdConverter.Reverse(Id));
			return writer;
		}

		/// <summary>
		/// Adds the given client to the set. Does not check if the client is already in the set.
		/// </summary>
		public async ValueTask<UserInRoom> AddUnchecked(WebSocketClient client, uint customId, FilterBase permFilter, JObject userFilter = null)
		{
			var block = UserInRoom<T,ID, ROOM_ID>.GetPooled();
			block.Room = this;
			block.Client = client;
			block.LoadPermission = permFilter;

			if (userFilter == null)
			{
				block.UserFilter = null;
			}
			else
			{
				// This user filter will be tested before sending.
				// It typically exists to define the inclusion state.
				block.UserFilter = ParentSet.Service.LoadFilter(userFilter) as Filter<T, ID>;

				// Next, rent any necessary collectors, and execute the collections. RentAndCollect internally performs Setup as well.
				// The first time this happens on a given filter type may also cause the mapping services to load, thus it is awaitable.
				block.UserFilter.FirstCollector = await block.UserFilter.RentAndCollect(client.Context, ParentSet.Service);
			}

			// Ensure perm filter is setup also:
			if (permFilter != null && permFilter.RequiresSetup)
			{
				await permFilter.Setup();
			}

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

			if (block.Previous != null)
			{
				block.Previous.Next = block;
			}

			if (wasEmpty)
			{
				// Add this server to the mapping:
				var ms = MappingService;
				if (ms != null)
				{
					await ms.CreateIfNotExists(ParentSet.ServerContext, Id, ParentSet.NodeId);
				}
			}

			// Add to clients room chain:
			block.NextForClient = null;

			if (client.FirstRoom == null)
			{
				client.FirstRoom = block;
				block.PreviousForClient = null;
			}
			else
			{
				block.PreviousForClient = client.LastRoom;
			}

			if (client.LastRoom != null)
			{
				client.LastRoom.NextForClient = block;
			}
			
			client.LastRoom = block;
			block.CustomId = customId;
			return block;
		}

	}
	
	/// <summary>
	/// Network room enumeration cursor.
	/// </summary>
	public struct NetworkRoomEnum<T,ID, ROOM_ID>
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		where ROOM_ID : struct, IConvertible, IEquatable<ROOM_ID>, IComparable<ROOM_ID>
	{
		/// <summary>
		/// Current block.
		/// </summary>
		public UserInRoom<T,ID, ROOM_ID> Block;
		
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
	public class UserInRoom<T, ID, ROOM_ID> : UserInRoom
		where T : Content<ID>, new()
		where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		where ROOM_ID : struct, IConvertible, IEquatable<ROOM_ID>, IComparable<ROOM_ID>
	{
		private const int MaxPoolCount = 10000;
		
		private static int PoolCount;
		
		private static UserInRoom<T, ID, ROOM_ID> FirstPooled;
		
		private static object PoolLock = new object();

		/// <summary>
		/// The filter to use when testing if this user can load the object or not.
		/// If it's null but they're in the room they can receive any message.
		/// </summary>
		public FilterBase LoadPermission;

		/// <summary>
		/// User filter. Typically for On() based filters as it can specify if it's included or not.
		/// </summary>
		public Filter<T, ID> UserFilter;

		/// <summary>
		/// Gets a pooled UserInRoom.
		/// </summary>
		public static UserInRoom<T, ID, ROOM_ID> GetPooled()
		{
			UserInRoom<T, ID, ROOM_ID> node = null;
			
			lock(PoolLock)
			{
				node = FirstPooled;
				if(node != null)
				{
					PoolCount--;
					FirstPooled = node.Next;
				}
			}

			if (node == null)
			{
				node = new UserInRoom<T, ID, ROOM_ID>();
			}
			else
			{
				node.Previous = null;
				node.Next = null;
				node.Room = null;
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
		public NetworkRoom<T, ID, ROOM_ID> Room;
		
		/// <summary>
		/// Previous in the chain of user entries in the room.
		/// </summary>
		public UserInRoom<T, ID, ROOM_ID> Previous;

		/// <summary>
		/// Next in the chain of user entries in the room.
		/// </summary>
		public UserInRoom<T, ID, ROOM_ID> Next;

		/// <summary>
		/// Removes this client from the room. Returns this UserInRoom node to the pool.
		/// </summary>
		public override void Remove()
		{
			// Remove from room chain.
			
			if (Next == null)
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

			if (Room.IsEmptyLocally)
			{
				// This room is now empty. Make sure other servers are aware of this by removing the NR record.
				Room.MarkEmpty();
			}

			// Return collectors:
			if (UserFilter != null)
			{
				if (UserFilter.FirstCollector != null)
				{
					var col = UserFilter.FirstCollector;
					while (col != null)
					{
						var next = col.NextCollector;
						col.Release();
						col = next;
					}
				}
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