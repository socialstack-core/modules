using System;

namespace Api.SocketServerLibrary;

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
	public static int PayloadStart(byte[] buffer, UdpDestination server, out ushort remotePort, ref Span<byte> ipBytes, out bool isV4)
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
