using Api.Signatures;
using Api.SocketServerLibrary;
using Api.SocketServerLibrary.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Stores state relating to a connected client over a WebRTC peer connection.
/// </summary>
public partial class BlockClient
{
	/// <summary>
	/// Message ID for outbound rtcp messages.
	/// </summary>
	public uint CurrentRtcpId = 0;

	/// <summary>
	/// The server this is a client for.
	/// </summary>
	public BlockServer Server;

	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	public BlockClient NextClient;

	/// <summary>
	/// Linked list of active clients.
	/// </summary>
	public BlockClient PreviousClient;

	/// <summary>
	/// The handshake metadata, if there is any.
	/// </summary>
	public BlockHandshakeMeta HandshakeMeta;

	/// <summary>
	/// Called when this client is ticked by the maintenance loop
	/// </summary>
	/// <param name="ticks"></param>
	public virtual void OnUpdate(long ticks)
	{
		
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
	public Api.SocketServerLibrary.Crypto.HMac ReceiveMac;
	/// <summary>
	/// 
	/// </summary>
	public Api.SocketServerLibrary.Crypto.Aes128Cm ReceiveCipherCtr;

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
	public Api.SocketServerLibrary.Crypto.HMac SendMac;

	/// <summary>
	/// 
	/// </summary>
	public Api.SocketServerLibrary.Crypto.Aes128Cm SendCipherCtr;

	/// <summary>
	/// Tracks replay with a 64 packet history.
	/// </summary>
	public ulong ReplayWindow;

	/// <summary>
	/// First or latest observed sequence number.
	/// </summary>
	public uint LatestSequence;

	/// <summary>
	/// Rollover code - essentially the number of times the sequence number has rolled over.
	/// </summary>
	public uint RolloverCode; // "roc", RFC 3711 3.3.1

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

	/// <summary>
	/// Updates the packet index information. Occurs after GuessIndex.
	/// </summary>
	/// <param name="seqNo"></param>
	/// <param name="delta"></param>
	/// <param name="guessedROC"></param>
	public void UpdatePacketIndex(uint seqNo, long delta, uint guessedROC)
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

		if (seqNo > LatestSequence)
		{
			LatestSequence = seqNo;
		}

		if (guessedROC > RolloverCode)
		{
			RolloverCode = guessedROC;
			LatestSequence = seqNo;
		}
	}
	
	/// <summary>
	/// 'guesses' the extended index for a given sequence number.
	/// The guess is extremely accurate - it is only wrong if a packet is delayed by multiple minutes or has an incorrect sequence number.
	/// </summary>
	/// <param name="seqNo"></param>
	/// <param name="guessedROC"></param>
	/// <returns></returns>
	public ulong GuessPacketIndex(uint seqNo, out uint guessedROC)
	{
		if (LatestSequence < 2147483648)
		{
			if (seqNo - LatestSequence > 2147483648)
			{
				// This packet is from the prev roc (this can happen just after a recent rollover).
				guessedROC = (uint)(RolloverCode - 1);
			}
			else
			{
				guessedROC = RolloverCode;
			}
		}
		else
		{
			if (LatestSequence - 2147483648 > seqNo)
			{
				// This packet is for the next roc (this happens just around a rollover, when the remote client has rolled but we haven't yet).
				guessedROC = RolloverCode + 1;
			}
			else
			{
				guessedROC = RolloverCode;
			}
		}

		return ((ulong)guessedROC << 32) | seqNo;
	}

	/// <summary>
	/// 
	/// </summary>
	public ulong LatestExtendedSequence => (uint)((RolloverCode << 32) | LatestSequence);
	
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
		
		SendMac.StatelessOutput(packetBuffer, startIndex, size - 10, rocIn, tempBuffer);

		var tagStartIndex = startIndex + size - 10;

		for (var i = 0; i < 10; i++)
		{
			packetBuffer[tagStartIndex + i] = tempBuffer[i];
		}

	}

	/// <summary>
	/// Checks a packet authentication tag (HMAC, SHA1, 80 bit)
	/// </summary>
	/// <param name="packetBuffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="size"></param>
	/// <param name="rocIn"></param>
	/// <returns></returns>
	public bool CheckPacketTag(byte[] packetBuffer, int startIndex, int size, uint rocIn)
	{
		Span<byte> tempBuffer = stackalloc byte[20];
		ReceiveMac.StatelessOutput(packetBuffer, startIndex, size - 10, rocIn, tempBuffer);

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

	private static readonly byte[] AES_KEY_LABEL = System.Text.Encoding.ASCII.GetBytes("block-extract-aes");
	private static readonly byte[] IV_KEY_LABEL = System.Text.Encoding.ASCII.GetBytes("iv-extract-label");

	/// <summary>
	/// Once the master key is known, the encryption/ MAC keys can be known too.
	/// </summary>
	public void DeriveKeys()
	{
		// MasterSecret is 32 bytes and we have 16 bytes from the client and 16 bytes from the server.
		// We need 2 AES keys which are 128 bit (16 bytes) each, as well as 2 salts for their IV's and those are 14 bytes each.

		// Note that 2 keys are used (send/ recv) to avoid any possible IV sequence number reuse.
		// Generally, this is very similar to the DTLS_SRTP extractor:
		// https://datatracker.ietf.org/doc/html/rfc5764#section-4.2

		Span<byte> keyMaterial = stackalloc byte[64];

		PRF(new Sha3Digest(), MasterSecret, AES_KEY_LABEL, IV_KEY_LABEL, HandshakeMeta.RandomData, keyMaterial);

		// Create the 2 key sets:
		ConfigureKeys(keyMaterial, 0, true);
		ConfigureKeys(keyMaterial, 32, false);
	}

	private static void PRF(Sha3Digest digest, byte[] secret, byte[] label, byte[] label2, byte[] seed, Span<byte> output)
	{
		// Keccack digest is not vulnerable to length extension so we can effectively write the 3 separate values to it to obtain our initial hash.
		digest.BlockUpdate(secret, 0, secret.Length);
		digest.BlockUpdate(label, 0, label.Length);
		digest.BlockUpdate(seed, 0, seed.Length);
		digest.DoFinal(output, 0);

		// Note: DoFinal internally reset the digest.

		// Secondary segment:
		for (var i = 0; i < 32; i++)
		{
			digest.Update(output[i]);
		}
		digest.BlockUpdate(secret, 0, secret.Length);
		digest.BlockUpdate(label2, 0, label2.Length);
		digest.BlockUpdate(seed, 0, seed.Length);
		digest.DoFinal(output, 32);
	}
	
	/// <summary>
	/// The SHA1 digest, thread safe as this object is internally stateless.
	/// </summary>
	public static Api.SocketServerLibrary.Crypto.Sha1Digest SHA1 = new Api.SocketServerLibrary.Crypto.Sha1Digest();

	private void ConfigureKeys(Span<byte> keyMaterial, int offset, bool isReceive)
	{
		var keyLen = 16;
		var saltLen = 14;

		var mac = new Api.SocketServerLibrary.Crypto.HMac(SHA1);
		
		var masterKey = new byte[keyLen];

		for (var i = 0; i < keyLen; i++)
		{
			masterKey[i] = keyMaterial[offset + i];
		}

		var aes128 = new Api.SocketServerLibrary.Crypto.Aes128Cm(masterKey);

		var encKey = new byte[keyLen]; // AES-128 CM
		var saltKey = new byte[saltLen];
		
		// Send and receive key materials are different to avoid the
		// otherwise guaranteed reuse of a sequence number.
		if (isReceive)
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

		var authKey = new byte[20]; // SHA-256 based HMAC, 20 byte length key

		// compute the session encryption key

		Span<byte> ivStore = stackalloc byte[16]; // Temporary IV store.

		// Construct initial IV:
		offset += 16;
		ComputeIv(0, keyMaterial, offset, ref ivStore);

		// Initialise the cipher and using the above IV, generate the session encryption key.
		aes128.GetCipherStream(encKey, 16, ivStore);

		// encKey now set.

		// Construct next IV. This one will be used for the authentication key. Generate the key too.
		ComputeIv(1, keyMaterial, offset, ref ivStore);
		aes128.GetCipherStream(authKey, 20, ivStore);

		// authKey now set. Init the HMAC:
		mac.Init(authKey, 0, authKey.Length);

		// Finally, construct IV for the salt and generate the salt.
		ComputeIv(2, keyMaterial, offset, ref ivStore);
		aes128.GetCipherStream(saltKey, 14, ivStore);

		// Initialize cipher with derived encryption key.
		aes128.Init(encKey);
	}

	/// <summary>
	/// SRTP 4.1.2 - key derivation rate for the profiles we use is 0.
	/// </summary>
	public const int keyDerivationRate = 0;

	private void ComputeIv(long label, Span<byte> masterSalt, int saltOffset, ref Span<byte> ivStore)
	{
		long key_id = label << 48;

		for (int i = 0; i < 7; i++)
		{
			ivStore[i] = masterSalt[saltOffset + i];
		}

		for (int i = 7; i < 14; i++)
		{
			ivStore[i] = (byte)((byte)(0xFF & (key_id >> (8 * (13 - i)))) ^ masterSalt[saltOffset + i]);
		}
	}
	
	/// <summary>
	/// The ID used in the clients username.
	/// </summary>
	public int TemporaryConnectId;

	/// <summary>
	/// True if they have been added to the lookup by their address.
	/// </summary>
	public bool AddedToLookup;

	/// <summary>
	/// The end clients remote address.
	/// </summary>
	public IPEndPoint RemoteAddress;

	/// <summary>
	/// Derived master secret.
	/// </summary>
	public byte[] MasterSecret;

	/// <summary>
	/// True if sending can happen. It's false (and sent messages will be queued) if a send is currently in progress.
	/// </summary>
	public bool CanProcessSend = true;

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
	/// Creates a new client. Generates a key to use in the offer.
	/// </summary>
	public BlockClient()
	{
	}

	/// <summary>
	/// 2 byte port followed by either 4 or 16 byte IP address in network order (BE)
	/// </summary>
	public byte[] PortAndIp;

	/// <summary>
	/// True if this is an IPv6 client.
	/// </summary>
	public bool IPv6;

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
			IPv6 = true;
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
	/// Sends a server hello message to this client.
	/// </summary>
	/// <param name="encMessageRaw">The Sike encoded payload.</param>
	/// <param name="ecdsaPublicKey">The ECDSA public key.</param>
	/// <param name="remoteTempId">The ID the remote wants us to use to identify ourselves on the next message.</param>
	public void SendServerHello(byte[] encMessageRaw, byte[] ecdsaPublicKey, ushort remoteTempId)
	{
		// - start of ServerHello record -
		var writer = Server.StartMessage();
		
		// 16 server random bytes:
		writer.Write(HandshakeMeta.RandomData, 16, 16);
		
		// Sike response:
		var sikeSize = encMessageRaw.Length;
		writer.WriteBE((ushort)sikeSize);
		writer.Write(encMessageRaw, 0, sikeSize);

		// ECDSA public key as well:
		var ecdsaSize = ecdsaPublicKey.Length;
		writer.WriteBE((ushort)ecdsaSize);
		writer.Write(ecdsaPublicKey, 0, ecdsaSize);

		SendAndRelease(writer);
	}

	private static Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator _generator;
	private static ECDomainParameters _domainParameters;

	/// <summary>
	/// Generates a key pair.
	/// </summary>
	/// <returns></returns>
	private static KeyPair GenerateClassicKeyPair()
	{
		if (_generator == null)
		{
			var curve = Org.BouncyCastle.Crypto.EC.CustomNamedCurves.GetByName("Curve25519");
			var domainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());
			_domainParameters = domainParameters;

			var keyParams = new ECKeyGenerationParameters(domainParameters, BlockHandshakeMeta.Rng);

			_generator = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator("ECDSA");
			_generator.Init(keyParams);
		}

		var keyPair = _generator.GenerateKeyPair();

		var privateKey = keyPair.Private as ECPrivateKeyParameters;
		var publicKey = keyPair.Public as ECPublicKeyParameters;

		return new KeyPair()
		{
			PrivateKeyBytes = privateKey.D.ToByteArrayUnsigned(),
			PublicKey = publicKey,
			PrivateKey = privateKey
		};
	}

	/// <summary>
	/// Reads the next byte in a sequence of BufferedByte fragments.
	/// </summary>
	/// <param name="index"></param>
	/// <param name="buffer"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	private byte NextByte(ref int index, ref BufferedBytes buffer, ref int max)
	{
		if (index == max)
		{
			buffer = buffer.After;
			index = buffer.Offset;
			max = buffer.Length - index;
		}

		return buffer.Bytes[index++];
	}

	/// <summary>
	/// Reads a compressed number from a sequence of BufferedByte fragments.
	/// </summary>
	/// <param name="index"></param>
	/// <param name="buffer"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	private ulong ReadCompressed(ref int index, ref BufferedBytes buffer, ref int max)
	{
		var first = NextByte(ref index, ref buffer, ref max);

		switch (first)
		{
			case 251:
				// 2 bytes:
				return (ulong)(NextByte(ref index, ref buffer, ref max) | (NextByte(ref index, ref buffer, ref max) << 8));
			case 252:
				// 3 bytes:
				return (ulong)(NextByte(ref index, ref buffer, ref max) | (NextByte(ref index, ref buffer, ref max)  << 8) | (NextByte(ref index, ref buffer, ref max) << 16));
			case 253:
				// 4 bytes:
				return (ulong)(NextByte(ref index, ref buffer, ref max) | (NextByte(ref index, ref buffer, ref max) << 8) | (NextByte(ref index, ref buffer, ref max) << 16) | (NextByte(ref index, ref buffer, ref max) << 24));
			case 254:
				// 8 bytes:
				return (ulong)NextByte(ref index, ref buffer, ref max) | ((ulong)NextByte(ref index, ref buffer, ref max) << 8) | ((ulong)NextByte(ref index, ref buffer, ref max) << 16) | ((ulong)NextByte(ref index, ref buffer, ref max) << 24) |
					((ulong)NextByte(ref index, ref buffer, ref max) << 32) | ((ulong)NextByte(ref index, ref buffer, ref max) << 40) | ((ulong)NextByte(ref index, ref buffer, ref max) << 48) | ((ulong)NextByte(ref index, ref buffer, ref max) << 56);
			default:
				return first;
		}
	}

	/// <summary>
	/// Sends a clientHello message to essentially start a key exchange.
	/// You only ever need to do the key exchange once with a remote node.
	/// </summary>
	public void SendClientHello()
	{
		HandshakeMeta = new BlockHandshakeMeta(0);
		
		// Generate a classic keypair. This is such that to get through this handshake
		// you have to break both quantum safe and classic exchanges.
		// This is recommended until quantum safe exchanges are something we can fully depend on.
		var classicKey = GenerateClassicKeyPair();
		HandshakeMeta.ClassicKey = classicKey;

		// - start of ServerHello record -
		var writer = Server.StartMessage();

		// Flags - unencrypted:
		writer.Write((byte)0);

		// Compressed node ID; this is 0 throughout the handshake:
		writer.Write((byte)0);

		// The temporary ID:
		writer.WriteBE((ushort)TemporaryConnectId);

		// Message type - ClientHello (0):
		writer.Write((byte)0);

		// Fragment count. This message is never fragmented. The payload is ~600 bytes which easily fits inside a packet.
		writer.Write((byte)1);

		// 16 client random bytes:
		writer.Write(HandshakeMeta.RandomData, 0, 16);

		// Sike handshake start. "Bob" (client) end:
		var sikeKey = Server.SikeKeyGenerator.GenerateKeyPairOpti(Lumity.SikeIsogeny.Party.BOB);
		HandshakeMeta.SikeKey = sikeKey;
		var sikePublicKey = sikeKey.PublicKey.GetEncoded();

		// Write Sike public key:
		var len = sikePublicKey.Length;
		writer.WriteBE((ushort)len);
		writer.Write(sikePublicKey, 0, len);

		// Write ECDH public key:
		var ecdsaPublicKey = classicKey.PublicKey.Q.GetEncoded(false);
		var ecdsaSize = ecdsaPublicKey.Length;
		writer.WriteBE((ushort)ecdsaSize);
		writer.Write(ecdsaPublicKey, 0, ecdsaSize);

		// Send the ClientHello:
		SendAndRelease(writer);
	}

	private void ConstructSharedSecretAndDeriveKeys(byte[] sikeSecret, byte[] ecdhSecret)
	{
		// Create the main master secret with SHA3:
		var sharedSecret = new Sha3Digest();
		sharedSecret.BlockUpdate(sikeSecret, 0, sikeSecret.Length);
		sharedSecret.BlockUpdate(ecdhSecret, 0, ecdhSecret.Length);

		MasterSecret = new byte[32];
		sharedSecret.DoFinal(MasterSecret, 0);

		// Derive enc/ auth bidi keys:
		DeriveKeys();
	}

	/// <summary>
	/// Called when this client has received a complete message, which may have been fragmented.
	/// The provided buffers MUST be offset to the start of the messages; i.e. 
	/// it begins with a messageType byte followed by fragment fields, followed by the messageType specific info.
	/// </summary>
	/// <param name="first"></param>
	public void ReceiveMessage(BufferedBytes first)
	{
		var index = first.Offset;
		var buffer = first;
		var max = first.Length - first.Offset;
		var messageType = NextByte(ref index, ref buffer, ref max);

		var fragCount = ReadCompressed(ref index, ref buffer, ref max);

		if (fragCount > 1)
		{
			// Skip frag index.
			ReadCompressed(ref index, ref buffer, ref max);
		}

		int pubKeySize;
		byte[] remotePublic;
		byte[] ecdhSecret;
		byte[] sikeSecret;
		ECPublicKeyParameters peerPublicKey;

		switch (messageType)
		{
			case 0:
				// ClientHello (handshake)
				HandshakeMeta = new BlockHandshakeMeta(16);

				// Generate a classic keypair. This is such that to get through this handshake
				// you have to break both quantum safe and classic exchanges.
				// This is recommended until quantum safe exchanges are something we can fully depend on.
				var classicKey = GenerateClassicKeyPair();
				HandshakeMeta.ClassicKey = classicKey;

				// Their temporary ID:
				var remoteTempId = (ushort)(NextByte(ref index, ref buffer, ref max) << 8 | NextByte(ref index, ref buffer, ref max));

				// Read the 16 random bytes:
				for (var i = 0; i < 16; i++)
				{
					HandshakeMeta.RandomData[i] = NextByte(ref index, ref buffer, ref max);
				}
				
				// Load Bobs Sike pubkey:
				pubKeySize = NextByte(ref index, ref buffer, ref max) << 8 | NextByte(ref index, ref buffer, ref max);

				remotePublic = new byte[pubKeySize];

				for (var i = 0; i < pubKeySize; i++)
				{
					remotePublic[i] = NextByte(ref index, ref buffer, ref max);
				}

				// Load the public Sike key:
				var loadedRemotePublic = new SikeIsogeny.SidhPublicKeyOpti(Server.SikeParam, remotePublic);
				
				// Remote's ECDSA pubkey too:
				pubKeySize = NextByte(ref index, ref buffer, ref max) << 8 | NextByte(ref index, ref buffer, ref max);
				remotePublic = new byte[pubKeySize];

				for (var i = 0; i < pubKeySize; i++)
				{
					remotePublic[i] = NextByte(ref index, ref buffer, ref max);
				}

				peerPublicKey = TlsEccUtilities.DeserializeECPublicKey(null, _domainParameters, remotePublic);
				ecdhSecret = TlsEccUtilities.CalculateECDHBasicAgreement(peerPublicKey, classicKey.PrivateKey);

				var encapsulated = Server.Sike.Encapsulate(loadedRemotePublic);

				// The Sike shared secret is:
				sikeSecret = encapsulated.GetSecret();
				var encMessage = encapsulated.GetEncryptedMessage();

				// Encode the public key:
				var ecdsaPublicKey = classicKey.PublicKey.Q.GetEncoded(false);
				
				// Get the reply:
				var encMessageRaw = encMessage.GetEncoded();

				// We now have enough info to reply to the client and calculate all our keys.
				ConstructSharedSecretAndDeriveKeys(sikeSecret, ecdhSecret);

				// Respond with a ServerHello (unencrypted as other end doesn't know the key yet).
				SendServerHello(encMessageRaw, ecdsaPublicKey, remoteTempId);

				break;
			case 1:
				// ServerHello (handshake)

				// This end (client) can now establish the MasterSecret too.

				if (HandshakeMeta == null)
				{
					// Wrong order - most likely abuse attempt.
					return;
				}

				// Read the 16 random bytes:
				for (var i = 0; i < 16; i++)
				{
					HandshakeMeta.RandomData[i + 16] = NextByte(ref index, ref buffer, ref max);
				}

				// Read the Sike message:
				pubKeySize = NextByte(ref index, ref buffer, ref max) << 8 | NextByte(ref index, ref buffer, ref max);
				remotePublic = new byte[pubKeySize];
				for (var i = 0; i < pubKeySize; i++)
				{
					remotePublic[i] = NextByte(ref index, ref buffer, ref max);
				}

				// Establish the Sike shared secret:
				var encMessageRef = new Lumity.SikeIsogeny.EncryptedMessageOpti(Server.SikeParam, remotePublic);
				sikeSecret = Server.Sike.Decapsulate(HandshakeMeta.SikeKey.PrivateKey, HandshakeMeta.SikeKey.PublicKey, encMessageRef);

				// Remote's ECDSA pubkey too:
				pubKeySize = NextByte(ref index, ref buffer, ref max) << 8 | NextByte(ref index, ref buffer, ref max);
				remotePublic = new byte[pubKeySize];

				for (var i = 0; i < pubKeySize; i++)
				{
					remotePublic[i] = NextByte(ref index, ref buffer, ref max);
				}

				peerPublicKey = TlsEccUtilities.DeserializeECPublicKey(null, _domainParameters, remotePublic);
				ecdhSecret = TlsEccUtilities.CalculateECDHBasicAgreement(peerPublicKey, HandshakeMeta.ClassicKey.PrivateKey);

				ConstructSharedSecretAndDeriveKeys(sikeSecret, ecdhSecret);

				// Submit a txn to prove authenticity for the remote end. We then reply with an encrypted txnID.
				// Because we assigned the node ID, we are certain it is them.

				// Unless we're in a trusted LAN setup, where we can assume the packets are not being manipulated
				// (but may be getting recorded, so we still do the full exchange but do not need to auth the other end).

				#warning ..todo!

				break;
			case 2:
				// Transaction
				break;
			case 3:
				// Guard
				break;
			case 4:
				// GuardState
				break;
			case 5:
				// Ping
				break;
		}

		// Release the buffers:
		buffer = first;

		while (buffer != null)
		{
			var next = buffer.After;
			buffer.Release();
			buffer = next;
		}
	}

	/// <summary>
	/// ProjectId to use when messaging the client.
	/// </summary>
	public ulong ProjectId;

	/// <summary>
	/// Sends the given writer contents and releases the writer.
	/// </summary>
	/// <param name="writer"></param>
	public bool SendAndRelease(Writer writer)
	{
		// The # of fragments is..
		var fragments = writer.BufferCount;

		// First, figure out the complete size of the Lumity header. It's the same for all fragments in the message.
		

		/*
		 
		// Flags - unencrypted:
		writer.Write((byte)0);

		// Compressed node ID; this is 0 throughout the handshake:
		writer.Write((byte)0);

		// The temporary ID:
		writer.WriteBE((ushort)remoteTempId);

		// Message type - ServerHello (1):
		writer.Write((byte)1);

		// Fragment count. This message is never fragmented. The payload is ~600 bytes which easily fits inside a packet.
		writer.Write((byte)1);
		
		*/



		// We only use one buffer from these writers.
		var buffer = writer.FirstBuffer as BlockBuffer;
		buffer.Length = writer.CurrentFill;
		buffer.Target = RemoteAddress;

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
				BlockBuffer frame = Server.FirstSendFrame;

				lock (Server.sendQueueLock)
				{
					Server.FirstSendFrame = null;
					Server.LastSendFrame = null;
				}

				BlockBuffer lastProcessed = null;

				while (frame != null)
				{
					await Server.ServerSocketUdp.SendToAsync(frame.Bytes.AsMemory(frame.Offset, frame.Length), SocketFlags.None, frame.Target);

					var next = frame.After as BlockBuffer;
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

		/*
		var frame = SendStackFrame.Get();
		frame.Writer = writer;
		frame.Current = null;

		var goImmediately = false;

		lock (this)
		{
			if (LastSendFrame == null)
			{
				FirstSendFrame = frame;
				LastSendFrame = frame;
				goImmediately = CanProcessSend;
			}
			else
			{
				LastSendFrame.After = frame;
				LastSendFrame = frame;
			}

			if (goImmediately)
			{
				// The queue is empty - Send immediately:
				CanProcessSend = false;
			}
		}

		if (goImmediately)
		{
			frame.Current = writer.FirstBuffer;

			var buffer = frame.Current;
			AsyncArgs.SetBuffer(buffer.Bytes, buffer.Offset, buffer.Length);

			if (!Server.ServerSocketUdp.SendToAsync(AsyncArgs))
			{
				// It completed immediately
				CompletedCurrentSend();
			}
		}

		return true;
		*/
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
