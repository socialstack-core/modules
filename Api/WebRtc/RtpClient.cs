using Api.SocketServerLibrary;
using Api.SocketServerLibrary.Crypto;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Api.WebRTC;


/// <summary>
/// Stores state relating to a connected client over a WebRTC peer connection.
/// </summary>
public partial class RtpClient
{
	/// <summary>
	/// Tick time of the last message this client sent to us.
	/// If it goes >10s, the client is assumed to be gone.
	/// The value comes from the servers LatestTickTime.
	/// </summary>
	public long LastMessageAt;

	/// <summary>
	/// General RTP random generator.
	/// </summary>
	public static RandomNumberGenerator Rng = RandomNumberGenerator.Create();

	/// <summary>
	/// DTLS handshake metadata. Exists whilst MasterSecret is being established.
	/// </summary>
	public RtpClientHandshake HandshakeMeta = new RtpClientHandshake();

	/// <summary>
	/// Message ID for outbound rtcp messages.
	/// </summary>
	public uint CurrentRtcpId = 0;

	/// <summary>
	/// The server this is a client for.
	/// </summary>
	public WebRTCServer Server;

	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	public RtpClient NextClient;

	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	public RtpClient PreviousClient;

	/// <summary>
	/// True if this is the very first RTP packet seen.
	/// </summary>
	public bool FirstRtpPacket = true;

	/// <summary>
	/// Each SSRC (track ID essentially) in a RTP/ RTCP stream has its own sequence number.
	/// Need to keep track of these sequence numbers to watch for rollovers which happen somewhat frequently for video.
	/// </summary>
	public SsrcState[] SsrcState;

	/// <summary>
	/// Finds a client (in the same huddle) with the given ssrc.
	/// </summary>
	/// <param name="ssrc"></param>
	/// <returns></returns>
	public virtual RtpClient FindClient(uint ssrc)
	{
		return null;
	}

	/// <summary>
	/// Gets the SSRC state index in the SsrcState array for the given ssrc.
	/// </summary>
	/// <param name="ssrc"></param>
	/// <returns></returns>
	public int GetSsrcStateIndex(uint ssrc)
	{
		if (SsrcState == null)
		{
			return -1;
		}

		// This array is usually extremely short
		// (as in, most of the time it's just 1 entry, but can be up to 3).
		// So simply iterating over it is by far the fastest way to store and get the state information.
		for (var i = 0; i < SsrcState.Length; i++)
		{
			if (SsrcState[i].Ssrc == ssrc)
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// This client has sent a PLI for the given SSRC.
	/// </summary>
	/// <param name="ssrc"></param>
	public virtual void DidRequestPli(uint ssrc)
	{
	}

	/// <summary>
	/// True if this client will do something with an RTP packet.
	/// </summary>
	public virtual bool WillHandle { get; }

	/// <summary>
	/// An RTP packet was recvd by this client.
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="index"></param>
	/// <param name="messageSize"></param>
	/// <param name="payloadType"></param>
	/// <param name="extStart"></param>
	/// <param name="ssrc"></param>
	public virtual void HandleRtpPacket(byte[] buffer, int startIndex, int index, int messageSize, int payloadType, int extStart, uint ssrc)
	{
	}

	/// <summary>
	/// Called when this client is ticked by the maintenance loop
	/// </summary>
	/// <param name="ticks"></param>
	public virtual void OnUpdate(long ticks)
	{
		
	}

	/// <summary>
	/// Assigns an ssrc state index.
	/// </summary>
	/// <param name="ssrc"></param>
	/// <returns></returns>
	public int AssignSsrcStateIndex(uint ssrc)
	{
		if (SsrcState == null)
		{
			SsrcState = new SsrcState[1];
			SsrcState[0].Ssrc = ssrc;
			return 0;
		}

		var index = SsrcState.Length;
		Array.Resize(ref SsrcState, index + 1);
		SsrcState[index].Ssrc = ssrc;
		return index;
	}

	/// <summary>
	/// In RTP (and RTCP), the key to use when sending is different from the one when receiving.
	/// RTP and RTCP including when on the same socket use different keys so we end up with 4 key/auth variants.
	/// </summary>
	public byte[] ReceiveSaltKey;
	/// <summary>
	/// 
	/// </summary>
	public byte[] ReceiveIvStore;
	/// <summary>
	/// 
	/// </summary>
	public System.Security.Cryptography.HMACSHA1 ReceiveMac;
	/// <summary>
	/// 
	/// </summary>
	public SocketServerLibrary.Crypto.Aes128Cm ReceiveCipherCtr;

	/// <summary>
	/// 
	/// </summary>
	public Org.BouncyCastle.Crypto.Engines.AesEngine RtpSendCipher;
	/// <summary>
	/// 
	/// </summary>
	public byte[] SendSaltKey;
	/// <summary>
	/// 
	/// </summary>
	public byte[] SendIvStore;
	/// <summary>
	/// 
	/// </summary>
	public System.Security.Cryptography.HMACSHA1 SendMac;

	/// <summary>
	/// 
	/// </summary>
	public SocketServerLibrary.Crypto.Aes128Cm SendCipherCtr;
	
	/// <summary>
	/// Note that RTCP uses its own, different keys.
	/// </summary>
    public byte[] RtcpReceiveSaltKey;
	/// <summary>
	/// 
	/// </summary>
	public byte[] RtcpReceiveIvStore;
	/// <summary>
	/// 
	/// </summary>
	public System.Security.Cryptography.HMACSHA1 RtcpReceiveMac;
	/// <summary>
	/// 
	/// </summary>
	public SocketServerLibrary.Crypto.Aes128Cm RtcpReceiveCipherCtr;

	/// <summary>
	/// 
	/// </summary>
	public byte[] RtcpSendSaltKey;
	/// <summary>
	/// 
	/// </summary>
	public byte[] RtcpSendIvStore;
	/// <summary>
	/// 
	/// </summary>
	public System.Security.Cryptography.HMACSHA1 RtcpSendMac;
	/// <summary>
	/// 
	/// </summary>
	public SocketServerLibrary.Crypto.Aes128Cm RtcpSendCipherCtr;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="rocIn"></param>
	public void AuthenticatePacket(byte[] packetBuffer, int startIndex, int size, uint rocIn)
	{
		Span<byte> tempBuffer = stackalloc byte[20];

		// Temp put roc in the packet buffer:
		var tagStartIndex = startIndex + size - 10;

		packetBuffer[tagStartIndex] = (byte)(rocIn >> 24);
		packetBuffer[tagStartIndex + 1] = (byte)(rocIn >> 16);
		packetBuffer[tagStartIndex + 2] = (byte)(rocIn >> 8);
		packetBuffer[tagStartIndex + 3] = (byte)rocIn;

		var packetSpan = packetBuffer.AsSpan(startIndex, size - 6);
		
		SendMac.TryComputeHash(packetSpan, tempBuffer, out int _);

		for (var i = 0; i < 10; i++)
		{
			packetBuffer[tagStartIndex + i] = tempBuffer[i];
		}

	}

	/// <summary>
	/// Creates the authentication tag for RTCP packets (It's different from regular RTP, and uses a different key).
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="index"></param>
	public void AuthenticateRtcpPacket(byte[] packetBuffer, int startIndex, int size, uint index)
	{
		Span<byte> tempBuffer = stackalloc byte[20];

		var packetSpan = packetBuffer.AsSpan(startIndex, size - 10); // includes the packetIndex anyway

		RtcpSendMac.TryComputeHash(packetSpan, tempBuffer, out int _); // packetBuffer, startIndex, size - 14, index, tempBuffer);

		var tagStartIndex = startIndex + size - 10;

		for (var i = 0; i < 10; i++)
		{
			packetBuffer[tagStartIndex + i] = tempBuffer[i];
		}

	}

	/// <summary>
	/// Checks an RTP packet authentication tag (HMAC, SHA1)
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="packetIndex"></param>
	/// <returns></returns>
	public bool CheckRtcpPacketTag(byte[] packetBuffer, int startIndex, int size, uint packetIndex)
	{
		Span<byte> tempBuffer = stackalloc byte[20];

		var packetSpan = packetBuffer.AsSpan(startIndex, size - 10); // includes the packetIndex anyway

		RtcpReceiveMac.TryComputeHash(packetSpan, tempBuffer, out int _);
		
		var tagStartIndex = startIndex + size - 10;

		for (var i = 0; i < 10; i++)
		{
			if (packetBuffer[tagStartIndex + i] != tempBuffer[i])
			{
				return false;
			}
		}

		return true;
	}
	
	/// <summary>
	/// Checks an RTP packet authentication tag (HMAC, SHA1)
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="rocIn"></param>
	/// <returns></returns>
	public bool CheckPacketTag(byte[] packetBuffer, int startIndex, int size, uint rocIn)
	{
		var tagStartIndex = startIndex + size - 10;
			
		var tagA = packetBuffer[tagStartIndex];
		var tagB = packetBuffer[tagStartIndex+1];
		var tagC = packetBuffer[tagStartIndex+2];
		var tagD = packetBuffer[tagStartIndex+3];

		packetBuffer[tagStartIndex] = (byte)(rocIn >> 24);
		packetBuffer[tagStartIndex + 1] = (byte)(rocIn >> 16);
		packetBuffer[tagStartIndex + 2] = (byte)(rocIn >> 8);
		packetBuffer[tagStartIndex + 3] = (byte)rocIn;

		var packetSpan = packetBuffer.AsSpan(startIndex, size - 6); // Going to reuse 4 bytes of the tag itself
		
		Span<byte> tempBuffer = stackalloc byte[20];
		ReceiveMac.TryComputeHash(packetSpan, tempBuffer, out int _);

		for (var i = 4; i < 10; i++)
		{
			if (packetBuffer[tagStartIndex + i] != tempBuffer[i])
			{
				return false;
			}
		}

		// Check first 4 as well:
		if (tempBuffer[0] != tagA)
		{
			return false;
		}

		if (tempBuffer[1] != tagB)
		{
			return false;
		}

		if (tempBuffer[2] != tagC)
		{
			return false;
		}

		if (tempBuffer[3] != tagD)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="payloadIndex"></param>
	/// <param name="payloadSize"></param>
	/// <param name="packetIndex"></param>
	/// <param name="ssrc"></param>
	public void EncryptRtpPacket(byte[] buffer, int payloadIndex, int payloadSize, ulong packetIndex, uint ssrc)
	{
		Span<byte> ivStore = stackalloc byte[16];

		ivStore[0] = SendSaltKey[0];
		ivStore[1] = SendSaltKey[1];
		ivStore[2] = SendSaltKey[2];
		ivStore[3] = SendSaltKey[3];

		for (var i = 4; i < 8; i++)
		{
			ivStore[i] = (byte)((0xFF & (ssrc >> ((7 - i) * 8))) ^ SendSaltKey[i]);
		}

		for (var i = 8; i < 14; i++)
		{
			ivStore[i] = (byte)((0xFF & (byte)(packetIndex >> ((13 - i) * 8))) ^ SendSaltKey[i]);
		}

		// The last 10 bytes are the auth tag (checked above).
		SendCipherCtr.Process(buffer, payloadIndex, payloadSize, ivStore);

	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="payloadIndex"></param>
	/// <param name="payloadSize"></param>
	/// <param name="packetIndex"></param>
	/// <param name="ssrc"></param>
	public void EncryptRtcpPacket(byte[] buffer, int payloadIndex, int payloadSize, uint packetIndex, uint ssrc)
	{
		Span<byte> ivStore = stackalloc byte[16];

		ivStore[0] = RtcpSendSaltKey[0];
		ivStore[1] = RtcpSendSaltKey[1];
		ivStore[2] = RtcpSendSaltKey[2];
		ivStore[3] = RtcpSendSaltKey[3];

		// The shifts transform the ssrc and index into network order
		ivStore[4] = (byte)(((ssrc >> 24) & 0xff) ^ RtcpSendSaltKey[4]);
		ivStore[5] = (byte)(((ssrc >> 16) & 0xff) ^ RtcpSendSaltKey[5]);
		ivStore[6] = (byte)(((ssrc >> 8) & 0xff) ^ RtcpSendSaltKey[6]);
		ivStore[7] = (byte)((ssrc & 0xff) ^ RtcpSendSaltKey[7]);

		ivStore[8] = RtcpSendSaltKey[8];
		ivStore[9] = RtcpSendSaltKey[9];

		ivStore[10] = (byte)(((packetIndex >> 24) & 0xff) ^ RtcpSendSaltKey[10]);
		ivStore[11] = (byte)(((packetIndex >> 16) & 0xff) ^ RtcpSendSaltKey[11]);
		ivStore[12] = (byte)(((packetIndex >> 8) & 0xff) ^ RtcpSendSaltKey[12]);
		ivStore[13] = (byte)((packetIndex & 0xff) ^ RtcpSendSaltKey[13]);

		// The last 10 bytes are the auth tag (checked above).
		RtcpSendCipherCtr.Process(buffer, payloadIndex, payloadSize, ivStore);
	}

	/// <summary>
	/// Once the master key is known, the RTP keys can be known too.
	/// </summary>
	public void DeriveSrtpKeys()
	{
		// Run the PRF to obtain the keying material:
		var keyLen = 16;
		var saltLen = 14;

		// https://datatracker.ietf.org/doc/html/rfc5764#section-4.2
		var seed = new byte[64];
		Array.Copy(HandshakeMeta.DtlsRandom, 0, seed, 0, 64);
		byte[] keyingMaterial = Ciphers.HashAlgorithm.PRF(MasterSecret, "EXTRACTOR-dtls_srtp", seed, 2 * (keyLen + saltLen));

		byte[] srtpMasterClientKey = new byte[keyLen];
		byte[] srtpMasterServerKey = new byte[keyLen];
		byte[] srtpMasterClientSalt = new byte[saltLen];
		byte[] srtpMasterServerSalt = new byte[saltLen];
		Buffer.BlockCopy(keyingMaterial, 0, srtpMasterClientKey, 0, keyLen);
		Buffer.BlockCopy(keyingMaterial, keyLen, srtpMasterServerKey, 0, keyLen);
		Buffer.BlockCopy(keyingMaterial, 2 * keyLen, srtpMasterClientSalt, 0, saltLen);
		Buffer.BlockCopy(keyingMaterial, (2 * keyLen + saltLen), srtpMasterServerSalt, 0, saltLen);

		// Create the 4 key sets - RTP in/ out, then RTCP in/ out:
		ConfigureRtpKeys(srtpMasterClientKey, srtpMasterClientSalt, true, false);
		ConfigureRtpKeys(srtpMasterServerKey, srtpMasterServerSalt, false, false);

		ConfigureRtpKeys(srtpMasterClientKey, srtpMasterClientSalt, true, true);
		ConfigureRtpKeys(srtpMasterServerKey, srtpMasterServerSalt, false, true);
	}

	/// <summary>
	/// The SHA1 digest, thread safe as this object is internally stateless.
	/// </summary>
	public static Api.SocketServerLibrary.Crypto.Sha1Digest SHA1 = new Api.SocketServerLibrary.Crypto.Sha1Digest();

	private void ConfigureRtpKeys(byte[] masterKey, byte[] masterSalt, bool isReceive, bool isControl)
	{
		const int keyLen = 16;
		const int saltLen = 14;
		
		var encKey = new byte[keyLen]; // AES-128 CM
		var saltKey = new byte[saltLen];
		
		var authKey = new byte[20]; // SHA-1 based HMAC, 20 byte length key

		// compute the session encryption key

		Span<byte> ivStore = stackalloc byte[16]; // Temporary IV store.

		if (isControl)
		{
			ComputeIvRtcp(3, masterSalt, ref ivStore);
		}
		else
		{
			ComputeIv(0, masterSalt, ref ivStore);
		}

		var aes128 = new SocketServerLibrary.Crypto.Aes128Cm(masterKey.AsSpan());

		aes128.GetCipherStream(encKey, keyLen, ivStore);

		// compute the session authentication key

		if (isControl)
		{
			ComputeIvRtcp(4, masterSalt, ref ivStore);
		}
		else
		{
			ComputeIv(1, masterSalt, ref ivStore);
		}

		aes128.GetCipherStream(authKey, 20, ivStore);

		var mac = new System.Security.Cryptography.HMACSHA1(authKey);
		
		// compute the session salt

		if (isControl)
		{
			ComputeIvRtcp(5, masterSalt, ref ivStore);
		}
		else
		{
			ComputeIv(2, masterSalt, ref ivStore);
		}

		aes128.GetCipherStream(saltKey, 14, ivStore);

		// As last step: initialize cipher with derived encryption key.
		aes128.Init(encKey.AsSpan());

		if (isControl)
		{
			if (isReceive)
			{
				RtcpReceiveSaltKey = saltKey;
				RtcpReceiveCipherCtr = aes128;
				RtcpReceiveMac = mac;
			}
			else
			{
				RtcpSendSaltKey = saltKey;
				RtcpSendCipherCtr = aes128;
				RtcpSendMac = mac;
			}
		}
		else if (isReceive)
		{
			ReceiveSaltKey = saltKey;
			ReceiveCipherCtr = aes128;
			ReceiveMac = mac;
		}
		else
		{
			SendSaltKey = saltKey;
			SendCipherCtr = aes128;
			SendMac = mac;
		}

	}

	/// <summary>
	/// SRTP 4.1.2 - key derivation rate for the profiles we use is 0.
	/// </summary>
	public const int keyDerivationRate = 0;

	private void ComputeIv(long label, byte[] masterSalt, ref Span<byte> ivStore)
	{
		long key_id = label << 48;

		/*
		 * keyDerivationRate is a constant 0 in all our ciphers, so we can skip this.
		if (keyDerivationRate == 0)
		{
			key_id = label << 48;
		}
		else
		{
			key_id = ((label << 48) | (index / keyDerivationRate));
		}
		*/

		for (int i = 0; i < 7; i++)
		{
			ivStore[i] = masterSalt[i];
		}

		for (int i = 7; i < 14; i++)
		{
			ivStore[i] = (byte)((byte)(0xFF & (key_id >> (8 * (13 - i)))) ^ masterSalt[i]);
		}
	}
	
	private void ComputeIvRtcp(byte label, byte[] masterSalt, ref Span<byte> ivStore)
	{
		for (int i = 0; i < 14; i++)
		{
			ivStore[i] = masterSalt[i];
		}
		ivStore[7] ^= (byte)label;
	}

	/// <summary>
	/// The password used during offer/answer. The bytes are always in the ascii range.
	/// </summary>
	public byte[] SdpSecret = new byte[32];

	/// <summary>
	/// HMAC used for the Stun process, based on the SDP secret.
	/// </summary>
	public Api.SocketServerLibrary.Crypto.HMac StunHmac;

	/// <summary>
	/// Cipher to use for encryption on DTLS.
	/// This is actually barely used in WebRTC - most of the action is done by the use_srtp DTLS extension and its own cipher.
	/// </summary>
	public Ciphers.Cipher DtlsCipher;
	
	/// <summary>
	/// Current expected DTLS sequence number.
	/// </summary>
	public long ReceiveDtlsSequence = -1;
	
	/// <summary>
	/// Current expected DTLS sequence number.
	/// </summary>
	public ulong SendDtlsSequence = 0;

	/// <summary>
	/// The ID used in the clients username.
	/// </summary>
	public int TemporaryConnectId;

	/// <summary>
	/// True if they have been added to the lookup by their address.
	/// </summary>
	public bool AddedToIpLookup;

	/// <summary>
	/// The end clients remote address.
	/// </summary>
	public IPEndPoint RemoteAddress;

	/// <summary>
	/// Derived key to use in DTLS encryption.
	/// </summary>
	public byte[] MasterSecret;

	/// <summary>
	/// Decryption/ encryption cipher
	/// </summary>
	public TlsCipher Encryption;

	/// <summary>
	/// True if sending can happen. It's false (and sent messages will be queued) if a send is currently in progress.
	/// </summary>
	public bool CanProcessSend = true;

	/// <summary>
	/// The secret as an allocated string.
	/// </summary>
	/// <returns></returns>
	public string SecretAsString()
	{
		return Encoding.ASCII.GetString(SdpSecret);
	}

	/// <summary>
	/// Gets a client address fragment.
	/// </summary>
	/// <param name="depth"></param>
	/// <returns></returns>
	public int GetAddressFragment(int depth)
	{
		if (depth == -2)
		{
			return RemoteAddress.Port;
		}

		// Otherwise it's part of the IP.
		Span<byte> addrBytes = stackalloc byte[16];
		RemoteAddress.Address.TryWriteBytes(addrBytes, out int addrSize);

		if (depth >= addrSize)
		{
			return 0;
		}

		return (addrBytes[depth] << 8) | addrBytes[depth + 1];
	}

	/// <summary>
	/// The ice username to use as an allocated string.
	/// </summary>
	/// <returns></returns>
	public string IceUsername()
	{
		return "rtpuser" + TemporaryConnectId;
	}

	/// <summary>
	/// Creates a new client. Generates a key to use in the offer.
	/// </summary>
	public RtpClient()
	{
		Rng.GetBytes(SdpSecret, 0, 16);

		// Encode the bytes such that they are in ascii range. It's always alphabetical.
		for (var i = 0; i < 16; i++)
		{
			// Generated byte:
			var currentByte = SdpSecret[15 - i];

			// Ascii versions:
			var hexOffset = 30 - (i * 2);
			SdpSecret[hexOffset] = (byte)(97 + (currentByte & 15));
			SdpSecret[hexOffset + 1] = (byte)(97 + ((currentByte >> 4) & 15));
		}
	}

	/// <summary>
	/// 2 byte port followed by either 4 or 16 byte IP address in network order (BE)
	/// </summary>
	public byte[] PortAndIp;

	/// <summary>
	/// Sets up RemoteAddress as a copy of the given IPEndpoint.
	/// </summary>
	/// <param name="port"></param>
	/// <param name="addressBytes"></param>
	/// <param name="ipv4"></param>
	public void SetAddressAs(ushort port, ref Span<byte> addressBytes, bool ipv4)
	{
		if (ipv4)
		{
			var ipv4Span = addressBytes.Slice(0, 4);
			RemoteAddress = new IPEndPoint(new IPAddress(ipv4Span), port);
		}
		else
		{
			RemoteAddress = new IPEndPoint(new IPAddress(addressBytes), port);
		}
		
		if (!ipv4)
		{
			PortAndIp = new byte[18];
			for (var i = 0; i < 16; i++)
			{
				PortAndIp[i + 2] = addressBytes[i];
			}
		}
		else
		{
			PortAndIp = new byte[6];
			for (var i = 0; i < 4; i++)
			{
				PortAndIp[i + 2] = addressBytes[i];
			}
		}

		PortAndIp[0] = (byte)(port >> 8);
		PortAndIp[1] = (byte)port;
	}

	/// <summary>
	/// Remote end requested a closure of the client
	/// </summary>
	public virtual void CloseRequested()
	{
		if (Server != null)
		{
			Server.RemoveFromLookup(this);
		}
	}

	/// <summary>
	/// Sends the given writer contents and releases the writer.
	/// </summary>
	/// <param name="writer"></param>
	public bool SendAndRelease(Writer writer)
	{
		if (Server.IsRunningInRawMode)
		{
			// Finish the UDP header:
			UdpHeader.Complete(writer);
		}

		// We only use one buffer from these writers.
		var buffer = writer.FirstBuffer as WebRTCBuffer;
		buffer.Length = writer.CurrentFill;
		buffer.Target = RemoteAddress;

		Server.BytesSent += buffer.Length;

		// We will manually release the buffers. Writer itself can go though:
		writer.FirstBuffer = null;
		writer.Release();

		// Add the writer to the servers send queue:
		var goNow = false;

		lock (Server.sendQueueLock)
		{
			if (Server.LastSendFrame == null)
			{
				Server.FirstSendFrame = buffer;
				Server.LastSendFrame = buffer;
				goNow = Server.CanStartSend;
				Server.CanStartSend = false;
			}
			else
			{
				Server.LastSendFrame.After = buffer;
				Server.LastSendFrame = buffer;
			}
		}

		if (goNow)
		{
			// We have the exclusive right to start a sender.

			// Send on a pool thread
			Task.Run(async () => {

				// Offload the current queue
				WebRTCBuffer frame = Server.FirstSendFrame;

				lock (Server.sendQueueLock)
				{
					Server.FirstSendFrame = null;
					Server.LastSendFrame = null;
				}

				WebRTCBuffer lastProcessed = null;

				while (frame != null)
				{
					await Server.ServerSocketUdp.SendToAsync(frame.Bytes.AsMemory(0, frame.Length), SocketFlags.None, frame.Target);

					var next = frame.After as WebRTCBuffer;
					frame.Release();
					lastProcessed = frame;
					frame = next;

					if (frame == null)
					{
						// Check if there is now more in the send queue.
						lock (Server.sendQueueLock)
						{
							frame = Server.FirstSendFrame;
							Server.FirstSendFrame = null;
							Server.LastSendFrame = null;

							if (frame == null)
							{
								Server.CanStartSend = true;
							}
						}
					}
				}
			});
		}

		return true;
	}

	/*
	/// <summary>
	/// Called when the current send operation has completed.
	/// Proceeds to send the next thing in the queue.
	/// </summary>
	public void CompletedCurrentSend()
	{
		while (true)
		{
			var frame = FirstSendFrame;

			if (frame == null)
			{
				CanProcessSend = true;
				return;
			}

			if (frame.Current == null)
			{
				// This occurs when we just sent a websocket header.
				// Get the actual first buffer now:
				frame.Current = frame.Writer.FirstBuffer;
			}
			else
			{
				frame.Current = frame.Current.After;
			}

			if (frame.Current == null)
			{
				// Done sending this writer.

				// Pop the first send frame:
				SendStackFrame next;

				lock (this)
				{
					next = frame.After;
					FirstSendFrame = next;

					if (next == null)
					{
						LastSendFrame = null;
					}
				}

				frame.Release();

				if (next == null)
				{
					CanProcessSend = true;
					return;
				}

				frame = next;

				if (frame.Current == null)
				{
					// WS header sent or wasn't needed. Set the current to the first buffer:
					frame.Current = frame.Writer.FirstBuffer;

					if (frame.Current == null)
					{
						Console.WriteLine("Writer appears to have been released prematurely. " + frame.Writer.SendQueueCount);
						return;
					}

				}

			}

			// Process next piece.
			var buffer = frame.Current;
			AsyncArgs.SetBuffer(buffer.Bytes, buffer.Offset, buffer.Length);

			if (Server.ServerSocketUdp.SendToAsync(AsyncArgs))
			{
				return;
			}
		}
	}
	*/
}
