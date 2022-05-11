using Api.SocketServerLibrary;
using Api.Startup;
using Lumity.BlockChains;
using Api.NetworkNodes;
using Org.BouncyCastle.Crypto;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Api.WebRTC;


/// <summary>
/// WebRTC server (base type)
/// </summary>
public class WebRTCServer : UdpDestination
{
	/// <summary>
	/// 
	/// </summary>
	public readonly object sendQueueLock = new object();

	/// <summary>
	/// 
	/// </summary>
	public WebRTCBuffer FirstSendFrame;
	/// <summary>
	/// 
	/// </summary>
	public WebRTCBuffer LastSendFrame;
	/// <summary>
	/// 
	/// </summary>
	public bool CanStartSend = true;

	/// <summary>
	/// 
	/// </summary>
	public readonly IPEndPoint _blankEndpoint = new IPEndPoint(IPAddress.Any, 0);

	/// <summary>
	/// True if the DTLS exchange requires the EMS extension.
	/// This is pretty much standard in TLS these days as it adds much more cryptographic strength during the handshake.
	/// Note that turning this off will cause failures for clients that do require it.
	/// </summary>
	public bool ExtendedMasterSecret = true;

	/// <summary>
	/// The UDP socket
	/// </summary>
	public Socket ServerSocketUdp;

	/// <summary>
	/// True if this server is in raw mode and an IP header must be prepended to any outbound message.
	/// </summary>
	public bool IsRunningInRawMode = false;

	/// <summary>
	/// Called to indicate that the given generic RtpClient is ready to exchange data.
	/// This basically just forwards it to OnReady(SpecificClientType client).
	/// </summary>
	/// <param name="client"></param>
	/// <exception cref="NotImplementedException"></exception>
	public virtual void OnReadyBase(RtpClient client)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Removes a client to ClientLookupByIp
	/// </summary>
	/// <param name="client"></param>
	public virtual void RemoveFromLookup(RtpClient client)
	{
		throw new NotImplementedException();
	}

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
	public Writer StartMessage(RtpClient forClient)
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
	/// Pool of 2k byte buffers.
	/// </summary>
	public static BinaryBufferPool<WebRTCBuffer> MtuSizedPool = new BinaryBufferPool<WebRTCBuffer>(2048, true);

	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public static ulong CurrentNtpTimestamp()
	{
		return DateTimeToNtpTimestamp(DateTime.UtcNow);
	}

	/// <summary>
	/// Converts the given datetime to a lower resolution timestamp.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static uint DateTimeToNtpTimestamp32(DateTime value) { return (uint)((DateTimeToNtpTimestamp(value) >> 16) & 0xFFFFFFFF); }

	/// <summary>
	/// 
	/// </summary>
	/// <param name="ntp64"></param>
	/// <returns></returns>
	public static uint GetNtp32(ulong ntp64)
	{
		return (uint)(ntp64 >> 16);
	}

	/// <summary>
	/// Converts specified DateTime value to long NTP time.
	/// </summary>
	/// <param name="value">DateTime value to convert. This value must be in local time.</param>
	/// <returns>Returns NTP value.</returns>
	/// <notes>
	/// Wallclock time (absolute date and time) is represented using the
	/// timestamp format of the Network Time Protocol (NPT), which is in
	/// seconds relative to 0h UTC on 1 January 1900 [4].  The full
	/// resolution NPT timestamp is a 64-bit unsigned fixed-point number with
	/// the integer part in the first 32 bits and the fractional part in the
	/// last 32 bits. In some fields where a more compact representation is
	/// appropriate, only the middle 32 bits are used; that is, the low 16
	/// bits of the integer part and the high 16 bits of the fractional part.
	/// The high 16 bits of the integer part must be determined independently.
	/// </notes>
	public static ulong DateTimeToNtpTimestamp(DateTime value)
	{
		long now = value.Ticks;
		long baseDate = (value >= UtcEpoch2036 ? UtcEpoch2036 : UtcEpoch1900).Ticks;

		long ticks = now > baseDate ? now - baseDate : baseDate - now;

		return (ulong)(ticks / TimeSpan.TicksPerSecond << 32) | (ulong)(ticks % TimeSpan.TicksPerSecond);
	}

	private static DateTime UtcEpoch2036 = new DateTime(2036, 2, 7, 6, 28, 16, DateTimeKind.Utc);
	private static DateTime UtcEpoch1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

/// <summary>
/// WebRTC server for a specific client type. If in doubt use WebRTCServer[RtpClient]
/// </summary>
public class WebRTCServer<T> : WebRTCServer
	where T:RtpClient, new()
{
	private readonly byte[] ClientHello = new byte[] { 0, 0, 0, 0 };

	private IpLookupTable<T> ClientLookupByIp = new IpLookupTable<T>();
	private T[] ConnectingClients = new T[2048]; // This number represents the amount of users that can attempt to connect simultaneously.
	private int connectingOffset = 0;
	private object connectingClientLock = new object();
	private object addingClientLock = new object();
	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	private T FirstClient;
	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	private T LastClient;

	/// <summary>
	/// Latest tick time of this server. This is used for quickly applying a receive time to clients. 
	/// </summary>
	public long LatestTickTime;

	private NetworkNodeService _networkNodes;
	
	/// <summary>
	/// Called when the given client has completed a handshake and is in the ready state.
	/// </summary>
	/// <param name="client"></param>
	public virtual void OnReady(T client)
	{
		
	}

	/// <summary>
	/// Called to indicate that the given generic RtpClient is ready to exchange data.
	/// This basically just forwards it to OnReady(SpecificClientType client).
	/// </summary>
	/// <param name="client"></param>
	/// <exception cref="NotImplementedException"></exception>
	public override void OnReadyBase(RtpClient client)
	{
		OnReady((T)client);
	}

	/// <summary>
	/// Adds a client to ClientLookupByIp
	/// </summary>
	/// <param name="client"></param>
	public void AddClientByAddress(T client)
	{
		if (client.AddedToIpLookup)
		{
			return;
		}

		client.AddedToIpLookup = true;
		client.NextClient = null;
		client.PreviousClient = null;

		lock (addingClientLock)
		{
			// Add to root node:
			ClientLookupByIp.AddClient(client, -2);


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
	/// Removes a client to ClientLookupByIp
	/// </summary>
	/// <param name="client"></param>
	public override void RemoveFromLookup(RtpClient client)
	{
		if (!client.AddedToIpLookup)
		{
			return;
		}

		client.AddedToIpLookup = false;

		lock (addingClientLock)
		{
			// Remove from the lookup:
			ClientLookupByIp.RemoveClient((T)client, -2);

			if (client.NextClient == null)
			{
				LastClient = (T)client.PreviousClient;
			}
			else
			{
				client.NextClient.PreviousClient = client.PreviousClient;
			}

			if (client.PreviousClient == null)
			{
				FirstClient = (T)client.NextClient;
			}
			else
			{
				client.PreviousClient.NextClient = client.NextClient;
			}
		}

	}

	private string _serverIp;

	/// <summary>
	/// Gets an SDP offer for this server.
	/// </summary>
	/// <returns></returns>
	public SdpOffer GetOffer(out T client)
	{
		// Create a client:
		client = CreateClient();

		// Start setting up the certificate set:
		var certs = CertificateLookup.GetCertificateSet();
		client.HandshakeMeta.ServerCertificates = certs;

		var certFingerPrints = certs.SdpFingerprints;
		
		if(_serverIp == null)
		{
			if (_networkNodes == null)
			{
				_networkNodes = Services.Get<NetworkNodeService>();
			}

			if (_networkNodes.DiscoveredIps == null)
			{
				var set = new IpSet();
				IpDiscovery.DiscoverPrivateIps(set, false);

				_serverIp = set.PrivateIPv4.ToString();
			}
			else
			{
#if DEBUG
				_serverIp = _networkNodes.DiscoveredIps.PrivateIPv4.ToString();
#else
				_serverIp = _networkNodes.DiscoveredIps.PublicIPv4.ToString();
#endif
			}

			if (_candidate == null)
			{
				_candidate = _serverIp + " " + Port;
			}
		}

		var icePwd = client.SecretAsString();
		var iceUser = client.IceUsername();

		// PCMA/8000 opus/48000/2

		var sdp = @"v=0
o=huddle 0 1 IN IP4 " + _serverIp + @"
s= 
t=0 0
a=ice-lite
a=ice-options:renomination
a=ice-ufrag:" + iceUser + @"
a=ice-pwd:" + icePwd + @"
" + certFingerPrints;

		return new SdpOffer()
		{
			Header = sdp,
			Candidate = _candidate
		};
	}

	private string _candidate;

	/// <summary>
	/// Creates an offer to use when connecting via WebRTC.
	/// </summary>
	/// <returns></returns>
	public T CreateClient()
	{
		var client = new T() {
			Server = this,
			LastMessageAt = LatestTickTime
		};

		int index;

		lock (connectingClientLock) {
			index = connectingOffset++;

			if (connectingOffset == 2048)
			{
				connectingOffset = 0;
			}
		}

		client.TemporaryConnectId = index;
		ConnectingClients[index] = client;
		return client;
	}

	/// <summary>
	/// Gets an RtpClient by the username we gave out. It is "rtpuser" followed by the index in a ring buffer. This lookup is only permitted once.
	/// </summary>
	/// <param name="usernameBuffer"></param>
	/// <param name="usernameStart"></param>
	/// <param name="usernameLength"></param>
	/// <returns></returns>
	public T GetClient(byte[] usernameBuffer, int usernameStart, int usernameLength)
	{
		if (usernameLength < 8)
		{
			return null;
		}

		var numberLength = usernameLength - 7; // E.g. "rtpuser14" => length of the number is 2.
											   // Note though that the remote client appends :theirUsername as well, so we actually terminate at a colon.
		usernameStart += 7;

		int clientId = 0;

		for (var i = 0; i < numberLength; i++)
		{
			var currentNumber = usernameBuffer[usernameStart + i];

			if (currentNumber == ':')
			{
				break;
			}

			currentNumber -= 48;

			if (currentNumber < 0 || currentNumber > 9)
			{
				// Not a number
				return null;
			}

			clientId = (clientId * 10) + currentNumber;
		}

		// Next is the ID in range?
		if (clientId < 0 || clientId >= 2048)
		{
			return null;
		}

		return ConnectingClients[clientId];
	}


	/// <summary>
	/// Gets an RtpClient by sender ip/port.
	/// </summary>
	/// <param name="port"></param>
	/// <param name="addressBytes"></param>
	/// <returns></returns>
	public T GetClient(ushort port, ref Span<byte> addressBytes)
	{
		// Get node by sender port:
		var subNode = ClientLookupByIp.SubTables[port];

		var client = subNode.Client;

		if (client != null)
		{
			return client;
		}

		// Lookup in the nodes subtables:
		if (subNode.SubTables == null)
		{
			// Nobody we know of is on this port.
			return null;
		}

		ushort index;

		// ipv6 capable lookup table.
		// Potentially up to 8 layers deep.
		for (var i = 0; i < 16; i += 2)
		{
			index = (ushort)((addressBytes[i] << 8) | addressBytes[i+1]);
			subNode = subNode.SubTables[index];
			client = subNode.Client;

			if (client != null)
			{
				return client;
			}

			if (subNode.SubTables == null)
			{
				// Nobody we know of is on this IP segment.
				return null;
			}
		}

		return null;
	}

	/// <summary>
	/// Gets an RtpClient by sender ip/port.
	/// </summary>
	/// <param name="portAndIp"></param>
	/// <returns></returns>
	public T GetClient(byte[] portAndIp)
	{
		// Get node by sender port:
		var subNode = ClientLookupByIp.SubTables[(ushort)((portAndIp[0] << 8) | portAndIp[1])];

		var client = subNode.Client;

		if (client != null)
		{
			return client;
		}

		// Lookup in the nodes subtables:
		if (subNode.SubTables == null)
		{
			// Nobody we know of is on this port.
			return null;
		}

		ushort index;

		if (portAndIp.Length == 6)
		{
			// ipv4

			// Sub-node lookup 2:
			index = (ushort)((portAndIp[2] << 8) | portAndIp[3]);

			subNode = subNode.SubTables[index];
			client = subNode.Client;

			if (client != null)
			{
				return client;
			}

			if (subNode.SubTables == null)
			{
				// Nobody we know of is on this upper IP.
				return null;
			}

			// Lookup 3 (this is as deep as ipv4 can go):
			index = (ushort)((portAndIp[4] << 8) | portAndIp[5]);
			subNode = subNode.SubTables[index];
			return subNode.Client;
		}

		// ipv6

		// Potentially up to 8 layers deep.
		for (var i = 2; i < 18; i += 2)
		{
			index = (ushort)((portAndIp[i] << 8) | portAndIp[i+1]);
			subNode = subNode.SubTables[index];
			client = subNode.Client;

			if (client != null)
			{
				return client;
			}

			if (subNode.SubTables == null)
			{
				// Nobody we know of is on this IP segment.
				return null;
			}
		}

		return null;
	}

	/// <summary>
	/// Creates a new RTP server. All traffic goes to the same port.
	/// </summary>
	/// <param name="port"></param>
	public WebRTCServer(int port)
	{
		Port = port;
		LatestTickTime = DateTime.UtcNow.Ticks;

		try
		{
			ServerSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Udp);
			ServerSocketUdp.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
			IsRunningInRawMode = true;
		}
		catch(Exception)
		{
			System.Console.WriteLine("[Notice] A WebRTC service was started as non-admin and is running in headerless mode. This is fine for a single server/ development instance. For more information, find this comment in the source.");
			// For a production cluster deployment of Huddle, header mode is required (run as an administrator/ super user).
			// Most production deployments run with the necessary permissions anyway.
			// A fallback is provided and it will work fine if you are running a single instance - i.e. either for testing locally, or because you're only running 1 server.
			// This exists at all because our cluster effectively forwards raw UDP messages.
			// It ultimately could allow the outbound packets to come from any server in the cluster as well which greatly simplifies routing and scaling mechanisms,
			// however, this requires permission from the underlying host due to MANRS.

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
			// WebRTC packets are limited in size to ~1400 bytes, so 2048 is easily enough
			byte[] buffer = GC.AllocateUninitializedArray<byte>(length: 2048, pinned: true);
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
		// This has the job of looking for "gone" clients, as well as sending out the receiver/ sender reports.
		// Those reports are mandatory as they are a critical piece of congestion control. When they're not sent, remote clients assume the worst
		// and video quality drops significantly.
		if (PacketsReceived != 0)
		{
			Console.WriteLine("Stats: " + PacketsReceived + ". " + StunServer.stunOut + "/"+StunServer.stunIn);
			PacketsReceived = 0;
		}

		var now = DateTime.UtcNow;
		var nowTicks = now.Ticks;
		LatestTickTime = nowTicks;
		var ntpTime64 = DateTimeToNtpTimestamp(now);
		var ntpTime32 = GetNtp32(ntpTime64);

		RtpClient client = FirstClient;

		while (client != null)
		{
			var timeDiff = LatestTickTime - client.LastMessageAt; // In ticks

			if (timeDiff > 100000000) // 10 seconds, in ticks
			{
				// They've been gone for too long. Disconnect them:
				var next = client.NextClient;
				client.CloseRequested();
				client = next;
				continue;
			}

			client.OnUpdate(nowTicks);

			Rtp.SendReport(client, nowTicks, ntpTime64, ntpTime32);

			client = client.NextClient;
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

		PacketsReceived++;

		var b = buffer[index];

		// https://www.rfc-editor.org/rfc/rfc5764.html#section-5.1.2
		if (b < 2)
		{
			// STUN request
			T client = null;
			StunServer.HandleMessage(buffer, index, size - index, port, ref ipBytes, isV4, this, ref client);

			if (client != null)
			{
				client.LastMessageAt = LatestTickTime;
			}
		}
		else if (b > 19 && b < 64)
		{
			// DTLS packet. Client should always be identifiable at this point.
			var client = GetClient(port, ref ipBytes);

			if (client != null)
			{
				client.LastMessageAt = LatestTickTime;
				Dtls.HandleMessage(buffer, index, size - index, client);
			}
		}
		else if (b > 127 && b < 192)
		{
			var client = GetClient(port, ref ipBytes);

			if (client != null)
			{
				// RTP packet in.
				client.LastMessageAt = LatestTickTime;
				Rtp.HandleMessage(buffer, index, size - index, client);
			}
		}
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

		var b = buffer[0];
		
		// https://www.rfc-editor.org/rfc/rfc5764.html#section-5.1.2
		if (b < 2)
		{
			// STUN request
			T client = null;
			StunServer.HandleMessage(buffer, 0, size, port, ref addressBytes, isV4, this, ref client);

			if (client != null)
			{
				client.LastMessageAt = LatestTickTime;
			}
		}
		else if(b > 19 && b < 64)
		{
			// DTLS packet. Client should always be identifiable at this point.
			var client = GetClient(port, ref addressBytes);

			if (client != null)
			{
				client.LastMessageAt = LatestTickTime;
				Dtls.HandleMessage(buffer, 0, size, client);
			}
		}
		else if(b > 127 && b < 192)
		{
			var client = GetClient(port, ref addressBytes);

			if (client != null)
			{
				// RTP packet in.
				client.LastMessageAt = LatestTickTime;
				Rtp.HandleMessage(buffer, 0, size, client);
			}
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
/// Handshake metadata. Exists only whilst the DTLS handshake is occuring.
/// </summary>
public class RtpClientHandshake
{
	/// <summary>
	/// The set of certs on the server to use.
	/// </summary>
	public CertificateSet ServerCertificates;

	/// <summary>
	/// DTLS client_hello.random followed by server_hello.random and SessionID
	/// </summary>
	public byte[] DtlsRandom = new byte[96];

	/// <summary>
	/// DTLS private key for FS.
	/// </summary>
	public Signatures.KeyPair DtlsPrivate;

	/// <summary>
	/// The remote ends public key from their certificate.
	/// </summary>
	public AsymmetricKeyParameter RemotePublicCertKey;

	/// <summary>
	/// Digest used whilst verifying the DTLS handshake.
	/// </summary>
	public IDigest HandshakeVerifyDigest = new Org.BouncyCastle.Crypto.Digests.Sha256Digest();

	/// <summary>
	/// Handshake sequence number (sender side).
	/// </summary>
	public ushort SendHandshakeSequence;

	/// <summary>
	/// Handshake sequence number (recv side).
	/// </summary>
	public int ReceiveHandshakeSequence = -1;

	/// <summary>
	/// A buffer for DTLS handshake messages. Note that handshakes can be fragmented and can occur during ordinary data flow.
	/// </summary>
	public Writer HandshakeBuffer;

	/// <summary>
	/// Fills the server part of DtlsRandom with 64 random bytes.
	/// </summary>
	public void PopulateDtlsServerRandom()
	{
		RtpClient.Rng.GetBytes(DtlsRandom, 32, 64);
	}

	/// <summary>
	/// Gets the current session hash.
	/// </summary>
	/// <returns></returns>
	public byte[] GetSessionHash()
	{
		var hash = (IDigest)((Org.BouncyCastle.Utilities.IMemoable)HandshakeVerifyDigest).Copy();
		var sessionHash = new byte[hash.GetDigestSize()];
		hash.DoFinal(sessionHash, 0);
		return sessionHash;
	}

}

/// <summary>
/// WebRTC buffer
/// </summary>
public class WebRTCBuffer : BufferedBytes
{
	/// <summary>
	/// Address to send to
	/// </summary>
	public IPEndPoint Target;
}

/// <summary>
/// Stores state for a particular SSRC such as packets received, current sequence etc
/// </summary>
public struct SsrcState
{
	/// <summary>
	/// Tracks replay with a 64 packet history.
	/// </summary>
	public ulong ReplayWindow;

	/// <summary>
	/// Ticks time that the last sender report was received for this SSRC.
	/// </summary>
	public long SenderReportReceivedAt;

	/// <summary>
	/// The latest received sender reports own NTP time, >>16.
	/// </summary>
	public uint SenderReportTime;

	/// <summary>
	/// "s_1" - First observed sequence number.
	/// </summary>
	public ushort Sequence_1;

	/// <summary>
	/// Rollover code - essentially the number of times the sequence number has rolled over.
	/// </summary>
	public uint RolloverCode; // "roc", RFC 3711 3.3.1

	/// <summary>
	/// Tracks replay with a 64 packet history.
	/// </summary>
	public ulong RtcpReplayWindow;

	/// <summary>
	/// "s_1" - First observed sequence number.
	/// </summary>
	public uint RtcpSequence_1;

	/// <summary>
	/// Total number of received packets.
	/// </summary>
	public ulong PacketsReceived;

	/// <summary>
	/// Total number of received octets.
	/// </summary>
	public ulong OctetsReceived;

	/// <summary>
	/// Total number of sent packets.
	/// </summary>
	public ulong PacketsSent;

	/// <summary>
	/// Total number of sent octets.
	/// </summary>
	public ulong OctetsSent;

	/// <summary>
	/// The SSRC that this is the state for.
	/// </summary>
	public uint Ssrc;

	/// <summary>
	/// Latest sent RTP timestamp.
	/// </summary>
	public uint LatestRtpTimestamp;

	/// <summary>
	/// 'guesses' the extended index for a given sequence number.
	/// The guess is extremely accurate - it is only wrong if a packet is delayed by multiple minutes or has an incorrect sequence number.
	/// </summary>
	/// <param name="seqNo"></param>
	/// <param name="guessedROC"></param>
	/// <returns></returns>
	public ulong GuessIndex(ushort seqNo, out uint guessedROC)
	{
		if (Sequence_1 < 32768)
		{
			if (seqNo - Sequence_1 > 32768)
			{
				guessedROC = (uint)(RolloverCode - 1);
			}
			else
			{
				guessedROC = RolloverCode;
			}
		}
		else
		{
			if (Sequence_1 - 32768 > seqNo)
			{
				guessedROC = RolloverCode + 1;
			}
			else
			{
				guessedROC = RolloverCode;
			}
		}

		return ((ulong)guessedROC << 16) | seqNo;
	}

	/// <summary>
	/// 
	/// </summary>
	public uint ExtendedSequence => (uint)((RolloverCode << 16) | Sequence_1);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="index"></param>
	/// <param name="delta"></param>
	public void UpdateRtcp(uint index, long delta)
	{
		// Note that delta is known to be in a safe range.

		/* update the replay bit mask */
		if (delta > 0)
		{
			RtcpReplayWindow = RtcpReplayWindow << (int)delta;
			RtcpReplayWindow |= 1;
		}
		else
		{
			RtcpReplayWindow |= (ulong)1 << (int)delta;
		}

		RtcpSequence_1 = index;
	}

	/// <summary>
	/// Updates the packet index information. Occurs after GuessIndex.
	/// </summary>
	/// <param name="seqNo"></param>
	/// <param name="delta"></param>
	/// <param name="guessedROC"></param>
	public void UpdatePacketIndex(ushort seqNo, long delta, uint guessedROC)
	{
		/* update the replay bit mask */
		if (delta > 0)
		{
			ReplayWindow = ReplayWindow << (int)delta;
			ReplayWindow |= 1;
		}
		else
		{
			ReplayWindow |= ((ulong)1 << (int)delta);
		}

		if (seqNo > Sequence_1)
		{
			Sequence_1 = seqNo;
		}

		if (guessedROC > RolloverCode)
		{
			RolloverCode = guessedROC;
			Sequence_1 = seqNo;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="delta"></param>
	/// <returns></returns>
	public bool WasRtcpReplayed(long delta)
	{
		if (delta > 0)
		{
			/* Packet not yet received */
			return false;
		}

		if (-delta > 64)
		{
			/* Packet too old */
			return true;
		}

		if (((RtcpReplayWindow >> ((int)-delta)) & 0x1) != 0)
		{
			/* Packet already received ! */
			return true;
		}
		else
		{
			/* Packet not yet received */
			return false;
		}
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="delta"></param>
	/// <returns></returns>
	public bool WasReplayed(long delta)
	{
		if (delta > 0)
		{
			/* Packet not yet received */
			return false;
		}

		if (-delta > 64)
		{
			/* Packet too old */
			return true;
		}

		if (((ReplayWindow >> ((int)-delta)) & 0x1) != 0)
		{
			/* Packet already received ! */
			return true;
		}
		else
		{
			/* Packet not yet received */
			return false;
		}
	}

}