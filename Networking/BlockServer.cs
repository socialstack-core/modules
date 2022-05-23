using Api.SocketServerLibrary;
using Api.Startup;
using Api.NetworkNodes;
using Org.BouncyCastle.Crypto;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Lumity.BlockChains;


/// <summary>
/// Block server (base type)
/// </summary>
public class BlockServer : UdpDestination
{
	/// <summary>
	/// 
	/// </summary>
	public readonly object sendQueueLock = new object();

	/// <summary>
	/// 
	/// </summary>
	public BlockBuffer FirstSendFrame;
	/// <summary>
	/// 
	/// </summary>
	public BlockBuffer LastSendFrame;
	/// <summary>
	/// 
	/// </summary>
	public bool CanStartSend = true;

	/// <summary>
	/// 
	/// </summary>
	public readonly IPEndPoint _blankEndpoint = new IPEndPoint(IPAddress.Any, 0);
	
	/// <summary>
	/// The UDP socket
	/// </summary>
	public Socket ServerSocketUdp;

	/// <summary>
	/// True if this server is in raw mode and an IP header must be prepended to any outbound message.
	/// </summary>
	public bool IsRunningInRawMode = false;

	/// <summary>
	/// Starts a writer, considering if this server is in raw packet mode.
	/// </summary>
	/// <param name="senderPort"></param>
	/// <param name="addressBytes">Often is ipv6 mapped ipv4 addresses but is always 16 bytes long</param>
	/// <param name="ipv4">Declares if it actually is ipv4 or not</param>
	/// <returns></returns>
	public Writer StartMessage(ushort senderPort, ref Span<byte> addressBytes, bool ipv4)
	{
		var writer = MtuSizedPool.GetWriter();

		if (IsRunningInRawMode)
		{
			UdpHeader.StartHeader(
				writer,
				ipv4 ? PortAndIpV4 : PortAndIpV6,
				senderPort,
				ref addressBytes
			);
		}
		else
		{
			writer.Start(null);
		}

		return writer;
	}

	/// <summary>
	/// Starts a writer, considering if this server is in raw packet mode.
	/// </summary>
	/// <param name="forClient"></param>
	/// <returns></returns>
	public Writer StartMessage(BlockClient forClient)
	{
		var writer = MtuSizedPool.GetWriter();

		if (IsRunningInRawMode)
		{
			UdpHeader.StartHeader(writer, forClient.PortAndIp.Length == 6 ? PortAndIpV4 : PortAndIpV6, forClient.PortAndIp);
		}
		else
		{
			writer.Start(null);
		}

		return writer;
	}
	
	/// <summary>
	/// Pool of 1400 byte buffers.
	/// </summary>
	public static BinaryBufferPool<BlockBuffer> MtuSizedPool = new BinaryBufferPool<BlockBuffer>(1400, true);
	
	private BlockClient[] ConnectingClients = new BlockClient[500]; // This number represents the amount of users that can attempt to handshake simultaneously. Handshakes are generally rare, so this can be quite low.
	private int connectingOffset = 0;
	private object connectingClientLock = new object();
	private object addingClientLock = new object();
	
	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	private BlockClient FirstClient;
	
	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	private BlockClient LastClient;

	private NetworkNodeService _networkNodes;
	
	/// <summary>
	/// Adds a client to client set.
	/// </summary>
	/// <param name="client"></param>
	public void AddClientByAddress(BlockClient client)
	{
		if (client.AddedToLookup)
		{
			return;
		}

		client.AddedToLookup = true;
		client.NextClient = null;
		client.PreviousClient = null;

		lock (addingClientLock)
		{
			// Add to linked list:
			if (LastClient == null)
			{
				FirstClient = LastClient = client;
			}
			else
			{
				client.PreviousClient = LastClient;
				LastClient.NextClient = client;
			}
		}

	}
	
	/// <summary>
	/// Removes a client from the active set.
	/// </summary>
	/// <param name="client"></param>
	public void RemoveFromLookup(BlockClient client)
	{
		if (!client.AddedToLookup)
		{
			return;
		}

		client.AddedToLookup = false;

		lock (addingClientLock)
		{
			//Remove from set:
			if (client.NextClient == null)
			{
				LastClient = client.PreviousClient;
			}
			else
			{
				client.NextClient.PreviousClient = client.PreviousClient;
			}

			if (client.PreviousClient == null)
			{
				FirstClient = client.NextClient;
			}
			else
			{
				client.PreviousClient.NextClient = client.NextClient;
			}
		}

	}

	/// <summary>
	/// Creates an offer to use when connecting.
	/// </summary>
	/// <returns></returns>
	public BlockClient CreateClient()
	{
		var client = new BlockClient() {
			Server = this
		};

		int index;

		lock (connectingClientLock) {
			index = connectingOffset++;

			if (connectingOffset == 500)
			{
				connectingOffset = 0;
			}
		}

		client.TemporaryConnectId = index;
		ConnectingClients[index] = client;

		return client;
	}

	/// <summary>
	/// Gets a client by node ID. Note that if it is 0, this is a handshake request.
	/// </summary>
	/// <param name="nodeId"></param>
	/// <returns></returns>
	public BlockClient GetClient(ulong nodeId)
	{
		if (nodeId == 0)
		{
			return null;
		}

#warning todo
		return null;
	}

	/// <summary>
	/// Creates a new block server. All traffic goes to the same port, 272.
	/// </summary>
	public BlockServer()
	{
		Port = 272;
		
		try
		{
			ServerSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Udp);
			ServerSocketUdp.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
			IsRunningInRawMode = true;
		}
		catch(Exception)
		{
			System.Console.WriteLine("[Notice] A block service was started as non-admin and is running in headerless mode. This is fine for a single server/ development instance. For more information, find this comment in the source.");
			// For a production cluster deployment, header mode is highly recommended. This is to avoid frequent unnecessary allocation of IPAddress objects.
			// Most production deployments run with the necessary permissions anyway.
			// A fallback is provided and it will work fine but will just experience some GC pressure on busy chains.
			
			ServerSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			IsRunningInRawMode = false;
		}

		var maintenanceLoop = new System.Timers.Timer();
		maintenanceLoop.Elapsed += new System.Timers.ElapsedEventHandler(OnMaintenanceTick);
		maintenanceLoop.Interval = 1000;
		maintenanceLoop.Start();

		ServerSocketUdp.ExclusiveAddressUse = false;
		ServerSocketUdp.EnableBroadcast = false;

		ServerSocketUdp.Bind(
			new IPEndPoint(IPAddress.Any, Port)
		);

		// Start recv:
		Task.Run(async () => {
			
			while (true)
			{
				var receiveBytes = GetPooledReceiver();
				var recvd = await ServerSocketUdp.ReceiveFromAsync(receiveBytes.Memory, SocketFlags.None, _blankEndpoint);
				receiveBytes.Length = recvd.ReceivedBytes;

				/*
				// Add to inbound q:
				lock (inboundQueueLock)
				{
					if (_inboundEnd == null)
					{
						_inboundEnd = receiveBytes;
						_inboundStart = receiveBytes;
					}
					else
					{
						_inboundEnd.After = receiveBytes;
						_inboundEnd = receiveBytes;
					}
				}*/

				var rb = receiveBytes;
				OnReceiveRaw(rb);

				// Add the bytes to the pool:
				lock (inboundPoolLock)
				{
					rb.After = _inboundPool;
					_inboundPool = rb;
				}

			}
		});

		/*
		// Start processors:
		for (var i = 0; i < 1; i++)
		{
			var thread = new System.Threading.Thread(() => {

				while (true)
				{
					ReceiveBytes receiveBytes = null;

					lock (inboundQueueLock)
					{
						receiveBytes = _inboundStart;
						if (receiveBytes != null)
						{
							_inboundStart = _inboundStart.After;
						}
					}

					if (receiveBytes == null)
					{
						continue;
					}

					try
					{

						OnReceiveRaw(receiveBytes);
					}
					catch (Exception e)
					{
						Console.WriteLine("[Error] Packet receiver failed: " + e.ToString());
					}
				}

			});
			thread.Start();
		}
		*/
	}

	/// <summary>
	/// Gets a receiveBytes object which may originate from the pool.
	/// </summary>
	/// <returns></returns>
	public ReceiveBytes GetPooledReceiver()
	{
		ReceiveBytes result = null;

		lock (inboundPoolLock)
		{
			result = _inboundPool;
			if (result != null)
			{
				_inboundPool = _inboundPool.After;
			}
		}

		if (result == null)
		{
			// Using the same buffer size as the MTU pool here.
			byte[] buffer = GC.AllocateUninitializedArray<byte>(length: 1400, pinned: true);
			result = new ReceiveBytes(buffer);
		}

		return result;
	}

	private object inboundPoolLock = new object();
	private object inboundQueueLock = new object();
	private ReceiveBytes _inboundPool;
	private ReceiveBytes _inboundStart;
	private ReceiveBytes _inboundEnd;

	private int PacketsReceived;

	private void OnMaintenanceTick(object source, System.Timers.ElapsedEventArgs e)
	{
		// Called once per second.
		// This has the job of looking for "gone" clients as well as sending out presence and chain status reports to other clients.

		if (PacketsReceived != 0)
		{
			Console.WriteLine("Stats: " + PacketsReceived + ".");
			PacketsReceived = 0;
		}
	}

	/// <summary>
	/// Processes received UDP packets or schedules the processing of them.
	/// </summary>
	internal void RunReceiver()
	{
		
	}

	/// <summary>
	/// Called when receiving a UDP packet
	/// </summary>
	internal void OnReceiveRaw(ReceiveBytes poolBuffer)
	{
		var buffer = poolBuffer.Bytes;
		var size = poolBuffer.Length;

		if (size <= 0) {
			return;
		}

		// This packet has the IP/UDP header on it. All outbound messages MUST be given the header as well.
		Span<byte> ipBytes = stackalloc byte[16];
		var index = UdpHeader.PayloadStart(buffer, this,out ushort port, ref ipBytes, out bool isV4);

		if (index == -1)
		{
			// Invalid packet - dropping it. Happens mainly when it wasn't for us.
			return;
		}

		// The port and addressBytes is who we reply to, but the node ID
		// is the basis of how we establish if this message is authentic or not.

		// If node ID is 0, this is a handshake packet.
		#warning todo
	}

	/// <summary>
	/// Called when receiving a UDP packet
	/// </summary>
	internal void OnReceive(BufferedBytes poolBuffer, ushort port, ref Span<byte> addressBytes, bool isV4)
	{
		var buffer = poolBuffer.Bytes;
		var size = poolBuffer.Length;

		if (size <= 0)
		{
			return;
		}

		// The port and addressBytes is who we reply to, but the node ID
		// is the basis of how we establish if this message is authentic or not.

		// If node ID is 0, this is an initial handshake packet. The handshake is concluded using a mapping with the remote IP/port as the key.
		// Note that it is purely for the one off handshake itself; after that point, the node ID is used exclusively.

		// [1 byte flags]. Lowest bit (i.e. 1) indicates encrypted. 2nd lowest bit (i.e. 2) indicates a HMAC is present.
		// [4 byte sequence number, extended with the inferred overflow key]
		// [Compressed node ID]. 0 indicates first time handshake.
		// [Compressed fragment packet index starting from 0]. Usually a single byte.
		// [Compressed fragment packet count]. Usually a single byte.
		// [MAC, if mac is present]. 10 bytes long.
		// [Payload]

		// A non-fragmented message (which is relatively rare) simply has a frag count of 1 and frag index 0.
		// MAC/ encryption may be required depending on what network the message is used on.
		// Ipv4 CRC is required.

#warning todo

	}

}

/// <summary>
/// Used when receiving bytes from the network socket
/// </summary>
public class ReceiveBytes
{	
	/// <summary>The length of Bytes.</summary>
	public int Length;
	/// <summary>When this object is in the pool, this is the object after.</summary>
	public ReceiveBytes After;
	/// <summary>The bytes themselves.</summary>
	public readonly byte[] Bytes;
	/// <summary>
	/// Raw pinned memory ref.
	/// </summary>
	public readonly Memory<byte> Memory;

	/// <summary>
	/// Instances a new block of bytes.
	/// </summary>
	public ReceiveBytes(byte[] bytes)
	{
		Bytes = bytes;
		Memory = bytes.AsMemory<byte>();
	}
}

/// <summary>
/// Handshake metadata. Exists only whilst the handshake is occuring.
/// </summary>
public class BlockHandshakeMeta
{
	/// <summary>
	/// General RTP random generator.
	/// </summary>
	public static RandomNumberGenerator Rng = RandomNumberGenerator.Create();

	/// <summary>
	/// 16 bytes from client and 16 from server in that order.
	/// </summary>
	public byte[] RandomData = new byte[32];


	/// <summary>
	/// Creates new handshake meta.
	/// </summary>
	public BlockHandshakeMeta()
	{
		// Setup server random bytes:
		Rng.GetBytes(RandomData, 16, 16);
	}

}

/// <summary>
/// Block buffer
/// </summary>
public class BlockBuffer : BufferedBytes
{
	/// <summary>
	/// Address to send to
	/// </summary>
	public IPEndPoint Target;
}