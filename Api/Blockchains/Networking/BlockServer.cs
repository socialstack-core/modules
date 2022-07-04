using Api.SocketServerLibrary;
using Api.Startup;
using Api.NetworkNodes;
using Org.BouncyCastle.Crypto;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Org.BouncyCastle.Security;
using Api.Signatures;

namespace Lumity.BlockChains;


/// <summary>
/// Block server (base type)
/// </summary>
public class BlockServer : UdpDestination
{
	/// <summary>
	/// A shared block server for this host machine. Setup when at least one block project is running in realtime mode, or this is the assembly service node.
	/// </summary>
	public static BlockServer SharedBlockServer;

	/// <summary>
	/// True if the shared server, when instanced, should run in the LAN only mode.
	/// </summary>
	public static bool SharedBlockServerSafeLanMode;

	/// <summary>
	/// True if the shared server is multitenant - it is syncing more than one project.
	/// </summary>
	public static bool SharedBlockServerMultiTenant;

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
	/// True if this server is multi-tenant, meaning it is syncing multiple chain projects.
	/// </summary>
	public bool MultiTenant;

	/// <summary>
	/// True if this server is on a LAN exclusively and can receive cleartext transactions.
	/// Doesn't affect handshake Hello messages.
	/// </summary>
	public bool CleartextPermitted;

	/// <summary>
	/// Set this to true to skip the auth check. Like CleartextPermitted, this can be used on private LAN networks 
	/// where you can be confident that the other end is who it declares to be.
	/// Note that the actual handshake still occurs in full as it may still be hard to prove that the handshake itself is not being recorded.
	/// </summary>
	public bool SkipAuthCheck;

	/// <summary>
	/// Starts a writer, considering if this server is in raw packet mode.
	/// </summary>
	/// <returns></returns>
	public Writer StartMessage()
	{
		Writer writer;

		if (IsRunningInRawMode)
		{
			writer = MtuSizedPoolRawMode.GetWriter();
			writer.Start(null);
		}
		else
		{
			writer = MtuSizedPoolBasicMode.GetWriter();
			writer.Start(null);
		}

		return writer;
	}

	/// <summary>
	/// The max number of bytes in the Lumity packet header. Compressed numbers are represented as 9 bytes here.
	/// </summary>
	public const int MaxLumityPacketHeaderSize = 52;

	/// <summary>
	/// MTU of 1400 minus the 100 bytes which occur in a IPv6 packet header plus the 52 byte Lumity header.
	/// </summary>
	public const int MaxLumityPacketPayloadSize = 1300;

	/// <summary>
	/// Pool of 1400 byte buffers.
	/// When a buffer is requested from the pool, including by writers, it starts with an offset giving enough space for a packet header.
	/// </summary>
	public static BinaryBufferPool<BlockBuffer> MtuSizedPoolBasicMode = new BinaryBufferPool<BlockBuffer>(1400, true, MaxLumityPacketHeaderSize);

	/// <summary>
	/// Pool of 1400 byte buffers.
	/// When a buffer is requested from the pool, including by writers, it starts with an offset giving enough space for a packet header including the IPV4 or IPV6/UDP headers.
	/// IPv6 headers are largest, so it uses that.
	/// </summary>
	public static BinaryBufferPool<BlockBuffer> MtuSizedPoolRawMode = new BinaryBufferPool<BlockBuffer>(1400, true, UdpHeader.V6HeaderSize + MaxLumityPacketHeaderSize); // Conveniently exactly 100 and is also the max overall.


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
	/// The Sike P751 parameters (NIST level 5).
	/// </summary>
	public SikeIsogeny.SikeParamP751 SikeParam;
	/// <summary>
	/// The Sike key exchange engine.
	/// </summary>
	public SikeIsogeny.Sike Sike;
	/// <summary>
	/// Used to generate Sike keys.
	/// </summary>
	public SikeIsogeny.KeyGenerator SikeKeyGenerator;

	/// <summary>
	/// Creates a new block server. All traffic goes to the same port, 272.
	/// </summary>
	/// <param name="safeLocalNetworkMode">True if the nodes of this network are on the same LAN and can skip various auth checks and encryption.</param>
	public BlockServer(bool safeLocalNetworkMode)
	{
		Port = 272;

		// Setup quantum safe key exchange:
		SikeParam = new SikeIsogeny.SikeParamP751(SikeIsogeny.ImplementationType.OPTIMIZED);
		Sike = new SikeIsogeny.Sike(SikeParam);
		SikeKeyGenerator = new SikeIsogeny.KeyGenerator(SikeParam);

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

		if (safeLocalNetworkMode)
		{
			var set = new IpSet();
			IpDiscovery.DiscoverPrivateIps(set, false);

			// Cleartext is permitted and the auth check is skipped:
			CleartextPermitted = true;
			SkipAuthCheck = true;

			ServerSocketUdp.Bind(
				new IPEndPoint(set.PrivateIPv4, Port)
			);
		}
		else
		{
			ServerSocketUdp.Bind(
				new IPEndPoint(IPAddress.Any, Port)
			);
		}

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

		if (size < 7)
		{
			// Min message is 7 bytes long.
			return;
		}

		// The port and addressBytes is who we reply to, but the node ID
		// is the basis of how we establish if this message is authentic or not.

		// If node ID is 0, this is an initial handshake packet. The handshake is concluded using a mapping with the remote IP/port as the key.
		// Note that it is purely for the one off handshake itself; after that point, the node ID is used exclusively.

		// [1 byte flags]. Lowest bit (i.e. 1) indicates encrypted message. 2nd lowest bit (i.e. 2) indicates this is a multitenant request and includes projectId.
		// [Compressed project ID], if multitenant.
		// [Compressed node ID]. 0 indicates first time handshake.
		// [4 byte sequence number, extended with the inferred overflow key]
		// If encrypted, everything else is encrypted from here. The MAC is then generated from everything except itself.
		// [1 byte type]. 0=ClientHello, 1=ServerHello, 2=Transaction, 3=Guard, 4=GuardOk, 5=Ping.
		// [Compressed fragment packet index starting from 0]. Usually a single byte.
		// [Compressed fragment packet count]. Usually a single byte.
		// [Payload]
		// [MAC, if encrypted]. 10 bytes long.

		var startIndex = 0;
		var index = 0;

		// Read the flags:
		var flags = buffer[index++];

		ulong projectId = 0;

		// Get the project ID if multitenant:
		if ((flags & 2) == 2)
		{
			// Read the project ID. The nodeId relates to this project.
			// Note that the ID is the ID of the project in the Lumity Public Index.
			projectId = ReadCompressed(buffer, ref index);
		}
		else if (MultiTenant)
		{
			// Drop packet - this is an MT server and was sent a single project packet. We don't know which project it is for.
			return;
		}

		// Compressed node ID:
		var nodeId = ReadCompressed(buffer, ref index);

		// Get the client meta by that node ID:
		BlockClient client;
		byte messageType;
		
		if (nodeId == 0)
		{
			// This is only valid for unencrypted links; specifically the handshake.
			if ((flags & 1) == 1)
			{
				// Drop malformed or manipulated packet.
				return;
			}

			// If this is a ClientHello, CreateClient, otherwise get client based on temporary assigned ID.
			messageType = buffer[index++];

			if (messageType == 0)
			{
				// ClientHello. Create a client now:
				client = CreateClient();
			}
			else
			{
				// Read the temporary ID:
				var id = buffer[index++] << 8 | buffer[index++];

				if (id >= ConnectingClients.Length)
				{
					// Incorrect ID.
					return;
				}

				// Get the client meta:
				client = ConnectingClients[id];

				if (client == null)
				{
					// Incorrect ID.
					return;
				}
			}
		}
		else
		{
			client = GetClient(nodeId);

			// Sequence number:
			var sequenceNumber = (uint)(buffer[index++] << 24 | buffer[index++] << 16 | buffer[index++] << 8 | buffer[index++]);

			// Guess the extended packet index (48 bit), see rFC 3711, 3.3.1
			// Stores the guessed rollover code as guessedROC
			var packetIndex = client.GuessPacketIndex(sequenceNumber, out uint guessedROC);

			var lastRocAndS1 = client.LatestExtendedSequence;
			long indexDelta = (long)packetIndex - (long)lastRocAndS1;

			// Replay control
			if (client.WasReplayed(indexDelta))
			{
				// Drop this packet
				System.Console.WriteLine("Dropping replayed packet");
				return;
			}

			if ((flags & 1) == 1)
			{
				// Encrypted message which also ends with a HMAC. Check the HMAC now.
				if (!client.CheckPacketTag(buffer, 0, size, guessedROC))
				{
					// Invalid HMAC. Drop the packet.
					System.Console.WriteLine("Dropping packet with bad tag");
					return;
				}
			}

			// HMAC is valid! We can now proceed to potentially decrypt the message.
			// We'll update the ROC etc first though just in case another packet from this same client is about to be processed.
			client.UpdatePacketIndex(sequenceNumber, indexDelta, guessedROC);

			// We're now at least 6 bytes in to the message and have everything we need to decrypt this message if necessary.
			
			if ((flags & 1) == 1)
			{
				// It's an encrypted message. Decrypt it now.

				// Uses the very last 10 bytes.
				Span<byte> ivStore = stackalloc byte[16];

				ivStore[0] = client.ReceiveSaltKey[0];
				ivStore[1] = client.ReceiveSaltKey[1];
				ivStore[2] = client.ReceiveSaltKey[2];
				ivStore[3] = client.ReceiveSaltKey[3];

				for (var i = 4; i < 8; i++)
				{
					ivStore[i] = (byte)((0xFF & (nodeId >> ((7 - i) * 8))) ^ client.ReceiveSaltKey[i]);
				}

				for (var i = 8; i < 14; i++)
				{
					ivStore[i] = (byte)((0xFF & (byte)(packetIndex >> ((13 - i) * 8))) ^ client.ReceiveSaltKey[i]);
				}

				// The last 10 bytes are the auth tag (checked above).
				var payloadSize = size - 10 - (index - startIndex);
				client.ReceiveCipherCtr.Process(buffer, index, payloadSize, ivStore);

				// Message payload has been decrypted.

				messageType = buffer[index++];
			}
			else
			{
				// Unencrypted message. These can be manipulated thus are only usable on either a controlled LAN or for handshakes.
				messageType = buffer[index++];

				// Enforce the rules first:
				if (!CleartextPermitted)
				{
					// Handshake related?
					if (messageType > 1)
					{
						// Drop
						System.Console.WriteLine("Unencrypted packet dropped due to server policy.");
						return;
					}
				}
			}
		}

		var messageStart = index - 1;

		// A non-fragmented message (which is relatively rare) simply has a frag count of 1 and no frag index.
		var fragCount = ReadCompressed(buffer, ref index);

		if (fragCount == 1)
		{
			// Unfragmented message. Process immediately.
			poolBuffer.Length -= (messageStart - startIndex);
			poolBuffer.Offset = messageStart;
			client.ReceiveMessage(poolBuffer);
		}
		else
		{
			var fragIndex = ReadCompressed(buffer, ref index);
			// Add to fragment cache.
#warning todo handle fragmented messages
		}
	}

	/// <summary>
	/// Reads a compressed number from the given buffer.
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	private ulong ReadCompressed(byte[] buffer, ref int index)
	{
		var first = buffer[index++];

		switch (first)
		{
			case 251:
				// 2 bytes:
				return (ulong)(buffer[index++] | (buffer[index++] << 8));
			case 252:
				// 3 bytes:
				return (ulong)(buffer[index++] | (buffer[index++] << 8) | (buffer[index++] << 16));
			case 253:
				// 4 bytes:
				return (ulong)(buffer[index++] | (buffer[index++] << 8) | (buffer[index++] << 16) | (buffer[index++] << 24));
			case 254:
				// 8 bytes:
				return (ulong)buffer[index++] | ((ulong)buffer[index++] << 8) | ((ulong)buffer[index++] << 16) | ((ulong)buffer[index++] << 24) |
					((ulong)buffer[index++] << 32) | ((ulong)buffer[index++] << 40) | ((ulong)buffer[index++] << 48) | ((ulong)buffer[index++] << 56);
			default:
				return first;
		}
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
	public static readonly SecureRandom Rng = new SecureRandom();

	/// <summary>
	/// 16 bytes from client and 16 from server in that order.
	/// </summary>
	public byte[] RandomData = new byte[32];

	/// <summary>
	/// Classic ECDH key pair.
	/// </summary>
	public KeyPair ClassicKey;

	/// <summary>
	/// Sike key pair.
	/// </summary>
	public SikeIsogeny.SidhKeyPairOpti SikeKey;

	/// <summary>
	/// Creates new handshake meta.
	/// </summary>
	public BlockHandshakeMeta(int randomByteOffset)
	{
		// Setup client/ server random bytes:
		Rng.NextBytes(RandomData, randomByteOffset, 16);
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