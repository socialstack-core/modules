using Api.SocketServerLibrary;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Org.BouncyCastle.X509;
using System;

namespace DTLS
{
	internal class DTLSContext : TlsContext
	{
		private class DTLSSecurityParameters : SecurityParameters
		{
			public Api.WebRTC.RtpClient Client;
			private byte[] _ClientRandom;
			private byte[] _ServerRandom;
			private int _PrfAlgorithm;

			public override int CipherSuite { get { return 0xC02B; } }
			public override byte[] ClientRandom { get { return _ClientRandom; } }
			public override byte[] MasterSecret { get { return Client.MasterSecret; } }
			public override int PrfAlgorithm { get { return _PrfAlgorithm; } }
			public override byte[] ServerRandom { get { return _ServerRandom; } }

			public DTLSSecurityParameters(Api.WebRTC.RtpClient client)
			{
				Client = client;
				_ClientRandom = new byte[32];
				_ServerRandom = new byte[32];

				Array.Copy(client.HandshakeMeta.DtlsRandom, 0, _ClientRandom, 0, 32);
				Array.Copy(client.HandshakeMeta.DtlsRandom, 32, _ServerRandom, 0, 32);

				_PrfAlgorithm = Org.BouncyCastle.Crypto.Tls.PrfAlgorithm.tls_prf_sha256;
			}
		}

		public ProtocolVersion ClientVersion { get; private set; }

		public byte[] ExportKeyingMaterial(string asciiLabel, byte[] context_value, int length)
		{
			throw new NotImplementedException();
		}

		public bool IsServer { get; private set; }

		public Org.BouncyCastle.Crypto.Prng.IRandomGenerator NonceRandomGenerator { get; private set; }

		public TlsSession ResumableSession
		{
			get { throw new NotImplementedException(); }
		}

		public Org.BouncyCastle.Security.SecureRandom SecureRandom
		{
			get;
			set;
		}

		public SecurityParameters SecurityParameters { get; private set; }

		public ProtocolVersion ServerVersion { get; private set; }

		public object UserObject
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public DTLSContext()
		{

		}

		public DTLSContext(Api.WebRTC.RtpClient clientR)
		{
			IsServer = true;
			ClientVersion = ProtocolVersion.DTLSv12;
			ServerVersion = ProtocolVersion.DTLSv12;
			SecurityParameters = new DTLSSecurityParameters(clientR);
			NonceRandomGenerator = new Org.BouncyCastle.Crypto.Prng.DigestRandomGenerator(TlsUtilities.CreateHash(HashAlgorithm.sha256));
			NonceRandomGenerator.AddSeedMaterial(new byte[8]);
		}
	}
}


namespace Api.WebRTC {



	/// <summary>
	/// Helpers for handling DTLS packets.
	/// </summary>
	public static class Dtls
	{
		// DTLS 1.2:
		// https://datatracker.ietf.org/doc/html/rfc6347

		// SRTP specific extension:
		// https://www.rfc-editor.org/rfc/rfc5764.html#section-5.1.2

		/// <summary>
		/// Displays a (D)TLS alert to the console.
		/// </summary>
		/// <param name="alertLevel"></param>
		/// <param name="description"></param>
		private static void DisplayAlert(byte alertLevel, byte description)
		{
			var message = "";

			switch (description)
			{
				case 0:
					// The other end simply wants to close the connection.
					// This alert is very common when e.g. a browser tab is closed but is also not something that can be completely relied on.
					message = "close_notify(0)";
				break;
				case 10:
					// The sequence numbers are wrong in some way or a message was sent out of order.
					message = "unexpected_message(10)";
					break;
				case 20:
					// Decrypt failure. Usually means the packet payload is wrong.
					message = "bad_record_mac(20)";
					break;
				case 21:
					message = "decryption_failed_RESERVED(21)";
					break;
				case 22:
					// (D)TLS record layer has incorrect length values in some way. Use something like wireshark to figure out what's up.
					message = "record_overflow(22)";
					break;
				case 30:
					message = "decompression_failure(30)";
					break;
				case 40:
					// Usually sent when a client has given up after timing out too many times.
					message = "handshake_failure(40)";
					break;
				case 41:
					message = "no_certificate_RESERVED(41)";
					break;
				case 42:
					message = "bad_certificate(42)";
					break;
				case 43:
					message = "unsupported_certificate(43)";
					break;
				case 44:
					message = "certificate_revoked(44)";
					break;
				case 45:
					message = "certificate_expired(45)";
					break;
				case 46:
					message = "certificate_unknown(46)";
					break;
				case 47:
					message = "illegal_parameter(47)";
					break;
				case 48:
					message = "unknown_ca(48)";
					break;
				case 49:
					message = "access_denied(49)";
					break;
				case 50:
					// There is an issue with the bytes of the packet. Use something like wireshark to figure out what's up.
					message = "decode_error(50)";
					break;
				case 51:
					// Either because the encryption was wrong, or because a signature did not verify as expected.
					message = "decrypt_error(51)";
					break;
				case 60:
					message = "export_restriction_RESERVED(60)";
					break;
				case 70:
					// DTLS 1.2+ only
					message = "protocol_version(70)";
					break;
				case 71:
					// Lacking suitable cipher suite to use.
					message = "insufficient_security(71)";
					break;
				case 80:
					message = "internal_error(80)";
					break;
				case 90:
					message = "user_canceled(90)";
					break;
				case 100:
					// A renegotiation was attempted but the other end did not allow it to go ahead.
					message = "no_renegotiation(100)";
					break;
				case 110:
					message = "unsupported_extension(110)";
				break;
			}

			if (alertLevel == 2)
			{
				System.Console.WriteLine("DTLS fatal alert received: " + message);
			}
			else
			{
				System.Console.WriteLine("DTLS alert received: " + message);
			}
		}

		private static byte[] AlertHeader = {
			21,  // Alert
			254, // v1.2
			253
		};

		private static byte[] HandshakeHeader = {
			22,  // Handshake
			254, // v1.2
			253,
			0,   // Epoch (2)
			0
			// Sequence, length etc follows
		};
		
		private static byte[] HandshakeHeaderEnc = {
			22,  // Handshake
			254, // v1.2
			253,
			0,   // Epoch (2)
			1
			// Sequence, length etc follows
		};
		
		private static byte[] ChangeCipherSpec = {
			20,  // ChangeCipherSpec
			254, // v1.2
			253,
			0,   // Epoch (2)
			0
		};

		private static byte[] ServerHelloExtensions = {
			0, // Length (2) - constant 24
			24,
			0,  // extended_master_secret (2)
			23,
			0, // extended_master_secret length (2) - constant 0
			0,
			255, // renegotiation_info (2)
			1,
			0, // renegotiation_info length (2) - constant 1
			1,
			0, // renegotiation_info body
			0, // ec_point_formats (2)
			11,
			0, // ec_point_formats length (2) - constant 2
			2,
			1, // ec_point_formats count
			0, // ec_point_format for uncompressed
			0, // use_srtp (2)
			14,
			0, // use_srtp length (2) - constant 5
			5,
			0, // SRTP protection profiles length (2)
			2,
			0, // SRTP protection profile for SRTP_AES128_CM_HMAC_SHA1_80 (2)
			1,
			0 // SRTP MKI length (1) - constant 0
		};

		private static byte[] CertificateRequestsBody = {
			2,  // Certificate types count (1)
			1,  // RSA signed
			64, // ECDSA signed
			0,  // Hash algorithms length (2) - constant 18
			18,
			4,  // ecdsa_secp256r1_sha256 (2)
			3,
			8,  // rsa_pss_rsae_sha256 (2)
			4,
			4,  // rsa_pkcs1_sha256 (2)
			1,
			5,  // ecdsa_secp384r1_sha384 (2)
			3,
			8,  // rsa_pss_rsae_sha384 (2)
			5,
			5,  // rsa_pkcs1_sha384 (2)
			1,
			8,  // rsa_pss_rsae_sha512 (2)
			6,
			6,  // rsa_pkcs1_sha512 (2)
			1,
			2,  // rsa_pkcs1_sha1 (2)
			1,
			0, // Distinguished names length (2)
			0
		};

		/// <summary>
		/// Sends a DTLS alert message to the given client. Uses v1.0 to ensure support.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="alertType"></param>
		/// <param name="description"></param>
		public static void SendAlert(RtpClient client, byte alertType, byte description)
		{
			var writer = client.Server.StartMessage(client);
			writer.WriteNoLength(AlertHeader);

			// Epoch:
			writer.WriteBE((client.Encryption != null) ? (ushort)1 : (ushort)0);

			// Sequence:
			writer.WriteUInt48BE(client.SendDtlsSequence);
			client.SendDtlsSequence++;
			
			// Length:
			writer.WriteBE((ushort)2);

			// TODO: Encrypted alerts. Need to be able to encrypt this msg if the client is in encrypted mode.

			// Payload:
			writer.Write(alertType);
			writer.Write(description);

			// Send it:
			client.Send(writer);

			writer.Release();
			client.CloseRequested();
		}

		/// <summary>
		/// DTLS packet main receive point.
		/// </summary>
		public static void HandleMessage(byte[] buffer, int index, int payloadSize, RtpClient client)
		{
			// DTLSPlaintext record:
			/*
			 struct {
				ContentType type; // 1 byte
				ProtocolVersion version; // 2 bytes
				uint16 epoch; // 2 bytes
				uint48 sequence_number; // 6 bytes
				uint16 length; // 2 bytes
				opaque fragment[DTLSPlaintext.length]; // length bytes
			  } DTLSPlaintext;
			*/

			var maxIndex = payloadSize + index;

			// Content type:

			while (index < maxIndex)
			{
				var contentType = buffer[index++] - 20;
				var length = (buffer[index + 10] << 8) | buffer[index + 11];

				if ((index + length + 12) > maxIndex)
				{
					// Invalid packet - length value is too big
					return;
				}

				// Discard if incorrect sequence number.
				// (may buffer if sequence is > but in practice this is extremely rare and forcing a retransmit is better on general resource consumption):
				var seq = (long)(buffer[index + 4] << 40 | buffer[index + 5] << 32 | buffer[index + 6] << 24 | buffer[index + 7] << 16 | buffer[index + 8] << 8 | buffer[index + 9]);
				var epoch = buffer[index + 2] << 8 | buffer[index + 3];

				// Drop retransmits and replays:
				if (seq != client.ReceiveDtlsSequence && client.ReceiveDtlsSequence != -1)
				{
					return;
				}

				// Next expected sequence number:
				client.ReceiveDtlsSequence = seq + 1;

				var msgBuffer = buffer;
				var msgLength = length;
				var msgIndex = index + 12;

				if (epoch != 0 && client.Encryption != null)
				{
					// Decrypt length bytes
					long sequenceEpoch = ((long)buffer[index + 2] << 56) | ((long)buffer[index + 3] << 48) | (long)seq;
					var decoded = client.Encryption.DecodeCiphertext(sequenceEpoch, (byte)(contentType + 20), buffer, index + 12, length);

					msgLength = decoded.Length;
					msgBuffer = decoded;
					msgIndex = 0;
				}

				switch (contentType)
				{
					case 0: // change_cipher_spec(20)

						// This is only permitted in one direction, from unencrypted -> encrypted.
						
						// Sequence numbers are reset when this is received:
						client.ReceiveDtlsSequence = -1;

						break;
					case 1: // alert(21)

						// 2 bytes - type and description
						if (length >= 2)
						{
							var alertLevel = msgBuffer[msgIndex++];
							var alertDescription = msgBuffer[msgIndex];

							if (alertDescription != 0)
							{
								// It's just a close notify otherwise.
								DisplayAlert(alertLevel, alertDescription);
							}

							// All alerts close the connection:
							client.CloseRequested();
						}
						break;
					case 2: // handshake(22)

						// Handshakes start with DTLS 1.0 but are then elevated to 1.2
						HandleHandshake(msgBuffer, msgLength, client, msgIndex);

					break;
					// We don't actually use application_data(3) here.
					// Instead, a dedicated encrypted RTP message is sent.
				}

				index += 12 + length; // Length is of the record body minus its header (12 bytes)
			}
		}

		/// <summary>
		/// Sends the ChangeCipherSpec and encrypted Finish message.
		/// </summary>
		/// <param name="client"></param>
		public static void SendChangeCipherFinish(RtpClient client)
		{
			// - start of ChangeCipherSpec record -
			var writer = client.Server.StartMessage(client);
			writer.WriteNoLength(ChangeCipherSpec);

			// Sequence:
			writer.WriteUInt48BE(client.SendDtlsSequence);
			client.SendDtlsSequence++;

			// Length:
			writer.WriteBE((ushort)1);

			// Payload (a constant 1):
			writer.Write((byte)1);

			// Note the CCS is not included in the hash as it isn't a handshake record.

			// CCS resets the DTLS sequence number:
			client.SendDtlsSequence = 0;

			// - end of ChangeCipherSpec record -

			// Double buffer the encrypted outbound packets:
			var encWriter = Writer.GetPooled();
			encWriter.Start(null);
			encWriter.Write((byte)20); // Handshake type (20 - finished)
			encWriter.WriteUInt24BE(0); // Length (constant)
			encWriter.WriteBE(client.HandshakeMeta.SendHandshakeSequence++); // Handshake sequence
			encWriter.WriteUInt24BE(0); // Fragment offset
			encWriter.WriteUInt24BE(0); // Fragment length (constant)

			// Write the verify:
			var verify = TlsUtilities.PRF(new DTLS.DTLSContext(client), client.MasterSecret, "server finished", client.HandshakeMeta.GetSessionHash(), 12);

			encWriter.WriteNoLength(verify);

			// Update lengths:
			encWriter.FirstBuffer.Bytes[3] = (byte)verify.Length;
			encWriter.FirstBuffer.Bytes[11] = (byte)verify.Length;

			// Sequence and epoch is always a constant (the 1<<48) here as we're right after the change cipher spec:
			var ciphertext = client.Encryption.EncodePlaintext(((long)1) << 48, 22, encWriter.FirstBuffer.Bytes, 0, encWriter.Length);
			encWriter.Release();

			writer.WriteNoLength(HandshakeHeaderEnc);
			writer.WriteUInt48BE(client.SendDtlsSequence); // Sequence
			client.SendDtlsSequence++;
			writer.WriteBE((ushort)ciphertext.Length); // Length
			writer.Write(ciphertext,0, ciphertext.Length);

			client.Send(writer);
			writer.Release();

			// Handshake is done:
			client.HandshakeMeta = null;
		}

		/// <summary>
		/// Sends a server hello message to the given client.
		/// </summary>
		/// <param name="client"></param>
		public static void SendServerHello(RtpClient client)
		{
			// - start of ServerHello record -
			var writer = client.Server.StartMessage(client);
			var startIndex = writer.Length;

			writer.WriteNoLength(HandshakeHeader); // HandshakeHeader
			writer.WriteUInt48BE(client.SendDtlsSequence); // Sequence
			client.SendDtlsSequence++;

			writer.WriteBE((ushort)108); // Length (constant)

			writer.Write((byte)2); // Handshake type (2 - serverHello)
			writer.WriteUInt24BE(96); // Length (constant)
			writer.WriteBE(client.HandshakeMeta.SendHandshakeSequence++); // Handshake sequence (handshake sequence has a slightly different meaning)
			writer.WriteUInt24BE(0); // Fragment offset
			writer.WriteUInt24BE(96); // Fragment length (constant)

			// ServerHello content next.
			writer.Write((byte)254); // DTLS version - 1.2
			writer.Write((byte)253);

			// Generate random bytes - this includes the args used for the key exchange as well:
			client.HandshakeMeta.PopulateDtlsServerRandom();

			// Send server random:
			writer.Write(client.HandshakeMeta.DtlsRandom, 32, 32);

			// SessionID length (32):
			writer.Write((byte)32);

			// Send session ID:
			writer.Write(client.HandshakeMeta.DtlsRandom, 64, 32);

			// Selected cipher suite:
			writer.WriteBE((ushort)client.DtlsCipher.TlsId);

			// Compression mode:
			writer.Write((byte)0);

			// Extensions (always constant, 2 byte length followed by 24 bytes):
			writer.Write(ServerHelloExtensions, 0, 26);

			// - End of ServerHello record -
			// - Start of Certificate record -

			// Get shared certificate for the cipher:
			var certificate = client.DtlsCipher.GetCertificate(client);
			var certificatePayload = certificate.CertificatePayload;
			var certLength = (uint)certificatePayload.Length;

			writer.WriteNoLength(HandshakeHeader);
			writer.WriteUInt48BE(client.SendDtlsSequence); // Sequence
			client.SendDtlsSequence++;
			
			writer.WriteBE((ushort)(certLength + 18)); // Length

			writer.Write((byte)11); // Handshake type (11 - certificate)
			writer.WriteUInt24BE(certLength + 6); // Length
			writer.WriteBE(client.HandshakeMeta.SendHandshakeSequence++); // Handshake sequence
			writer.WriteUInt24BE(0); // Fragment offset
			writer.WriteUInt24BE(certLength + 6); // Fragment length

			// Certificate content next.
			writer.WriteUInt24BE(certLength + 3); // Can never have too many lengths
			writer.WriteUInt24BE(certLength); // All the lengths
			writer.Write(certificatePayload, 0, certificatePayload.Length);

			// - End of Certificate record -
			// - Start of ServerKeyExchange record -

			// server_key_exchange is optional - if the cipher doesn't need it, it's omitted.
			// Note that every cipher we support and all of the future ones will have these args.
			if (client.DtlsCipher.HasKeyExchangeArgs)
			{
				var buff = writer.LastBuffer.Bytes;

				writer.WriteNoLength(HandshakeHeader);
				writer.WriteUInt48BE(client.SendDtlsSequence); // Sequence
				client.SendDtlsSequence++;

				writer.WriteBE((ushort)0); // Length + 12 - set after
				var offset = writer.CurrentFill - 2;

				writer.Write((byte)12); // Handshake type (12 - server key exchange)
				writer.WriteUInt24BE(0); // Length - set after
				writer.WriteBE(client.HandshakeMeta.SendHandshakeSequence++); // Handshake sequence
				writer.WriteUInt24BE(0); // Fragment offset
				writer.WriteUInt24BE(0); // Fragment length - set after

				// Key exchange args content next.

				// This will generally derive a public key from random data that was generated earlier.
				var currentSize = writer.Length;
				client.DtlsCipher.KeyExchange.WriteKeyExchangeArgs(writer, client, certificate);
				var keyExchangeLength = writer.Length - currentSize;

				var kE12 = keyExchangeLength + 12;

				buff[offset] = (byte)(kE12 >> 8);
				buff[offset + 1] = (byte)kE12;

				buff[offset + 4] = (byte)(keyExchangeLength >> 8);
				buff[offset + 5] = (byte)keyExchangeLength;

				buff[offset + 12] = (byte)(keyExchangeLength >> 8);
				buff[offset + 13] = (byte)keyExchangeLength;
			}

			// - End of ServerKeyExchange record -
			// - Start of CertificateRequest record -

			writer.WriteNoLength(HandshakeHeader);
			writer.WriteUInt48BE(client.SendDtlsSequence); // Sequence
			client.SendDtlsSequence++;
			writer.WriteBE((ushort)37); // Length

			writer.Write((byte)13); // Handshake type (13 - certificate request)
			writer.WriteUInt24BE(25); // Length
			writer.WriteBE(client.HandshakeMeta.SendHandshakeSequence++); // Handshake sequence
			writer.WriteUInt24BE(0); // Fragment offset
			writer.WriteUInt24BE(25); // Fragment length

			// certificate request payload
			writer.Write(CertificateRequestsBody, 0, 25);

			// - End of CertificateRequest record -
			// - Start of ServerHelloDone record -

			writer.WriteNoLength(HandshakeHeader);
			writer.WriteUInt48BE(client.SendDtlsSequence); // Sequence
			client.SendDtlsSequence++;
			writer.WriteBE((ushort)12); // Length

			writer.Write((byte)14); // Handshake type (14 - server hello done)
			writer.WriteUInt24BE(0); // Length
			writer.WriteBE(client.HandshakeMeta.SendHandshakeSequence++); // Handshake sequence
			writer.WriteUInt24BE(0); // Fragment offset
			writer.WriteUInt24BE(0); // Fragment length

			// - End of ServerHelloDone record -

			// Note: Has a side effect of updating the last buffers length which is used by the verify digest in a moment.
			client.Send(writer);

			// Handshake verify digest update:
			if (client.HandshakeMeta.HandshakeVerifyDigest != null)
			{
				// Scan along the writer and add each handshake message (not the whole DTLS record - just the message, incl type/length/seq etc).
				var index = startIndex;
				var buffer = writer.FirstBuffer;

				while (buffer != null && index < buffer.Length)
				{
					index += 11; // Skip record type, sequence etc.

					// Message length:
					var messageLength = (buffer.Bytes[index] << 8) | buffer.Bytes[index + 1];
					index += 2;

					client.HandshakeMeta.HandshakeVerifyDigest.BlockUpdate(buffer.Bytes, index, messageLength);

					index += messageLength;

					if (index >= buffer.Length)
					{
						buffer = buffer.After;
						if (buffer != null)
						{
							index -= buffer.Length;
						}
					}
				}
			}

			writer.Release();
		}

		private static void HandleHandshake(byte[] buffer, int length, RtpClient client, int index)
		{
			if (client.HandshakeMeta == null)
			{
				// Renegotiation is not supported (it's also depreciated in TLS 1.3 anyway).
				return;
			}

			var sequence = (buffer[index + 4] << 8) | buffer[index + 5];

			// Replay prevention:
			if (sequence != client.HandshakeMeta.ReceiveHandshakeSequence && client.HandshakeMeta.ReceiveHandshakeSequence != -1)
			{
				// Replay - drop the message.
				return;
			}

			client.HandshakeMeta.ReceiveHandshakeSequence = sequence + 1;

			/*
			enum {
			 hello_request(0), client_hello(1), server_hello(2),
			 hello_verify_request(3),                          // New field
			 certificate(11), server_key_exchange (12),
			 certificate_request(13), server_hello_done(14),
			 certificate_verify(15), client_key_exchange(16),
			 finished(20), (255) } HandshakeType;

		   struct {
			 HandshakeType msg_type;
			 uint24 length;
			 uint16 message_seq;
			 uint24 fragment_offset;
			 uint24 fragment_length;
			 select (HandshakeType) {
			   case hello_request: HelloRequest;
			   case client_hello:  ClientHello;
			   case server_hello:  ServerHello;
			   case hello_verify_request: HelloVerifyRequest;
			   case certificate:Certificate;
			   case server_key_exchange: ServerKeyExchange;
			   case certificate_request: CertificateRequest;
			   case server_hello_done:ServerHelloDone;
			   case certificate_verify:  CertificateVerify;
			   case client_key_exchange: ClientKeyExchange;
			   case finished: Finished;
			 } body; } Handshake;
			*/

			// If the fragment_offset != length, this message is fragmented. Must buffer it up until all is received.
			var dataLength = buffer[index + 1] << 16 | buffer[index + 2] << 8 | buffer[index + 3];
			var fragLength = buffer[index + 9] << 16 | buffer[index + 10] << 8 | buffer[index + 11];
			Writer fromHSBuffer = null;

			var handshakeType = buffer[index];
			var startIndex = index;

			// A hash of the handshake messages is used twice - once to verify the clients ownership of the cert private key
			// and also in the finished message as a final proof that the shared key matches and is based on this handshake.
			// So, to avoid storing all the handshake messages which could be easily abused, we run a rolling hash.
			if (client.HandshakeMeta.HandshakeVerifyDigest != null && handshakeType != 15 && handshakeType != 20)
			{
				// Add message bytes to the rolling hash:
				client.HandshakeMeta.HandshakeVerifyDigest.BlockUpdate(buffer, startIndex, length);
			}
			
			if (fragLength != dataLength)
			{
				// Read offset:
				var fragOffset = buffer[index + 6] << 16 | buffer[index + 7] << 8 | buffer[index + 8];

				// Store the fragment in the client handshake buffer.
				var hsBuffer = client.HandshakeMeta.HandshakeBuffer;
				if (hsBuffer == null) {
					hsBuffer = Writer.GetPooled();
					client.HandshakeMeta.HandshakeBuffer = hsBuffer;
					hsBuffer.Start(null);
				}

				hsBuffer.Write(buffer, index, dataLength + 12);

				// Was this the last fragment?
				if ((fragOffset + fragLength) == dataLength)
				{
					buffer = client.HandshakeMeta.HandshakeBuffer.FirstBuffer.Bytes;
					index = 0;
					fromHSBuffer = client.HandshakeMeta.HandshakeBuffer;
					client.HandshakeMeta.HandshakeBuffer = null;
				}
				else
				{
					return;
				}
			}

			// Seek over data/frag length:
			index += 12;

			switch (handshakeType) {
				case 1:
					// client_hello(1)

					// Clients version MUST be 1.2+ or we reject
					if (buffer[index] == 254 && buffer[index + 1] <= 253)
					{
						// Ok
						index += 2;

						// Read 32 random bytes:
						for (var i = 0; i < 32; i++)
						{
							client.HandshakeMeta.DtlsRandom[i] = buffer[index++];
						}

						// Ignore session ID + cookie. Each has a length followed by n bytes (usually 0 here though).
						var skipLength = buffer[index++];
						index += skipLength;

						skipLength = buffer[index++];
						index += skipLength;

						// Cipher suite size in bytes:
						var cipherSuiteSize = (buffer[index++] << 8 | buffer[index++]);

						Ciphers.Cipher favouriteCipher = null;
						Ciphers.CurveInfo favouriteCurve = null;

						for (var i = 0; i < cipherSuiteSize; i += 2)
						{
							var cipherId = (ushort)(buffer[index++] << 8 | buffer[index++]);

							var cipher = Ciphers.CipherLookup.GetCipher(cipherId);

							if (cipher != null)
							{
								if (favouriteCipher == null || cipher.Priority < favouriteCipher.Priority)
								{
									favouriteCipher = cipher;
								}
							}
						}

						client.DtlsCipher = favouriteCipher;

						if (favouriteCipher == null)
						{
							// Send a fatal alert - insufficient security. We don't have a cipher that the client supports.
							SendAlert(client, 2, 71);
							return;
						}
						
						// Compression methods next, which we will just ignore.
						var methodSize = buffer[index++];
						index += methodSize;

						// Extensions block. We're mainly after supported_groups to know what curves we can use (if any) during the exchange.
						var extensionMax = (buffer[index++] << 8 | buffer[index++]);
						extensionMax += index;

						var extendedMasterRequired = client.Server.ExtendedMasterSecret;
						var clientSupportsEMS = false;

						while (index < extensionMax)
						{
							var extType = (buffer[index++] << 8 | buffer[index++]);
							var extLength = (buffer[index++] << 8 | buffer[index++]);

							var localExtIndex = index;
							var localExtMax = extLength + index;

							if (extType == 0x0d)
							{
								// signature_algorithms (curves)
								if (favouriteCipher.KeyExchange.HasCurves)
								{
									// ECDSA
									while (localExtIndex < localExtMax)
									{
										var algoId = (ushort)(buffer[localExtIndex++] << 8 | buffer[localExtIndex++]);

										var curve = Ciphers.CurveLookup.GetCurveBySignatureAlgorithm(algoId);

										if (curve != null)
										{
											if (favouriteCurve == null || curve.Priority < favouriteCurve.Priority)
											{
												favouriteCurve = curve;
											}
										}
									}
								}
							}
							else if (extType == 0x17)
							{
								// Extended master secret.
								// If the client supports it, we must use it here.
								clientSupportsEMS = true;
							}

							index += extLength;
						}

						if (favouriteCipher.KeyExchange.HasCurves)
						{
							if (favouriteCurve == null)
							{
								// No suitable curve available.
								SendAlert(client, 2, 71);
								return;
							}

							// Apply the curve info:
							favouriteCipher = favouriteCipher.GetCurveVariant(favouriteCurve);
							client.DtlsCipher = favouriteCipher;
						}

						if (clientSupportsEMS != extendedMasterRequired)
						{
							SendAlert(client, 2, 40);
							return;
						}

						// The extensions will include use_srtp which itself just declares the actual encryption ciphers to use for the SRTP packets.
						
						// Now we'll respond with a serverHello with the cipher we have just selected.
						// Note that we respond with a few other pieces at the same time, including our certificate amongst other things.
						SendServerHello(client);
					}
					else
					{
						// Send a fatal alert - 1.0 not permitted here
						SendAlert(client, 2, 70);
						return;
					}

					break;
				case 11:
					// certificate(11)

					// Certs size, followed by single cert size
					// Only ever 1 cert that we need to care about though, so skip first 3 bytes and read the length:
					index += 3;
					var certSize = (buffer[index++] << 24 | buffer[index++] << 8 | buffer[index++]);

					client.HandshakeMeta.RemotePublicCertKey = null;
					client.MasterSecret = null;

					if (certSize < dataLength && (certSize + index) < buffer.Length)
					{
						// index -> index+certSize
						// First, check this cert is OK. We'll need to get its public key in order to verify with it.
						var certBytes = new byte[certSize];
						System.Array.Copy(buffer, index, certBytes, 0, certSize);

						try
						{
							var cert = new X509CertificateParser().ReadCertificate(certBytes);

							var pubKey = cert.GetPublicKey();

							if (cert.IsValid(System.DateTime.UtcNow))
							{
								cert.Verify(pubKey);
							}

							System.Console.WriteLine("Received a valid certificate. Ownership of it has not yet been confirmed.");
							client.HandshakeMeta.RemotePublicCertKey = pubKey;
						}
						catch (Exception e)
						{
							System.Console.WriteLine("Received an invalid certificate: " + e.ToString());

							// Halt the exchange and send an alert of an invalid cert.
							SendAlert(client, 2, 42);
							client.CloseRequested();
							return;
						}
					}
					break;
				case 16:
					// client_key_exchange(16)
					var pkLength = buffer[index++];
					
					var preMasterSecret = client.DtlsCipher.KeyExchange.CalculateAgreement(client, buffer, index, pkLength);

					// Console.WriteLine("pre-master-secret: " + Hex.Convert(secret));

					// Derive the master secret now.
					if (client.Server.ExtendedMasterSecret)
					{
						// Extended master secret mode, where it is derived based on the session hash.
						var sessionHash = client.HandshakeMeta.GetSessionHash();
						client.MasterSecret = Ciphers.HashAlgorithm.PRF(preMasterSecret, "extended master secret", sessionHash, 48);
					}
					else
					{
						// Barely used - the original master secret mode, where it is based on the random values from client and server.
						var seed = new byte[64];
						Array.Copy(client.HandshakeMeta.DtlsRandom, 0, seed, 0, 64);
						client.MasterSecret = Ciphers.HashAlgorithm.PRF(preMasterSecret, "master secret", seed, 48);
					}

					// Console.WriteLine("Master: " + Hex.Convert(client.MasterSecret));

					client.Encryption = client.DtlsCipher.Encryption.SetupCipher(client);
					
					// Setup the SRTP keys now too:
					client.DeriveSrtpKeys();

					return;
				case 15:
					// certificate_verify(15)
					
					// First this message is only valid IF the cert key and the client key exchange has happened.
					// That's to avoid someone simply dodging this verification from applying to the key exchange.
					if (client.HandshakeMeta.RemotePublicCertKey != null && client.MasterSecret != null)
					{
						// Signature in here is from SIGN(SHA256(handshake_messages)) where:
						// - Handshake messages are all handshake records sent/ received so far by the client, excluding this verify one itself
						// - The private key used is the one in the clients certificate
						// - We therefore verify it using the certificate public key. A rolling hash is used to avoid storing all the handshake messages.
						var hash = client.HandshakeMeta.GetSessionHash();

						var _verifier = new ECDsaSigner();
						_verifier.Init(false, client.HandshakeMeta.RemotePublicCertKey);

						// Length of the signature:
						var sigLength = (buffer[index + 2] << 8) | buffer[index + 3];

						if (sigLength < dataLength)
						{
							index += 4; // Skip over sig algo and also sig size.

							var rLength = (int)buffer[index]; // first byte contains length of r array
							var r = new BigInteger(1, buffer, index + 1, rLength);
							var s = new BigInteger(1, buffer, index + rLength + 1, sigLength - (rLength + 1));

							// Console.WriteLine("Signature length was " + sigLength);

							var signatureVerify = _verifier.VerifySignature(hash, r, s);

							if (!signatureVerify)
							{
								Console.WriteLine("DTLS verification signature failed (ignoring for now, but must be handled correctly).");
							}

							// Now add this packet to the verify digest as it's used in the finish message:
							client.HandshakeMeta.HandshakeVerifyDigest.BlockUpdate(buffer, startIndex, length);
						}
					}

					break;
				case 20:
					System.Console.WriteLine("Received a handshake finished message");

					// The payload must verify:

					var verify = TlsUtilities.PRF(new DTLS.DTLSContext(client), client.MasterSecret, "client finished", client.HandshakeMeta.GetSessionHash(), 12);

					// Comparing verify with the contents of buffer.

					var verified = true;

					if (verify.Length != dataLength)
					{
						verified = false;
					}
					else
					{
						for (var i = 0; i < dataLength; i++)
						{
							if (buffer[index + i] != verify[i])
							{
								verified = false;
								break;
							}
						}
					}

					if (!verified)
					{
						SendAlert(client, 2, 40); // Handshake failed - unable to verify the finished record received from the client.
						return;
					}

					// Update the verify digest as we'll be using this message in our own finished as well:
					client.HandshakeMeta.HandshakeVerifyDigest.BlockUpdate(buffer, startIndex, length);

					Console.WriteLine("Sending finish..");
					SendChangeCipherFinish(client);

					// Ok! This client is now properly with us:
					client.Server.OnReadyBase(client);

				break;

					/*
					case 0:
						// hello_request(0)
					break;
					case 2:
						// server_hello(2)
					break;
					case 3:
						// hello_verify_request(3)
					break;
					case 12:
						// server_key_exchange (12)
					break;
				case 13:
					// certificate_request(13)
				break;
					case 14:
						// server_hello_done(14)
					break;
					*/
			}

			if (fromHSBuffer != null)
			{
				fromHSBuffer.Release();
			}

		}


	}
}