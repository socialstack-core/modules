using Api.SocketServerLibrary;

namespace Api.WebRTC.Codecs;


/// <summary>
/// Convenience functions for writing opus ogg files from the opus RTP packets.
/// </summary>
public static class OpusWriter
{
	private static byte[] OggS = new byte[]{ (byte)'O', (byte)'g', (byte)'g', (byte)'S' };
	private static byte[] OpusHead = new byte[]{ (byte)'O', (byte)'p', (byte)'u', (byte)'s', (byte)'H', (byte)'e', (byte)'a', (byte)'d' };
	private static byte[] OpusTags = new byte[]{ (byte)'O', (byte)'p', (byte)'u', (byte)'s', (byte)'T', (byte)'a', (byte)'g', (byte)'s' };
	private static byte[] Vendor = new byte[]{ (byte)'s', (byte)'t', (byte)'a', (byte)'c', (byte)'k' };
	
	/// <summary>
	/// Writes the header at the start of a .opus file (ogg container, OPUS audio).
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="mono"></param>
	public static void WriteInitialHeader(Writer writer, bool mono = false)
	{
		// https://datatracker.ietf.org/doc/html/rfc7845
		// Ogg is little endian.

		// Opus header page
		var crcStart = writer.Length;
		writer.WriteNoLength(OggS); // Capture pattern
		writer.Write((byte)0); // Version
		writer.Write((byte)2); // Header type (2, beginning of stream)
		writer.Write((ulong)0); // Granule position. Must be 0 for the first packet in an Opus stream.
		writer.Write((uint)1); // Bistream serial number. Only 1 logical stream inside the file so this can be a constant 1.
		writer.Write((uint)0); // Page ID, starts from 0.
		writer.Write((uint)0); // Crc32 of the whole page. Set initially as 0 as the Crc does cover "itself" and is required to be 0 when calculating it.
		var crcPos = writer.CurrentFill - 4; // Track current position so we can come back for the CRC later.
		var crcBuffer = writer.LastBuffer;
		
		writer.Write((byte)1); // Segment count
		writer.Write((byte)19); // OpusHead segment size (a constant 19)
		writer.WriteNoLength(OpusHead);
		writer.Write((byte)1); // OpusHead Version
		writer.Write((byte)(mono ? 1 : 2)); // Channel count
		writer.Write((ushort)0); // Pre-skip (little endian)
		writer.Write((uint)48000); // Sample rate. Always 48khz in WebRTC. (little endian)
		writer.Write((ushort)0); // Output gain (little endian)
		writer.Write((byte)0); // Channel mapping family

		// Calculate the CRC32:
		var crc32 = writer.GetCrc32(crcStart, writer.Length - crcStart);

		// Write CRC32:
		crcBuffer.Bytes[crcPos++] = (byte)crc32;
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 8);
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 16);
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 24);

		// Opus comment header page
		crcStart = writer.Length;
		writer.WriteNoLength(OggS);
		writer.Write((byte)0); // Version
		writer.Write((byte)0); // Header type (0, continuation of the intro)
		writer.Write((ulong)0); // Granule position
		writer.Write((uint)1); // Bistream serial number.
		writer.Write((uint)1); // Page ID, starts from 0.
		writer.Write((uint)0); // Crc32 of the whole page. Set initially as 0 as the Crc does cover "itself" and is required to be 0 when calculating it.
		crcPos = writer.CurrentFill - 4; // Track current position so we can come back for the CRC later.
		crcBuffer = writer.LastBuffer;
		
		writer.Write((byte)1); // Segment count
		writer.Write((byte)(16 + Vendor.Length)); // OpusTags segment size. Varies depending on the vendor value, but is effectively constant.
		writer.WriteNoLength(OpusTags);
		writer.Write((uint)Vendor.Length); // Vendor length
		writer.WriteNoLength(Vendor);
		writer.Write((uint)0); // Additional tag count

		// Calculate the CRC32:
		crc32 = writer.GetCrc32(crcStart, writer.Length - crcStart);

		// Write CRC32:
		crcBuffer.Bytes[crcPos++] = (byte)crc32;
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 8);
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 16);
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 24);
	}

	/// <summary>
	/// Writes a packet from an RTP payload.
	/// </summary>
	public static void WritePacketFromRTP(Writer writer, uint packetNumber, ulong numberOf48KhzSamples, byte[] buffer, int offset, int length)
	{
		// https://datatracker.ietf.org/doc/html/rfc7845

		var crcStart = writer.Length;

		writer.WriteNoLength(OggS); // Capture pattern
		writer.Write((byte)0); // Version
		writer.Write((byte)0); // Header type (0, continuation)
		writer.Write((ulong)numberOf48KhzSamples); // Granule position - is the number of samples. Note that this is 64 bit which is why it's a ulong above.
		writer.Write((uint)1); // Bistream serial number. Only 1 logical stream inside the file so this can be a constant 1.
		writer.Write(packetNumber + 2); // Page ID, starts from 0.
		writer.Write((uint)0); // Crc32 of the whole page. Set initially as 0 as the Crc does cover "itself" and is required to be 0 when calculating it.
		var crcPos = writer.CurrentFill - 4; // Track current position so we can come back for the CRC later.
		var crcBuffer = writer.LastBuffer;
		
		writer.Write((byte)1); // Segment count
		writer.Write((byte)length); // Segment length
		
		// Write the payload now:
		writer.Write(buffer, offset, length);

		// Calculate the CRC32:
		var crc32 = writer.GetCrc32(crcStart, writer.Length - crcStart);

		// Write CRC32:
		crcBuffer.Bytes[crcPos++] = (byte)crc32;
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 8);
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 16);
		crcBuffer.Bytes[crcPos++] = (byte)(crc32 >> 24);
	}

}