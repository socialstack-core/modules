using Api.SocketServerLibrary;
using System;
using System.Net;
using System.Text;

namespace Api.WebRTC;


/// <summary>
/// Implementation of the STUN protocol with non-allocating message handling.
/// </summary>
public static class StunServer
{
	/// <summary>
	/// STUN success message header
	/// </summary>
	private static byte[] StunResponseHeader = new byte[4] { 1, 1, 0, 0 };

	/// <summary>
	/// Handles a STUN message in the given buffer, but only for offered connections.
	/// Builds the reply and sends it to them too. Returns 0 if there was a failure, 1=ok, 2=ok (first time for this client).
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="index"></param>
	/// <param name="payloadSize"></param>
	/// <param name="port"></param>
	/// <param name="addressBytes"></param>
	/// <param name="ipv4"></param>
	/// <param name="server"></param>
	/// <param name="client">The identified client</param>
	public static int HandleMessage<T>(byte[] buffer, int index, int payloadSize, ushort port, ref Span<byte> addressBytes, bool ipv4, WebRTCServer<T> server,ref T client)
		where T : RtpClient, new()
	{
		var response = server.StartMessage(port, ref addressBytes, true);
		var responseStartIndex = response.Length;
		var startIndex = index;
		var maxIndex = payloadSize + index;
		index += 2; // Skip
		var firstTime = false;

		var length = buffer[index++] << 8 | buffer[index++];

		response.WriteNoLength(StunResponseHeader);

		// 4 byte magic cookie (a constant, used to identify if this udp packet is STUN) and 12 byte transaction ID
		// var transactionId = buffer, index, 12;

		// Copy the cookie and txId into the reply:
		response.Write(buffer, index, 16);
		index += 16;

		if ((maxIndex - index) != length)
		{
			Console.WriteLine("Invalid STUN binding request received (incorrect length)");
			response.Release();
			return 0;
		}

		// Read attributes next.
		int paddingOffset;
		uint fingerprint;
		int userStart = 0;
		int userLength = 0;

		client = server.GetClient(port, ref addressBytes);

		while (index < maxIndex)
		{
			var attributeType = buffer[index++] << 8 | buffer[index++];
			var attributeLength = buffer[index++] << 8 | buffer[index++];

			if ((maxIndex - index) < attributeLength)
			{
				// Not enough bytes
				Console.WriteLine("Invalid STUN binding request received (incorrect attribute length)");
				response.Release();
				return 0;
			}

			switch (attributeType)
			{
				case 0x0006: // USERNAME
					userStart = index;
					userLength = attributeLength;

					index += attributeLength;

					// STUN is 32 bit aligned (rfc5389#section-15). Skip padding bytes:
					paddingOffset = attributeLength % 4;

					if (paddingOffset != 0)
					{
						index += 4 - paddingOffset;
					}

					// Using this, we relate the sender ip/port with an offer.

					if (client == null)
					{
						client = server.GetClient(buffer, userStart, userLength);

						if (client != null)
						{
							Console.WriteLine("Looked up a client by username successfully.");

							// First time we've seen this client send something. Adding their address to the lookup.
							client.SetAddressAs(port, ref addressBytes, ipv4);
							server.AddClientByAddress(client);
							firstTime = true;
						}
					}

					break;
				case 0x0008: // MESSAGE-INTEGRITY (a HMAC of buffer[0 -> index-4], always 20 long)

					if (client == null)
					{
						Console.WriteLine("STUN failed: unknown client");
						return 0;
					}

					// Required to validate this, and also required to generate one in our response
					if (!StunHmac.CheckIntegrity(buffer, startIndex, index - 4, index, client))
					{
						return 0;
					}

					index += attributeLength;

					break;
				case 0x0009: // ERROR-CODE

					// We sent a client a bad STUN message, whoops, sorry!

					index += attributeLength;

					// STUN is 32 bit aligned (rfc5389#section-15). Skip padding bytes:
					paddingOffset = attributeLength % 4;

					if (paddingOffset != 0)
					{
						index += 4 - paddingOffset;
					}
					
					break;
				case 0x000A: // UNKNOWN-ATTRIBUTES

					// We sent a client an attribute it doesn't recognise, whoops, sorry!

					index += attributeLength;

					// STUN is 32 bit aligned (rfc5389#section-15). Skip padding bytes:
					paddingOffset = attributeLength % 4;

					if (paddingOffset != 0)
					{
						index += 4 - paddingOffset;
					}
					
					break;

				/*
				 *  Addresses are given in replies only.
					We don't need to handle receiving them.

					case 0x0001: // MAPPED-ADDRESS

					index++;

					// Family
					family = buffer[index++];
					port = buffer[index++] << 8 | buffer[index++];

					if (family == 1)
					{
						var ipv4 = (uint)buffer[index++] << 24 | (uint)buffer[index++] << 16 | (uint)buffer[index++] << 8 | (uint)buffer[index++];
						Console.WriteLine("family " + family + ", port " + port + ", ipv4 " + ipv4);
					}
					else if (family == 2)
					{
						var ipv6hi = ((ulong)buffer[index++] << 56) | ((ulong)buffer[index++] << 48) | ((ulong)buffer[index++] << 40) | ((ulong)buffer[index++] << 32) |
							((ulong)buffer[index++] << 24) | ((ulong)buffer[index++] << 16) | ((ulong)buffer[index++] << 8) | (ulong)buffer[index++];
						var ipv6lo = ((ulong)buffer[index++] << 56) | ((ulong)buffer[index++] << 48) | ((ulong)buffer[index++] << 40) | ((ulong)buffer[index++] << 32) |
							((ulong)buffer[index++] << 24) | ((ulong)buffer[index++] << 16) | ((ulong)buffer[index++] << 8) | (ulong)buffer[index++];
						Console.WriteLine("family " + family + ", port " + port + ", ipv6 " + ipv6hi);
					}

					index += attributeLength;

				break;
				case 0x0020: // XOR-MAPPED-ADDRESS
					index++;

					// Family
					family = buffer[index++];
					port = buffer[index++] << 8 | buffer[index++];

					if (family == 1)
					{
						var ipv4 = (uint)buffer[index++] << 24 | (uint)buffer[index++] << 16 | (uint)buffer[index++] << 8 | (uint)buffer[index++];
					}
					else if (family == 2)
					{
						var ipv6hi = ((ulong)buffer[index++] << 56) | ((ulong)buffer[index++] << 48) | ((ulong)buffer[index++] << 40) | ((ulong)buffer[index++] << 32) |
							((ulong)buffer[index++] << 24) | ((ulong)buffer[index++] << 16) | ((ulong)buffer[index++] << 8) | (ulong)buffer[index++];
						var ipv6lo = ((ulong)buffer[index++] << 56) | ((ulong)buffer[index++] << 48) | ((ulong)buffer[index++] << 40) | ((ulong)buffer[index++] << 32) |
							((ulong)buffer[index++] << 24) | ((ulong)buffer[index++] << 16) | ((ulong)buffer[index++] << 8) | (ulong)buffer[index++];
					}
				break;
				*/
				case 0x8028: // FINGERPRINT
							 // CRC32 of buffer[0 -> index-4], then xored with 0x5354554e

					var calculatedPrint = StunCrc32.CalcFingerprint(buffer, startIndex, index - 4);

					fingerprint = (uint)buffer[index++] << 24 | (uint)buffer[index++] << 16 | (uint)buffer[index++] << 8 | (uint)buffer[index++];

					if (calculatedPrint != fingerprint)
					{
						return 0;
					}

					break;

				case 0x0024: // ICE PRIORITY (rfc5245#section-19.1)

					// 4 byte priority, calculated by the client, to state what it thinks about this particular candidate (port).
					// We only give 1 candidate though, so the priority goes unused.
					index += attributeLength;

				break;
				case 0x0025: // ICE USE-CANDIDATE (rfc5245#section-19.1)

					// Just a flag to say the port and ip to use are the ones that this message originated from.

				break;
				case 0x8029: // ICE ICE-CONTROLLED (rfc5245#section-19.1, the number isn't actually in there though! https://www.iana.org/assignments/stun-parameters/stun-parameters.xhtml)

					// This should never appear, because we declare ice-lite and are therefore expected to be controlled always.
					// The client *could* also be ice-lite (although this will never happen in a standards compliant browser).
					// If it was though, the body of this message, an 8 byte number, is used to establish who "wins" the control position.
					index += attributeLength;

				break;
				case 0x802A: // ICE ICE-CONTROLLING (rfc5245#section-19.1, the number isn't actually in there though! https://www.iana.org/assignments/stun-parameters/stun-parameters.xhtml)

					// We should always get this one as browsers use full ICE and we are ice-lite.
					// The body is an 8 byte number.
					index += attributeLength;

				break;
				case 0x0014: // REALM (domain name essentially - we don't use this)
				case 0x0015: // NONCE (if one was sent, we don't reply with it unless there was an error)
				default:
					// Unknown or a value we don't need to do anything with - skip.
					index += attributeLength;

					paddingOffset = attributeLength % 4;

					if (paddingOffset != 0)
					{
						index += 4 - paddingOffset;
					}
				break;
			}
		}

		if (client == null)
		{
			Console.WriteLine("Cannot reply to STUN message: unknown client");
			response.Release();
			return 0;
		}
		
		// Response MUST end with the message integrity attribute and the fingerprint.
		var firstBuffer = response.FirstBuffer.Bytes;
		var addressSize = ipv4 ? 4 : 16;

		// Write the clients address:
		response.WriteBE((ushort)1); // Mapped-Address (type)
		response.WriteBE((ushort)(addressSize == 4 ? 8 : 20)); // Mapped-Address (length)

		response.Write((byte)0);
		response.Write((byte)(addressSize == 4 ? 1 : 2)); // Family
		response.WriteBE(port);

		response.Write(addressBytes, 0, addressSize);

		// XOR-Mapped-Address next
		response.WriteBE((ushort)0x0020); // XOR-Mapped-Address (type)
		response.WriteBE((ushort)(addressSize == 4 ? 8 : 20)); // XOR-Mapped-Address (length)

		response.Write((byte)0);
		response.Write((byte)(addressSize == 4 ? 1 : 2)); // Family
		response.WriteBE((ushort)(port ^ 0x2112));

		// XOR the addressBytes with the magic cookie, and potentially also the txID after it.
		// The cookie and the txID are conveniently already in the correct order in the output, starting at byte offset #4.
		for (var i = 0; i < addressSize; i++)
		{
			addressBytes[i] ^= firstBuffer[responseStartIndex + 4 + i];
		}

		response.Write(addressBytes, 0, addressSize);

		// Integrity:
		response.WriteBE((ushort)8); // Message-Integrity (type)
		response.WriteBE((ushort)20); // Message-Integrity (length)
		StunHmac.CalcIntegrity(response, responseStartIndex, response.Length-4, client); // Calcs integrity up to given limit (length-4), and writes it into the response, increasing its length by 20.

		// Calculate length header *before* fingerprint. It is the message length minus header, plus 8 bytes for fingerprint:
		var msgLength = response.Length - 20 + 8 - responseStartIndex;
		firstBuffer[responseStartIndex + 2] = (byte)(msgLength >> 8);
		firstBuffer[responseStartIndex + 3] = (byte)msgLength;

		// Fingerprint next:
		fingerprint = StunCrc32.CalcFingerprint(firstBuffer, responseStartIndex, response.Length);
		response.WriteBE((ushort)0x8028); // Fingerprint (type)
		response.WriteBE((ushort)4); // Fingerprint (length)
		response.WriteBE(fingerprint); // Fingerprint (value)

		// Send response to client:
		client.Send(response);
		response.Release();

		return firstTime ? 2 : 1;
	}
}

/// <summary>
/// HMAC calculator for STUN services
/// </summary>
public static class StunHmac
{
	/// <summary>
	/// Calcs message-integrity for the given writer, from 0 to max. The result goes into the writer at its current position.
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="client"></param>
	public static void CalcIntegrity(Writer writer, int min, int max, RtpClient client)
	{
		// Hmac from 0 -> max
		// Result goes into the writer
		Span<byte> hmac = stackalloc byte[20];
		ComputeHmac(writer.FirstBuffer.Bytes, min, max, client, ref hmac);
		writer.Write(hmac, 0, 20);
	}

	private static void ComputeHmac(byte[] ofArray, int start, int end, RtpClient client, ref Span<byte> target)
	{
		// First, must adjust the length bytes ([2] and [3]) to be equiv of end + the Message-Integrity attribute (and nothing after it).
		// rfc5389#section-15.4
		var origLength2 = ofArray[start+2];
		var origLength3 = ofArray[start + 3];

		var endOfHmacZone = end + 24 - 20 - start; // End is just before the Message-Integrity attribute. The length includes the message-integrity attrib (24 bytes) but not the header (20 bytes).

		ofArray[start + 2] = (byte)(endOfHmacZone >> 8);
		ofArray[start + 3] = (byte)endOfHmacZone;

		// Compute the HMAC of the given array between start and end, using the username key in the given region.
		// The resulting HMAC goes into target.

		var hmacSha1 = client.StunHmac;

		if (hmacSha1 == null)
		{
			hmacSha1 = new Crypto.HMac(RtpClient.SHA1);
			hmacSha1.Init(client.SdpSecret, 0, client.SdpSecret.Length);
			client.StunHmac = hmacSha1;
		}
		
		// Reset it:
		hmacSha1.StatelessOutputSingleBlock(ofArray, start, end - start, target);

		// Restore the lengths:
		ofArray[start + 2] = origLength2;
		ofArray[start + 3] = origLength3;

	}

	/// <summary>
	/// Calcs message-integrity for the given writer, from 0 to max. Compares the result with the bytes starting at hmacAt.
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="hmacAt"></param>
	/// <param name="client"></param>
	public static bool CheckIntegrity(byte[] buffer, int min, int max, int hmacAt, RtpClient client)
	{
		Span<byte> hmac = stackalloc byte[20];
		ComputeHmac(buffer, min, max, client, ref hmac);

		// Compare:
		for (var i = 0; i < 20; i++)
		{
			if (hmac[i] != buffer[hmacAt + i])
			{
				return false;
			}
		}

		return true;
	}

}

/// <summary>
/// CRC32 using STUN's polynomial.
/// </summary>
public static class StunCrc32
{
	private static uint[] _table;
	private const uint StunFingerprintXor = 0xffffffff ^ 0x5354554e;

	/// <summary>
	/// Calculate the CRC32 for a given block of bytes from min to max (excluding max). Then xors it with the STUN 
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	public static uint CalcFingerprint(byte[] buffer, int min, int max)
	{
		var crc = 0xffffffff;

		if (_table == null)
		{
			_table = CreateTable(0xedb88320);
		}

		var table = _table;

		for (var index = min; index < max; index++)
		{
			crc = table[(crc ^ buffer[index]) & 0xff] ^ (crc >> 8);
		}

		return crc ^ StunFingerprintXor;
	}

	private static uint[] CreateTable(uint polynom)
	{
		var table = new uint[256];

		for (uint i = 0; i < 256; i++)
		{
			uint crc = i;

			for (var j = 0; j < 8; j++)
			{
				crc = ((crc & 1) == 1) ? (crc >> 1) ^ polynom : crc >> 1;
			}

			table[i] = crc;
		}

		return table;
	}
}