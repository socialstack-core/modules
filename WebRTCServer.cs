using Api.SocketServerLibrary;
using Org.BouncyCastle.Crypto;
using System;
using System.Net;
using System.Net.Sockets;

namespace Api.WebRTC;


/// <summary>
/// WebRTC server (base type)
/// </summary>
public class WebRTCServer
{
	/// <summary>
	/// The port number this server is on
	/// </summary>
	public int Port;

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
	/// Established when the first raw IpV4 UDP packet is received here.
	/// </summary>
	public byte[] PortAndIpV4;

	/// <summary>
	/// Established when the first raw IpV6 UDP packet is received here.
	/// </summary>
	public byte[] PortAndIpV6;

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
	public static BinaryBufferPool MtuSizedPool = new BinaryBufferPool(2048);

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

		#warning serverIp todo - grab from contentSyncs server info
		var serverIp = "192.168.1.150";
		var icePwd = client.SecretAsString();
		var iceUser = client.IceUsername();
		var port = Port;

		// * bundle, dtls, rtcp-mux

		// PCMA/8000 opus/48000/2

		/*var sdp = @"v=0
o=huddle " + connectionId + " 1 IN IP4 " + serverIp + @"
s= 
t=0 0
a=ice-lite
a=ice-options:renomination
a=ice-ufrag:" + iceUser + @"
a=ice-pwd:" + icePwd + @"
" + certFingerPrints + @"
a=group:BUNDLE 0
m=audio 7 UDP/TLS/RTP/SAVPF 111
c=IN IP4 " + serverIp + @"
a=rtpmap:111 opus/48000/2
a=fmtp:111 stereo=1;usedtx=1
a=rtcp-fb:111 transport-cc 
a=extmap:1 urn:ietf:params:rtp-hdrext:ssrc-audio-level
a=extmap:2/recvonly urn:ietf:params:rtp-hdrext:csrc-audio-level
a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid
a=setup:passive
a=mid:0
a=recvonly
a=candidate:udpcandidate 1 udp 1111 " + serverIp + " " + port + @" typ host
a=end-of-candidates
a=rtcp-mux
a=rtcp-rsize
";*/

		var sdp = @"v=0
o=huddle 0 1 IN IP4 " + serverIp + @"
s= 
t=0 0
a=ice-lite
a=ice-options:renomination
a=ice-ufrag:" + iceUser + @"
a=ice-pwd:" + icePwd + @"
" + certFingerPrints + @"
a=group:BUNDLE 0 1
m=audio 7 UDP/TLS/RTP/SAVPF 111
c=IN IP4 " + serverIp + @"
a=rtpmap:111 opus/48000/2
a=fmtp:111 stereo=1;usedtx=1
a=extmap:1 urn:ietf:params:rtp-hdrext:ssrc-audio-level
a=extmap:2/recvonly urn:ietf:params:rtp-hdrext:csrc-audio-level
a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid
a=setup:passive
a=mid:0
a=sendrecv
a=candidate:udpcandidate 1 udp 1111 " + serverIp + " " + port + @" typ host
a=end-of-candidates
a=rtcp-mux
a=rtcp-rsize
m=video 7 UDP/TLS/RTP/SAVPF 96
c=IN IP4 " + serverIp + @"
a=rtpmap:96 H264/90000
a=rtcp-fb:96 goog-remb
a=fmtp:96 profile-level-id=42e01f;level-asymmetry-allowed=1;x-google-max-bitrate=6000;x-google-min-bitrate=3500;x-google-start-bitrate=3500
a=extmap:3 urn:ietf:params:rtp-hdrext:sdes:mid
a=extmap:4 http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time
a=extmap:5 urn:ietf:params:rtp-hdrext:toffset
a=extmap:6/recvonly http://www.webrtc.org/experiments/rtp-hdrext/playout-delay
a=setup:passive
a=mid:1
b=AS:3500
a=sendrecv
a=candidate:udpcandidate 1 udp 1111 " + serverIp + " " + port + @" typ host
a=end-of-candidates
a=rtcp-mux
a=rtcp-rsize
";

		/*
		a=rid:r0 sendrecv
		a=rid:r1 sendrecv
		a=simulcast:recv r0;r1
		 */

		return new SdpOffer()
		{
			Sdp = sdp
		};
	}

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
			ReceiveArgs = new RtpSocketAsyncEventArgsRaw<T>(this);
			ReceiveArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
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
			ReceiveArgs = new RtpSocketAsyncEventArgs<T>(this);
			ReceiveArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
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
		if (IsRunningInRawMode)
		{
			RunReceiverRaw(null);
		}
		else
		{
			RunReceiver();
		}
		
	}

	private int PacketsReceived;

	private RtpSocketArgs<T> ReceiveArgs;
	private BinaryBufferPool ReceiveBuffers = new BinaryBufferPool(4096);

	private void OnMaintenanceTick(object source, System.Timers.ElapsedEventArgs e)
	{
		// Called once per second.
		// This has the job of looking for "gone" clients, as well as sending out the receiver/ sender reports.
		// Those reports are mandatory as they are a critical piece of congestion control. When they're not sent, remote clients assume the worst
		// and video quality drops significantly.
		Console.WriteLine("Stats: " + PacketsReceived);
		PacketsReceived = 0;

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
	/// Processes received UDP packets or schedules the processing of them.
	/// This is in raw mode meaning the packets start with an IP/UDP header.
	/// </summary>
	internal void RunReceiverRaw(BufferedBytes buffer)
	{
		#warning this is very close but not quite there!
		// State somewhere goes wrong - seemingly stops receiving packets entirely.
		// firefox disconnects due to failed ...?
		
		if (buffer != null)
		{
			try
			{
				OnReceiveRaw(buffer);
			}
			catch (Exception e)
			{
				Console.WriteLine("[Error] Packet receiver failed: " + e.ToString());
			}

			// Add args to the pool:
			buffer.Release();
		}

		var buff2 = ReceiveBuffers.Get();
		ReceiveArgs.SetBuffer(buff2.Bytes);
		ReceiveArgs.BufferedBytes = buff2;
		if (!ServerSocketUdp.ReceiveFromAsync(ReceiveArgs))
		{
			buff2.Length = ReceiveArgs.BytesTransferred;
			RunReceiverRaw(buff2);
		}

		return;

		var stackBottom = buffer;
		var stackTop = buffer;

		// The goal here is to start re-listening on the port as soon as possible.
		// This means the very first thing we do is get a new buffer in order to receive straight away - we don't process the latest buffer yet.

		while (true)
		{
			var buff = ReceiveBuffers.Get();
			ReceiveArgs.SetBuffer(buff.Bytes);
			ReceiveArgs.BufferedBytes = buff;
			var wasAsync = ServerSocketUdp.ReceiveFromAsync(ReceiveArgs);

			if (wasAsync)
			{
				break;
			}

			buff.Length = ReceiveArgs.BytesTransferred;

			// It completed synchronously because there was something in the buffer immediately.
			// Stack up the event objects and go again.
			// They're stacked up like this such that their original wire order is retained.

			if (stackBottom == null)
			{
				stackBottom = stackTop = buff;
			}
			else
			{
				stackTop.After = buff;
				stackTop = buff;
			}
		}

		// An async receive is now happening. In the meantime, we'll process the packets we have received (if any).
		while (stackBottom != null)
		{
			var args = stackBottom;
			stackBottom = args.After;
			args.After = null;

			try
			{
				OnReceiveRaw(args);
			}
			catch (Exception e)
			{
				Console.WriteLine("[Error] Packet receiver failed: " + e.ToString());
			}

			args.Release();
		}
	}

	/// <summary>
	/// Called when receiving a UDP packet
	/// </summary>
	internal void OnReceiveRaw(BufferedBytes poolBuffer)
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

internal sealed class RtpSocketAsyncEventArgs<T> : RtpSocketArgs<T>
	where T:RtpClient, new()
{
	public RtpSocketAsyncEventArgs(WebRTCServer<T> server) : base(server)
	{
	}

	protected override void OnCompleted(SocketAsyncEventArgs e)
	{
		_server.RunReceiver();
	}
}

internal class RtpSocketArgs<T> : SocketAsyncEventArgs
	where T : RtpClient, new()
{
	public BufferedBytes BufferedBytes;
	protected readonly WebRTCServer<T> _server;


	public RtpSocketArgs(WebRTCServer<T> server)
	{
		_server = server;
	}

}

internal sealed class RtpSocketAsyncEventArgsRaw<T> : RtpSocketArgs<T>
	where T:RtpClient, new()
{

	public RtpSocketAsyncEventArgsRaw(WebRTCServer<T> server) : base(server)
	{
	}

	protected override void OnCompleted(SocketAsyncEventArgs e)
	{
		BufferedBytes.Length = BytesTransferred;
		_server.RunReceiverRaw(BufferedBytes);
	}
}

/// <summary>
/// Reading and writing UDP and IP headers for both Ipv4 and Ipv6
/// </summary>
public static class UdpHeader
{
	/// <summary>
	/// Rolling identification field
	/// </summary>
	private static ushort Identification = 1;

	private static byte[] BasicUdpV4;
	private static byte[] BasicUdpV6;

	static UdpHeader()
	{
		BasicUdpV4 = new byte[28];

		BasicUdpV4[0] = 0x45; // Version and class
		// [1] DSCP/ ECN
		// [2..3] Length of header and payload (pop'd later)
		// [4..5] Identification
		// [6..7] Flags and fragment
		BasicUdpV4[8] = 128; // [8] TTL
		BasicUdpV4[9] = 17; // [9] UDP protocol
		// [10..11] header checksum (unused)
		// [12..15] Src IP
		// [16..19] Dst IP
		// [20..21] Src Port
		// [22..23] Dst Port
		// [24..25] Payload length (pop'd later)
		// [26..27] Checksum (pop'd later)


		BasicUdpV6 = new byte[48];
		BasicUdpV6[0] = 0x60; // Version and class
							  // [1..3] Flow label
							  // [4..5] Length
		BasicUdpV6[6] = 17; // [6] Next header
		BasicUdpV6[7] = 128; // [7] Hop limit
		// [8..23] Src IP
		// [24..39] Dst IP
		// [40..41] Src Port
		// [42..43] Dst Port
		// [44..45] Payload length (pop'd later)
		// [46..47] Checksum (pop'd later)
	}

	/// <summary>
	/// Gets the index that the payload starts at.
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="server"></param>
	/// <param name="remotePort"></param>
	/// <param name="ipBytes">Source IP bytes are placed in this span. Must be 16 bytes.</param>
	/// <param name="isV4">True if it's a v4 address</param>
	/// <returns></returns>
	public static int PayloadStart(byte[] buffer, WebRTCServer server, out ushort remotePort, ref Span<byte> ipBytes, out bool isV4)
	{
		var version = buffer[0] >> 4;

		if (version == 4)
		{
			isV4 = true;

			// V4
			var size = ((buffer[0] & 15) << 2) + 8;

			var port = (buffer[size - 6] << 8) | buffer[size - 5];

			if (port != server.Port)
			{
				remotePort = 0;
				return -1;
			}

			if (server.PortAndIpV4 == null)
			{
				// Write the destination ip/port now:
				var portIp = new byte[6];
				Array.Copy(buffer, size - 6, portIp, 0, 2);
				Array.Copy(buffer, 16, portIp, 2, 4);

				server.PortAndIpV4 = portIp;
			}

			// Get the remote IP. The given ipBytes span is always 16 bytes,
			// meaning we effectively output (correctly formatted) ipv6 mapped ipv4 addresses here.
			for (var i = 0; i < 4; i++)
			{
				ipBytes[i] = buffer[12 + i];
			}

			remotePort = (ushort)((buffer[size - 8] << 8) | buffer[size - 7]);
			return size; // Almost always 28
		}
		else if(version == 6)
		{
			// V6
			isV4 = false;

			for (var i = 0; i < 16; i++)
			{
				ipBytes[i] = buffer[8 + i];
			}
			
			// Read next header flag:
			var nextHeader = buffer[6];

			if (nextHeader == 17)
			{
				var port = (buffer[42] << 8) | buffer[43];

				if (port != server.Port)
				{
					remotePort = 0;
					return -1;
				}

				if (server.PortAndIpV6 == null)
				{
					// Write the destination ip/port now:
					var portIp = new byte[18];
					Array.Copy(buffer, 42, portIp, 0, 2);
					Array.Copy(buffer, 24, portIp, 2, 16);
					server.PortAndIpV6 = portIp;
				}

				remotePort = (ushort)((buffer[40] << 8) | buffer[41]);
				return 48;
			}

			if (nextHeader == 0)
			{
				// Hop by hop
				nextHeader = buffer[40];
				var size = buffer[41]; // Size minus the first 8 bytes (thus we add another 8 to it)

				if (nextHeader == 17)
				{
					// UDP is next. The actual payload starts at 40 + HbH 8 bytes + HbH size + UDP 8 bytes, for 56 + its size
					size += 56;

					var port = (buffer[size - 6] << 8) | buffer[size - 5];

					if (port != server.Port)
					{
						remotePort = 0;
						return -1;
					}
					
					if (server.PortAndIpV6 == null)
					{
						// Write the destination ip/port now:
						var portIp = new byte[18];
						Array.Copy(buffer, size - 6, portIp, 0, 2);
						Array.Copy(buffer, 24, portIp, 2, 16);

						server.PortAndIpV6 = portIp;
					}

					remotePort = (ushort)((buffer[size - 8] << 8) | buffer[size - 7]);
					return size;
				}
			}

			// All other extensions are things we can't handle, thus drop the packet.
		}

		// Drop packet
		remotePort = 0;
		isV4 = true;
		return -1;
	}

	/// <summary>
	/// Starts a UDP packet header into the given writer.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="portAndSrc"></param>
	/// <param name="dstPort"></param>
	/// <param name="dstAddressBytes"></param>
	public static void StartHeader(Writer writer, byte[] portAndSrc, ushort dstPort, ref Span<byte> dstAddressBytes)
	{
		// If IPv4:
		if (portAndSrc.Length == 6)
		{
			writer.Start(BasicUdpV4);
			var bytes = writer.FirstBuffer.Bytes;

			var idef = Identification++;
			bytes[4] = (byte)(idef >> 8);
			bytes[5] = (byte)idef;

			// Source IP:
			Array.Copy(portAndSrc, 2, bytes, 12, 4);

			// Destination IP:
			for (var i = 0; i < 4; i++)
			{
				bytes[16 + i] = dstAddressBytes[i]; 
			}

			// - UDP part -

			// Source Port:
			Array.Copy(portAndSrc, 0, bytes, 20, 2);

			// Destination Port:
			var port = dstPort;
			bytes[22] = (byte)(port >> 8);
			bytes[23] = (byte)port;
		}
		else
		{
			// IPv6
			writer.Start(BasicUdpV6);
			var bytes = writer.FirstBuffer.Bytes;

			var idef = Identification++;
			bytes[2] = (byte)(idef >> 8);
			bytes[3] = (byte)idef;

			// Source IP:
			Array.Copy(portAndSrc, 2, bytes, 8, 16);

			// Destination IP:
			for (var i = 0; i < 16; i++)
			{
				bytes[24 + i] = dstAddressBytes[i];
			}

			// - UDP part -

			// Source Port:
			Array.Copy(portAndSrc, 0, bytes, 40, 2);

			// Destination Port:
			var port = dstPort;
			bytes[42] = (byte)(port >> 8);
			bytes[43] = (byte)port;
		}

	}
	
	/// <summary>
	/// Starts a UDP packet header into the given writer.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="portAndSrc"></param>
	/// <param name="portAndDst"></param>
	public static void StartHeader(Writer writer, byte[] portAndSrc, byte[] portAndDst)
	{
		// If IPv4:
		if (portAndSrc.Length == 6)
		{
			writer.Start(BasicUdpV4);
			var bytes = writer.FirstBuffer.Bytes;

			var idef = Identification++;
			bytes[4] = (byte)(idef >> 8);
			bytes[5] = (byte)idef;

			// Source IP:
			Array.Copy(portAndSrc, 2, bytes, 12, 4);

			// Destination IP:
			Array.Copy(portAndDst, 2, bytes, 16, 4);

			// - UDP part -

			// Source Port:
			Array.Copy(portAndSrc, 0, bytes, 20, 2);

			// Destination Port:
			Array.Copy(portAndDst, 0, bytes, 22, 2);
		}
		else
		{
			// IPv6
			writer.Start(BasicUdpV6);
			var bytes = writer.FirstBuffer.Bytes;

			var idef = Identification++;
			bytes[2] = (byte)(idef >> 8);
			bytes[3] = (byte)idef;

			// Source IP:
			Array.Copy(portAndSrc, 2, bytes, 8, 16);

			// Destination IP:
			Array.Copy(portAndDst, 2, bytes, 24, 16);

			// - UDP part -

			// Source Port:
			Array.Copy(portAndSrc, 0, bytes, 40, 2);

			// Destination Port:
			Array.Copy(portAndDst, 0, bytes, 42, 2);
		}

	}

	private static uint net_checksum_add(byte[] buf, int offset, int len)
	{
		uint sum = 0;

		for (int i = 0; i < len; i++)
		{
			if ((i & 1) == 1)
			{
				sum += buf[offset + i];
			}
			else
			{
				sum += (uint)buf[offset + i] << 8;
			}
		}
		return sum;
	}

	private static ushort net_checksum_finish(uint sum)
	{
		while ((sum >> 16) != 0)
		{
			sum = (sum & 0xFFFF) + (sum >> 16);
		}

		var result = (ushort)~sum;

		if (result == 0)
		{
			// a 0 result must be sent as FFFF.
			return 0xFFFF;
		}

		return result;
	}

	private static ushort net_checksum_tcpudpv4(byte[] buf, int udpSize)
	{
		// Packet size is udpSize + 20
		uint sum = net_checksum_add(buf, 12, 8);    // src + dst address
		sum += 17 + (uint)udpSize;                // protocol + length 
		sum += net_checksum_add(buf, 20, udpSize);	// UDP section, i.e. ports, length, checksum (a 0, has no impact) and the payload
		
		return net_checksum_finish(sum);
	}
	
	private static ushort net_checksum_tcpudpv6(byte[] buf, int udpSize)
	{
		// Packet size is udpSize + 40
		uint sum = 0;

		sum += net_checksum_add(buf, 40, udpSize);	// UDP section, i.e. ports, length, checksum (a 0, has no impact) and the payload
		sum += net_checksum_add(buf, 8, 32);		// src + dst address
		sum += 17 + (uint)udpSize;				// protocol + length
		return net_checksum_finish(sum);
	}

	/// <summary>
	/// Completes a UDP packet header, writing the lengths and checksum.
	/// </summary>
	/// <param name="writer"></param>
	public static void Complete(Writer writer)
	{
		var buff = writer.FirstBuffer.Bytes;

		var isIpV4 = (buff[0] >> 4) == 4;

		if (isIpV4)
		{
			// Set both length fields. UDP payload and also the total packet size.
			var totalSize = writer.Length;

			// IP header - length:
			buff[2] = (byte)(totalSize >> 8);
			buff[3] = (byte)totalSize;

			totalSize -= 20;

			// UDP - length:
			buff[24] = (byte)(totalSize >> 8);
			buff[25] = (byte)totalSize;

			// Compute ipv4 checksum:
			var udpChecksum = net_checksum_tcpudpv4(buff, totalSize);

			// Write checksum:
			buff[26] = (byte)(udpChecksum >> 8);
			buff[27] = (byte)udpChecksum;
		}
		else
		{
			// Set both length fields. UDP payload and also the total packet size.
			var totalSize = writer.Length;

			// IP header - length:
			buff[4] = (byte)(totalSize >> 8);
			buff[5] = (byte)totalSize;

			totalSize -= 40;

			// UDP - length:
			buff[44] = (byte)(totalSize >> 8);
			buff[45] = (byte)totalSize;

			// Compute ipv6 checksum:
			var udpChecksum = net_checksum_tcpudpv6(buff, totalSize);

			// Write checksum:
			buff[46] = (byte)(udpChecksum >> 8);
			buff[47] = (byte)udpChecksum;
		}
	}
}

/// <summary>
/// RtpSocketSendAsyncEventArgs for a particular sender.
/// </summary>
public class RtpSocketSendAsyncEventArgs : SocketAsyncEventArgs
{
	/// <summary>
	/// The client this is in.
	/// </summary>
	public RtpClient Client;

	/// <summary>
	/// Called when done sending.
	/// </summary>
	/// <param name="args"></param>
	protected override void OnCompleted(SocketAsyncEventArgs args)
	{
		Client.CompletedCurrentSend();
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