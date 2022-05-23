using Api.SocketServerLibrary;
using Api.SocketServerLibrary.Crypto;
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
	public Api.SocketServerLibrary.Ciphers.SrtpCipherCTR ReceiveCipherCtr;

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
	public Api.SocketServerLibrary.Ciphers.SrtpCipherCTR SendCipherCtr;
	
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
	/// Checks an RTP packet authentication tag (HMAC, SHA1)
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
		
		var cipherCtr = new Api.SocketServerLibrary.Ciphers.SrtpCipherCTR();
		var mac = new Api.SocketServerLibrary.Crypto.HMac(SHA1);
		var cipher = cipherCtr.Cipher;

		var masterKey = new byte[keyLen];

		for (var i = 0; i < keyLen; i++)
		{
			masterKey[i] = keyMaterial[offset + i];
		}

		var encKey = new byte[keyLen]; // AES-128 CM
		var saltKey = new byte[saltLen];
		
		// Send and receive key materials are different to avoid the
		// otherwise guaranteed reuse of a sequence number.
		if (isReceive)
		{
			ReceiveSaltKey = saltKey;
			ReceiveCipherCtr = cipherCtr;
			ReceiveMac = mac;
		}
		else
		{
			SendSaltKey = saltKey;
			SendCipherCtr = cipherCtr;
			SendMac = mac;
		}

		var authKey = new byte[20]; // SHA-256 based HMAC, 20 byte length key

		// compute the session encryption key

		Span<byte> ivStore = stackalloc byte[14]; // Temporary IV store.

		// Construct initial IV:
		offset += 16;
		ComputeIv(0, keyMaterial, offset, ref ivStore);

		// Initialise the cipher and using the above IV, generate the session encryption key.
		cipher.Init(true, masterKey);
		cipherCtr.GetCipherStream(encKey, 16, ivStore);

		// encKey now set.

		// Construct next IV. This one will be used for the authentication key. Generate the key too.
		ComputeIv(1, keyMaterial, offset, ref ivStore);
		cipherCtr.GetCipherStream(authKey, 20, ivStore);

		// authKey now set. Init the HMAC:
		mac.Init(authKey, 0, authKey.Length);

		// Finally, construct IV for the salt and generate the salt.
		ComputeIv(2, keyMaterial, offset, ref ivStore);
		cipherCtr.GetCipherStream(saltKey, 14, ivStore);
		
		// Initialize cipher with derived encryption key.
		cipher.Init(true, encKey);
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
					await Server.ServerSocketUdp.SendToAsync(frame.Bytes.AsMemory(0, frame.Length), SocketFlags.None, frame.Target);

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
