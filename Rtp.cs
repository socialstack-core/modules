using System;

namespace Api.WebRTC;

/// <summary>
/// Helpers for handling RTP packets.
/// </summary>
public static class Rtp
{
	/// <summary>
	/// SRTP/SRTCP packet main receive point.
	/// </summary>
	public static void HandleMessage(byte[] buffer, int index, int messageSize, RtpClient client)
	{
		if (messageSize < 12)
		{
			// there are at least 12 bytes in an RTP/ RTCP packet header.
			return;
		}

		var startIndex = index;

		// First, do we have SRTP (actual media) or SRTCP (a control packet)?
		// This can be identified from the payload type byte, which for media will be in the range 96-127.
		// Note that it's in that region because our SDP offer/ answer makes it so.
		// This specific range is selected to avoid collisions with a variety of RTCP packets which are in use.

		var typeAndMarker = buffer[index+1]; // and marker bit in SRTP

		var payloadType = typeAndMarker & 127;
		
		if (payloadType >= 96 && payloadType <= 127)
		{
			// SRTP

			var flags = buffer[index]; // version, padding, header extension flag, CC count.
			ushort seq = (ushort)(buffer[index+2] << 8 | buffer[index + 3]);
			uint ssrc = (uint)(buffer[index + 8] << 24 | buffer[index + 9] << 16 | buffer[index + 10] << 8 | buffer[index + 11]);

			index += 12;

			// CC values next:
			var ccCount = flags & 15;
			index += 4 * ccCount;

			/*
			for (var i = 0; i < ccCount; i++)
			{
				var cc = (uint)(buffer[index++] << 24 | buffer[index++] << 16 | buffer[index++] << 8 | buffer[index++]);
			}
			*/

			var isExtended = (flags & 16) == 16;
			var isPadded = (flags & 32) == 32;

			// The meaning of these extensions is defined in the original SDP via the extmap value.
			/*
			if (isExtended)
			{
				// Extended header.
				ushort profile = (ushort)(buffer[index++] << 8 | buffer[index++]);
				int length = (buffer[index++] << 8 | buffer[index++]) * 4; // The length is a count of 32 bits.

				var extOffset = index;
				var end = extOffset + length;

				if (profile == 0xbede)
				{
					// RFC 8285 RTP One Byte Header Extension
					while (extOffset < end)
					{
						if (buffer[extOffset] == 0)
						{
							extOffset++;
							continue;
						}

						var extId = buffer[extOffset] >> 4;

						var len = (buffer[extOffset] & (buffer[extOffset] ^ 0xf0)) + 1;
						extOffset++;

						if (extId == 0xf)
						{
							break;
						}

						if (len == 1)
						{
							// System.Console.WriteLine("1 byte extension, id: " + extId + "(" + len + "). Value: " + buffer[extOffset]);
						}
						else
						{
							// System.Console.WriteLine("1 byte extension, id: " + extId + "(" + len + ")");
						}

						extOffset += len;
					}
				}
				else if (profile == 0x1000)
				{
					// RFC 8285 RTP Two Byte Header Extension
					while (extOffset < end)
					{
						var extId = buffer[extOffset++];

						if (extId == 0)
						{
							continue;
						}

						var len = buffer[extOffset++];

						// System.Console.WriteLine("2 byte extension, id: " + extId + "(" + len + ")");

						extOffset += len;
					}
				}

				index += length;
			}
			*/

			var extStart = -1;

			if (isExtended)
			{
				extStart = index;
				index += 2; // profile
				int length = (buffer[index++] << 8 | buffer[index++]) * 4; // The length is a count of 32 bits.
				index += length;
			}

			// Ok, next we need to determine the packet index (RFC 3711, 3.3.1). This is then used in the decryption.
			var ssrcIndex = client.GetSsrcStateIndex(ssrc);

			if (ssrcIndex == -1)
			{
				// Claim one and set the first seq:
				ssrcIndex = client.AssignSsrcStateIndex(ssrc);
				client.SsrcState[ssrcIndex].Sequence_1 = seq;
				Console.WriteLine("Assigned SSRC state for " + ssrc);
			}

			client.SsrcState[ssrcIndex].PacketsReceived++;
			client.SsrcState[ssrcIndex].OctetsReceived+=(ulong)messageSize;

			var ssrcStateArray = client.SsrcState;

			// Guess the SRTP index (48 bit), see rFC 3711, 3.3.1
			// Stores the guessed roc in this.guessedROC
			var packetIndex = ssrcStateArray[ssrcIndex].GuessIndex(seq, out uint guessedROC);

			var lastRocAndS1 = (((ulong)ssrcStateArray[ssrcIndex].RolloverCode) << 16 | ssrcStateArray[ssrcIndex].Sequence_1);
			long indexDelta = (long)packetIndex - (long)lastRocAndS1;
			
			// Replay control
			if (ssrcStateArray[ssrcIndex].WasReplayed(indexDelta))
			{
				// Drop this packet
				System.Console.WriteLine("Dropping replayed packet");
				return;
			}

			if (!client.WillHandle)
			{
				// If no listeners, update index and drop packet:
				ssrcStateArray[ssrcIndex].UpdatePacketIndex(seq, indexDelta, guessedROC);
				return;
			}

			if (!client.CheckPacketTag(buffer, startIndex, messageSize, guessedROC))
			{
				System.Console.WriteLine("Packet dropped due to invalid auth tag (" + isPadded + ")");
				return;
			}

			// Uses the very last 10 bytes.
			Span<byte> ivStore = stackalloc byte[16];

			ivStore[0] = client.ReceiveSaltKey[0];
			ivStore[1] = client.ReceiveSaltKey[1];
			ivStore[2] = client.ReceiveSaltKey[2];
			ivStore[3] = client.ReceiveSaltKey[3];

			for (var i = 4; i < 8; i++)
			{
				ivStore[i] = (byte)((0xFF & (ssrc >> ((7 - i) * 8))) ^ client.ReceiveSaltKey[i]);
			}

			for (var i = 8; i < 14; i++)
			{
				ivStore[i] = (byte)((0xFF & (byte)(packetIndex >> ((13 - i) * 8))) ^ client.ReceiveSaltKey[i]);
			}

			// The last 10 bytes are the auth tag (checked above).
			var payloadSize = messageSize - 10 - (index - startIndex);
			client.ReceiveCipherCtr.Process(buffer, index, payloadSize, ivStore);

			// Update inbound packet index:
			ssrcStateArray[ssrcIndex].UpdatePacketIndex(seq, indexDelta, guessedROC);

			// buffer now contains plaintext RTP packet bytes

			/*
			if (payloadType == 96)
			{
				if (fs == null)
				{
					fs = Codecs.H264Writer.Start("test-rtp/hello-video3.h264");
				}

				var paddingSize = 0;

				if (isPadded)
				{
					// If there is some padding, there is a length byte just before the auth tag.
					// The auth tag is 10 bytes long for the _80 protection profile.
					paddingSize = buffer[startIndex + messageSize - 11];
				}

				// timestamp -= firstTimestamp;
				Codecs.H264Writer.Write(buffer, paddingSize, index, payloadSize, seq, timestamp, (typeAndMarker & 128) == 128, fs);
			}
			else
			{

				if (payloadSize < 30)
				{
					Console.WriteLine("Very short payload, " + payloadSize +". " + isPadded);

					System.IO.File.WriteAllText("sm-payload-" + seq + ".bin", SocketServerLibrary.Hex.ConvertWithSeparator(buffer, ' '));

				}

			}
			*/

			/*
			 if (fs == null)
			{
				fs = new System.IO.FileStream("test-rtp/hello-world2.h264", System.IO.FileMode.Create);
				
				var writer2 = SocketServerLibrary.Writer.GetPooled();
				writer2.Start(null);
				Codecs.OpusWriter.WriteInitialHeader(writer2);
				fs.Write(writer2.AllocatedResult());
				writer2.Release();
			}

			if (payloadSize >= 4)
			{
				if (firstTimestamp == 0)
				{
					firstTimestamp = timestamp;
				}

				// timestamp -= firstTimestamp;

				var writer3 = SocketServerLibrary.Writer.GetPooled();
				writer3.Start(null);
				Codecs.OpusWriter.WritePacketFromRTP(writer3, 0, timestamp, buffer, index, payloadSize);
				fs.Write(writer3.AllocatedResult());
				writer3.Release();
			}
			 */

			/*
			var paddingSize = 0;

			if (isPadded)
			{
				// If there is some padding, there is a length byte just before the auth tag.
				// The auth tag is 10 bytes long for the _80 protection profile.
				paddingSize = buffer[size - 11];
			}
			*/

			client.HandleRtpPacket(buffer, startIndex, index, messageSize, payloadType, extStart, ssrc);
		}
		else
		{
			return;
			// RTCP

			// Packet index:
			var maxIndex = startIndex + messageSize;
			var packetIndexE = (uint)(buffer[maxIndex - 14] << 24 | buffer[maxIndex - 13] << 16 | buffer[maxIndex - 12] << 8 | buffer[maxIndex - 11]);
			bool decrypt = ((packetIndexE & 0x80000000) == 0x80000000);
			var packetIndex = (packetIndexE & ~0x80000000);
			uint ssrc = (uint)(buffer[startIndex+4] << 24 | buffer[startIndex + 5] << 16 | buffer[startIndex + 6] << 8 | buffer[startIndex + 7]);
			
			// Offset over the fixed header:
			index += 8;

			var ssrcIndex = client.GetSsrcStateIndex(ssrc);

			if (ssrcIndex == -1)
			{
				// Claim one and set the first seq:
				ssrcIndex = client.AssignSsrcStateIndex(ssrc);
				client.SsrcState[ssrcIndex].RtcpSequence_1 = packetIndex;
				Console.WriteLine("Assigned SSRC state for RTCP " + ssrc);
			}

			var ssrcStateArray = client.SsrcState;

			long indexDelta = (long)packetIndex - (long)ssrcStateArray[ssrcIndex].RtcpSequence_1;

			if (ssrcStateArray[ssrcIndex].WasRtcpReplayed(indexDelta))
			{
				// Drop this packet
				System.Console.WriteLine("Dropping replayed RTCP packet");
				return;
			}

			// System.Console.WriteLine("- RTCP packet received (PT " + typeAndMarker + ", len: " + size + "). " + packetIndex + " -");

			// Authenticate:
			if (!client.CheckRtcpPacketTag(buffer, startIndex, messageSize, packetIndexE))
			{
				System.Console.WriteLine("Packet dropped due to invalid auth tag (RTCP " + packetIndex + ")");
				return;
			}

			/*
			if (fsIn == null)
			{
				fsIn = new System.IO.FileStream("test-rtp/rtcp-inbound.bin", System.IO.FileMode.Create);
			}
			*/

			Span<byte> ivStore = stackalloc byte[16];

			ivStore[0] = client.RtcpReceiveSaltKey[0];
			ivStore[1] = client.RtcpReceiveSaltKey[1];
			ivStore[2] = client.RtcpReceiveSaltKey[2];
			ivStore[3] = client.RtcpReceiveSaltKey[3];

			// The shifts transform the ssrc and index into network order
			ivStore[4] = (byte)(((ssrc >> 24) & 0xff) ^ client.RtcpReceiveSaltKey[4]);
			ivStore[5] = (byte)(((ssrc >> 16) & 0xff) ^ client.RtcpReceiveSaltKey[5]);
			ivStore[6] = (byte)(((ssrc >> 8) & 0xff) ^ client.RtcpReceiveSaltKey[6]);
			ivStore[7] = (byte)((ssrc & 0xff) ^ client.RtcpReceiveSaltKey[7]);

			ivStore[8] = client.RtcpReceiveSaltKey[8];
			ivStore[9] = client.RtcpReceiveSaltKey[9];

			ivStore[10] = (byte)(((packetIndex >> 24) & 0xff) ^ client.RtcpReceiveSaltKey[10]);
			ivStore[11] = (byte)(((packetIndex >> 16) & 0xff) ^ client.RtcpReceiveSaltKey[11]);
			ivStore[12] = (byte)(((packetIndex >> 8) & 0xff) ^ client.RtcpReceiveSaltKey[12]);
			ivStore[13] = (byte)((packetIndex & 0xff) ^ client.RtcpReceiveSaltKey[13]);

			var payloadSize = messageSize - 22; // 10 for the tag, 4 for the packet index, 8 for the fixed header.
			client.RtcpReceiveCipherCtr.Process(buffer, index, payloadSize, ivStore);

			index = startIndex;
			maxIndex -= 14; // avoid tag and packet index

			// These can (and often do) stack. They have a length value in their header which can thus be used to 
			// Identify the individual packets in the buffer.
			while (index < maxIndex)
			{
				var flags = buffer[index++];
				payloadType = buffer[index++];
				var packetLen = ((buffer[index++] << 8 | buffer[index++])+1) * 4;
				var chunkIndex = index;

				// 200, 201, 202, 206
				switch (payloadType)
				{
					case 200:
						// Sender report. Must track the time that this was received.
						// Console.WriteLine("SSRC for sender report " + ssrc + " and the count was " + (flags & 63));
						var localSsrc = (uint)(buffer[chunkIndex++] << 24 | buffer[chunkIndex++] << 16 | buffer[chunkIndex++] << 8 | buffer[chunkIndex++]);
						
						var localIndex = localSsrc == ssrc ? ssrcIndex : client.GetSsrcStateIndex(localSsrc);

						if (localIndex != -1)
						{
							var ntp = (ulong)buffer[chunkIndex++] << 56 | (ulong)buffer[chunkIndex++] << 48 | (ulong)buffer[chunkIndex++] << 40 | (ulong)buffer[chunkIndex++] << 32 |
								(ulong)buffer[chunkIndex++] << 24 | (ulong)buffer[chunkIndex++] << 16 | (ulong)buffer[chunkIndex++] << 8 | (ulong)buffer[chunkIndex++];

							ssrcStateArray[ssrcIndex].SenderReportTime = (uint)(ntp >> 16);
							ssrcStateArray[ssrcIndex].SenderReportReceivedAt = DateTime.UtcNow.Ticks;
						}

					break;
					case 203:
						// RTCP BYE - Client is going away (bye!)
						client.CloseRequested();
					break;
					case 206:
						// REMB sender side bitrate estimate
						chunkIndex += 12; // Skipping unused mediaSSRC, packet sender and "REMB" identifier

						var numSSRCs = buffer[chunkIndex++];

						var expMantissa = (uint)(buffer[chunkIndex++] << 16 | buffer[chunkIndex++] << 8 | buffer[chunkIndex++]);

						// Console.WriteLine("REMB media SSRC count " + numSSRCs + " and exp/mant " + expMantissa);
					break;
				}

				// If unrecognised, just skip it:
				index += packetLen - 4;
			}

			/*
			var bytes = System.Text.Encoding.ASCII.GetBytes("0000 " + SocketServerLibrary.Hex.ConvertWithSeparator(buffer, 0, messageSize + startIndex, ' ') + " ....\r\n");
			fsIn.Write(bytes);
			fsIn.Flush();
			*/

			/*
			// Immediately reflect it back:
			// client.EncryptRtcpPacket(buffer, startIndex + 8, payloadSize, packetIndex, ssrc);

			// And authenticate the packet:
			// client.AuthenticateRtcpPacket(buffer, startIndex, messageSize, packetIndexE);

			// TEMP - Remove encrypted flag:
			buffer[maxIndex] = (byte)(buffer[maxIndex] & ~0x80);
			
			// Reflect back the RTCP message:
			var writer = client.Server.StartMessage(client);
			writer.Write(buffer, startIndex, messageSize - 10); // everything
			client.Send(writer);
			writer.Release();
			*/

			ssrcStateArray[ssrcIndex].UpdateRtcp(packetIndex, indexDelta);

		}

	}

	/// <summary>
	/// Sends an RTP packet to the given client. It must be unencrypted and still have the tag at the end for this to work.
	/// </summary>
	/// <param name="client"></param>
	/// <param name="buffer"></param>
	/// <param name="startIndex"></param>
	/// <param name="index"></param>
	/// <param name="messageSize"></param>
	/// <param name="ssrc"></param>
	public static void SendRtpPacket(RtpClient client, byte[] buffer, int startIndex, int index, int messageSize, uint ssrc)
	{

		var ssrcIndex = client.GetSsrcStateIndex(ssrc);
		
		if (ssrcIndex == -1)
		{
			// Claim one and set the first seq:
			ssrcIndex = client.AssignSsrcStateIndex(ssrc);
			client.SsrcState[ssrcIndex].Sequence_1 = 1;
			Console.WriteLine("Assigned SSRC state for " + ssrc);
		}
		
		ushort seq = client.SsrcState[ssrcIndex].Sequence_1++;

		if (seq == 0)
		{
			// It rolled over
			client.SsrcState[ssrcIndex].RolloverCode++;
		}

		uint rolloverCode = client.SsrcState[ssrcIndex].RolloverCode;
		ulong packetIndex = (rolloverCode << 16) | seq;

		// Build an outbound message:
		var writer = client.Server.StartMessage(client);
		var outboundHeaderSize = writer.Length;
		writer.Write(buffer, startIndex, messageSize);
		var outboundBuffer = writer.FirstBuffer.Bytes;

		outboundBuffer[outboundHeaderSize + 2] = (byte)(seq >> 8);
		outboundBuffer[outboundHeaderSize + 3] = (byte)seq;

		/*
		outboundBuffer[outboundHeaderSize + 8] = (byte)(ssrc >> 24);
		outboundBuffer[outboundHeaderSize + 9] = (byte)(ssrc >> 16);
		outboundBuffer[outboundHeaderSize + 10] = (byte)(ssrc >> 8);
		outboundBuffer[outboundHeaderSize + 11] = (byte)ssrc;
		*/

		// Encrypt the packet:
		var rtpHeaderSize = (index - startIndex);
		var payloadSize = messageSize - 10 - rtpHeaderSize;
		client.EncryptRtpPacket(outboundBuffer, outboundHeaderSize + rtpHeaderSize, payloadSize, packetIndex, ssrc);

		// And authenticate the packet:
		client.AuthenticatePacket(outboundBuffer, outboundHeaderSize, messageSize, rolloverCode);

		client.SendAndRelease(writer);

		uint timestamp = (uint)(buffer[startIndex + 4] << 24 | buffer[startIndex + 5] << 16 | buffer[startIndex + 6] << 8 | buffer[startIndex + 7]);
		client.SsrcState[ssrcIndex].LatestRtpTimestamp = timestamp;
		client.SsrcState[ssrcIndex].PacketsSent++;
		client.SsrcState[ssrcIndex].OctetsSent += (ulong)messageSize;
	}

	private static void WriteWavHeader(System.IO.FileStream stream, bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
	{
		stream.Position = 0;

		// RIFF header.
		// Chunk ID.
		stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);

		// Chunk size.
		stream.Write(System.BitConverter.GetBytes(((bitDepth / 8) * totalSampleCount) + 36), 0, 4);

		// Format.
		stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);



		// Sub-chunk 1.
		// Sub-chunk 1 ID.
		stream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);

		// Sub-chunk 1 size.
		stream.Write(System.BitConverter.GetBytes(16), 0, 4);

		// Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
		stream.Write(System.BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)), 0, 2);

		// Channels.
		stream.Write(System.BitConverter.GetBytes(channelCount), 0, 2);

		// Sample rate.
		stream.Write(System.BitConverter.GetBytes(sampleRate), 0, 4);

		// Bytes rate.
		stream.Write(System.BitConverter.GetBytes(sampleRate * channelCount * (bitDepth / 8)), 0, 4);

		// Block align.
		stream.Write(System.BitConverter.GetBytes((ushort)channelCount * (bitDepth / 8)), 0, 2);

		// Bits per sample.
		stream.Write(System.BitConverter.GetBytes(bitDepth), 0, 2);



		// Sub-chunk 2.
		// Sub-chunk 2 ID.
		stream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);

		// Sub-chunk 2 size.
		stream.Write(System.BitConverter.GetBytes((bitDepth / 8) * totalSampleCount), 0, 4);
	}

	/// <summary>
	/// Sends a sender report for the given client.
	/// </summary>
	/// <param name="client"></param>
	/// <param name="nowTicks"></param>
	/// <param name="ntpFull"></param>
	/// <param name="ntpLow"></param>
	public static void SendReport(RtpClient client, long nowTicks, ulong ntpFull, uint ntpLow)
	{
		if (client.SsrcState == null || client.SsrcState.Length == 0)
		{
			return;
		}

		/*
		if (fs == null)
		{
			fs = new System.IO.FileStream("test-rtp/rtcp2.bin", System.IO.FileMode.Create);
		}
		*/

		var set = client.SsrcState;
		for (var i = 0; i < set.Length; i++)
		{
			if (set[i].PacketsReceived == 0)
			{
				continue;
			}

			var writer = client.Server.StartMessage(client);
			var startIndex = writer.Length;
			var buffer = writer.FirstBuffer.Bytes;

			var packetIndex = client.CurrentRtcpId++;
			var packetIndexE = packetIndex | 0x80000000;

			var ssrc = (uint)2;
			
			writer.Write((byte)(128 | 1));
			writer.Write((byte)201); // Receiver report payload type
			writer.WriteBE((ushort)7); // Length including the header. Divided by 4, plus 1. It's *6 here because each block is 24 bytes. 24/4=6.
			writer.WriteBE((uint)ssrc); // SSRC for the server
			
			byte fractionLoss = 0;
			uint totalLoss = 0;
			uint jitter = 1;
			uint lsr = set[i].SenderReportTime;
			uint dlsr = 0;

			if (lsr != 0)
			{
				var receivedAt = set[i].SenderReportReceivedAt;

				// Difference in time:
				var ticksDiff = nowTicks - receivedAt; // This is in ticks.

				// DLSR is in 1/2^16-th of a second
				// There are TimeSpan.TicksPerSecond ticks per second
				// 10000000 // very approx 2^23, thus ntpDiff * 2^16 / 2^23 which gives us a >> 23 and << 16 for a >> 7:
				dlsr = (uint)(ticksDiff >> 7);
			}

			// Source SSRC
			writer.WriteBE(set[i].Ssrc);

			// Loss
			writer.WriteBE((uint)(fractionLoss << 24) | totalLoss);

			// Extended highest sequence number received so far:
			writer.WriteBE(set[i].ExtendedSequence);

			writer.WriteBE(jitter);
			writer.WriteBE(lsr);
			writer.WriteBE(dlsr);

			writer.WriteBE(packetIndexE);
			writer.WriteNoLength(EmptyTag);

			var messageSize = writer.Length - startIndex;
			var payloadSize = messageSize - 22;

			/*
			UdpHeader.Complete(writer);
			var bytes = System.Text.Encoding.ASCII.GetBytes("0000 " + SocketServerLibrary.Hex.ConvertWithSeparator(buffer, 0, writer.Length, ' ') + " ....\r\n");
			fs.Write(bytes);
			fs.Flush();
			*/

			// Encrypt the message:
			client.EncryptRtcpPacket(buffer, startIndex + 8, payloadSize, packetIndex, ssrc);

			// And authenticate the packet:
			client.AuthenticateRtcpPacket(buffer, startIndex, messageSize, packetIndexE);

			// Send it:
			client.SendAndRelease(writer);
		}

		// Sender reports - this is optional and can be omitted if no packets were received since the last one sent.
		for (var i = 0; i < set.Length; i++)
		{
			if (set[i].PacketsSent == 0)
			{
				continue;
			}

			var writer = client.Server.StartMessage(client);
			var startIndex = writer.Length;
			var buffer = writer.FirstBuffer.Bytes;

			var packetIndex = client.CurrentRtcpId++;
			var packetIndexE = packetIndex | 0x80000000;
				
			// Fixed header:
			writer.Write((byte)(128)); // version 2 only; there is no report count.
			writer.Write((byte)200); // Sender report payload type
			writer.WriteBE((ushort)6); // Length including the header. Always a constant for these SR's.

			// Source SSRC
			writer.WriteBE(set[i].Ssrc);

			writer.WriteBE((uint)(ntpFull >> 32));
			writer.WriteBE((uint)ntpFull);

			// RTP timestamp - simply latest timestamp transmitted:
			writer.WriteBE(set[i].LatestRtpTimestamp);

			// Sender stats:
			writer.WriteBE((uint)set[i].PacketsSent);
			writer.WriteBE((uint)set[i].OctetsSent);

			// Source description:

			// Fixed header:
			writer.Write((byte)(128 | 1)); // version 2 only; 1 count always.
			writer.Write((byte)202); // Sender report payload type
			writer.WriteBE((ushort)3); // Length including the header. Also a constant.

			// Source SSRC
			writer.WriteBE(set[i].Ssrc);

			// SDES
			writer.Write((byte)1); // CNAME
			writer.Write((byte)5); // Length
			var cpos = writer.CurrentFill;

			writer.Write((byte)'h');
			writer.WriteS(set[i].Ssrc);
			var len = writer.CurrentFill - cpos;

			for (var p = len; p < 5; p++)
			{
				writer.Write((byte)'_');
			}
			
			writer.Write((byte)0); // End

			// REMB feedback:

			/*
			// Fixed header:
			writer.Write((byte)(128 | 15)); // version 2 only; fixed '15' in here.
			writer.Write((byte)206); // Feedback
			writer.WriteBE((ushort)5); // Length including the header. Also a constant.

			// Source SSRC
			writer.WriteBE(set[i].Ssrc); // The source SSRC
			writer.WriteBE((uint)0); // Media SSRC (always 0)
			writer.Write((byte)'R');
			writer.Write((byte)'E');
			writer.Write((byte)'M');
			writer.Write((byte)'B');

			uint ssrcAndBitrate = ((uint)0xffff20 << 8) | (uint)1; 

			writer.Write(ssrcAndBitrate); // 1 SSRC, the bitrate estimation
			writer.WriteBE(set[i].Ssrc); // The SSRC again

			/
			UdpHeader.Complete(writer);
			var bytes = System.Text.Encoding.ASCII.GetBytes("0000 " + SocketServerLibrary.Hex.ConvertWithSeparator(buffer, 0, writer.Length, ' ') + " ....\r\n");
			fs.Write(bytes);
			fs.Flush();
			*/

			writer.WriteBE(packetIndexE);
			writer.WriteNoLength(EmptyTag);

			var messageSize = writer.Length - startIndex;
			var payloadSize = messageSize - 22;

			// Encrypt the message:
			client.EncryptRtcpPacket(buffer, startIndex + 8, payloadSize, packetIndex, set[i].Ssrc);

			// And authenticate the packet:
			client.AuthenticateRtcpPacket(buffer, startIndex, messageSize, packetIndexE);

			// Send it:
			client.SendAndRelease(writer);
		}
	}

	private static byte[] EmptyTag = new byte[10];
	// private static System.IO.FileStream fs;
	// private static System.IO.FileStream fsIn;

	// SRTCP packet:

	/*
		  0                   1                   2                   3
		  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
		 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<+
		 |V=2|P|    RC   |   PT=SR or RR   |             length          | |
		 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
		 |                         SSRC of sender                        | |
	   +>+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+ |
	   | ~                          sender info                          ~ |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | ~                         report block 1                        ~ |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | ~                         report block 2                        ~ |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | ~                              ...                              ~ |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | |V=2|P|    SC   |  PT=SDES=202  |             length            | |
	   | +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+ |
	   | |                          SSRC/CSRC_1                          | |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | ~                           SDES items                          ~ |
	   | +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+ |
	   | ~                              ...                              ~ |
	   +>+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+<+
	   | |E|                         SRTCP index                         | |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | ~                     SRTCP MKI (OPTIONAL)                      ~ |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   | :                     authentication tag                        : |
	   | +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
	   |                                                                   |
	   +-- Encrypted Portion                    Authenticated Portion -----+
	*/


	// SRTP packet:

	/*
  0                   1                   2                   3
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<+
 |V=2|P|X|  CC   |M|     PT      |       sequence number         | |
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
 |                           timestamp                           | |
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
 |           synchronization source (SSRC) identifier            | |
 +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+ |
 |            contributing source (CSRC) identifiers             | |
 |                               ....                            | |
 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
 |                   RTP extension (OPTIONAL)                    | |
+>+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
| |                          payload  ...                         | |
| |                               +-------------------------------+ |
| |                               | RTP padding   | RTP pad count | |
+>+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+<+
| ~                     SRTP MKI (OPTIONAL)                       ~ |
| +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
| :                 authentication tag (RECOMMENDED)              : |
| +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ |
|                                                                   |
+- Encrypted Portion*                      Authenticated Portion ---+
	 */


}