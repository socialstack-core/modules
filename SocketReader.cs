using System;
using System.Collections.Generic;

namespace Api.SocketServerLibrary {

	/// <summary>
	/// Non-blocking, non-allocating socket reader.
	/// </summary>
	public partial class SocketReader
	{

		#if DEBUG && SOCKET_SERVER_PROBE_ON
		/// <summary>
		/// Probe meta for the message currently being received from this client.
		/// </summary>
		public ProbeMeta ProbeMeta;
		#endif

		private int Index = 0;
		private int DataLength = 0;
		private byte[] Data;
		private int WaitingFor = 0;
		private int PendingBufferIndex = 0;
		/// <summary>Last time a message was received. The Time.GlobalTime.</summary>
		public uint LastMessageAt;
		private BufferedBytes FirstPendingBuffer;
		private BufferedBytes LastPendingBuffer;
		private BufferedBytes DataContainer;
		private Action<BufferSegment> PendingCallback;
		/// <summary>
		/// Number of bytes read.
		/// </summary>
		protected long BytesRead = 0;
		/// <summary>Bytes until the next websocket container header.</summary>
		public int BytesUntilWebsocketHeader;
		/// <summary>The 4 byte xor mask for websocket links.</summary>
		public byte[] WebsocketMask;
		/// <summary>0-3 index in the websocket mask.</summary>
		public int WebsocketMaskIndex;
		/// <summary>True if this reader is running in websocket mode. It dumps websocket headers and handles xor masking.</summary>
		public bool IsWebsocketMode
		{
			get
			{
				return WebsocketMask != null;
			}
		}

		private readonly Action<BufferSegment> ThenReadSingleByte_D;
		private readonly Action<BufferSegment> ThenToMySQLString_D;
		private readonly Action<BufferSegment> ThenSkipBytes_D;
		private readonly Action<byte> ThenReadPackedSwitch_D;
		private readonly Action<byte> ThenReadPackedIntSwitch_D;
		private readonly Action<byte> ThenCheckForNul_D;
		private readonly Action<long> ThenReadBufferLong_D;
		private readonly Action<BufferSegment> ThenReadDateTime_D;
		private readonly Action<BufferSegment> ThenReadNDateTime_D;
		private readonly Action<BufferSegment> ThenReadNInt32_D;
		private readonly Action<BufferSegment> ThenReadNUInt32_D;
		private readonly Action<BufferSegment> ThenReadUInt32_D;
		private readonly Action<BufferSegment> ThenReadUInt24_D;
		private readonly Action<BufferSegment> ThenReadUInt16_D;
		private readonly Action<BufferSegment> ThenReadInt32_D;
		private readonly Action<BufferSegment> ThenReadInt24_D;
		private readonly Action<BufferSegment> ThenReadInt16_D;
		private readonly Action<BufferSegment> ThenReadU64_D;
		private readonly Action<BufferSegment> ThenReadU32_D;
		private readonly Action<BufferSegment> ThenReadU48_D;
		private readonly Action<BufferSegment> ThenReadU40_D;
		private readonly Action<BufferSegment> ThenReadU24_D;
		private readonly Action<BufferSegment> ThenReadShort_D;
		private readonly Action<BufferSegment> ThenReadI64_D;
		private readonly Action<BufferSegment> ThenReadI32_D;
		private readonly Action<BufferSegment> ThenReadI24_D;
		private readonly Action<BufferSegment> ThenReadI16_D;
		private readonly Action<BufferSegment> ThenReadDouble_D;
		private readonly Action<BufferSegment> ThenReadFloat_D;
		private readonly Action<byte> ThenReadNDateTimeFlag_D;
		private readonly Action<byte> ThenReadNInt32Flag_D;
		private readonly Action<byte> ThenReadNUInt32Flag_D;
		private readonly Action<ulong> ThenReadXBytes_D;
		private readonly Action<ulong> ThenReadXItems_D;
		private readonly Action<ulong> ThenReadXListItems_D;
		private readonly Action<BufferSegment> ThenReadStringListItem_D;
		private readonly Action<BufferSegment> ThenReadBufferArrayItem_D;

		private List<object> __Stack = new List<object>(10);
		private int __Index;

		/// <summary>
		/// Pushes a callback to the callback stack.
		/// </summary>
		public void Push(object cb)
		{
			if (__Index == __Stack.Count)
			{
				__Stack.Add(cb);
				__Index++;
			}
			else
			{
				__Stack[__Index++] = cb;
			}
		}

		/// <summary>
		/// Pops from the callback stack.
		/// </summary>
		public object Pop()
		{
			__Index--;
			return __Stack[__Index];
		}

		/// <summary>
		/// Peek the stack at a particular depth.
		/// </summary>
		/// <param name="depth"></param>
		/// <returns></returns>
		public object Peek(int depth)
		{
			return __Stack[__Index - 1 - depth];
		}

		/// <summary>
		/// Peek the stack.
		/// </summary>
		/// <returns></returns>
		public object Peek()
		{
			return __Stack[__Index - 1];
		}

		/// <summary>
		/// Creates a new socket reader.
		/// </summary>
		public SocketReader()
		{
			ThenReadSingleByte_D = new Action<BufferSegment>(ThenReadSingleByte);
			ThenToMySQLString_D = new Action<BufferSegment>(ThenToMySQLString);
			ThenSkipBytes_D = new Action<BufferSegment>(ThenSkipBytes);
			ThenReadPackedSwitch_D = new Action<byte>(ThenReadPackedSwitch);
			ThenReadPackedIntSwitch_D = new Action<byte>(ThenReadPackedIntSwitch);
			ThenCheckForNul_D = new Action<byte>(ThenCheckForNul);
			ThenReadBufferLong_D = new Action<long>(ThenReadBufferLong);
			ThenReadDateTime_D = new Action<BufferSegment>(ThenReadDateTime);
			ThenReadNDateTime_D = new Action<BufferSegment>(ThenReadNDateTime);
			ThenReadNInt32_D = new Action<BufferSegment>(ThenReadNInt32);
			ThenReadNUInt32_D = new Action<BufferSegment>(ThenReadNUInt32);
			ThenReadNDateTimeFlag_D = new Action<byte>(ThenReadNDateTimeFlag);
			ThenReadNInt32Flag_D = new Action<byte>(ThenReadNInt32Flag);
			ThenReadNUInt32Flag_D = new Action<byte>(ThenReadNUInt32Flag);
			ThenReadUInt32_D = new Action<BufferSegment>(ThenReadUInt32);
			ThenReadUInt24_D = new Action<BufferSegment>(ThenReadUInt24);
			ThenReadUInt16_D = new Action<BufferSegment>(ThenReadUInt16);
			ThenReadInt32_D = new Action<BufferSegment>(ThenReadInt32);
			ThenReadInt24_D = new Action<BufferSegment>(ThenReadInt24);
			ThenReadInt16_D = new Action<BufferSegment>(ThenReadInt16);
			ThenReadU64_D = new Action<BufferSegment>(ThenReadU64);
			ThenReadU32_D = new Action<BufferSegment>(ThenReadU32);
			ThenReadU48_D = new Action<BufferSegment>(ThenReadU48);
			ThenReadU40_D = new Action<BufferSegment>(ThenReadU40);
			ThenReadU24_D = new Action<BufferSegment>(ThenReadU24);
			ThenReadShort_D = new Action<BufferSegment>(ThenReadShort);
			ThenReadI64_D = new Action<BufferSegment>(ThenReadI64);
			ThenReadI32_D = new Action<BufferSegment>(ThenReadI32);
			ThenReadI24_D = new Action<BufferSegment>(ThenReadI24);
			ThenReadI16_D = new Action<BufferSegment>(ThenReadI16);
			ThenReadDouble_D = new Action<BufferSegment>(ThenReadDouble);
			ThenReadFloat_D = new Action<BufferSegment>(ThenReadFloat);
			ThenReadXBytes_D = new Action<ulong>(ThenReadXBytes);
			ThenReadXItems_D = new Action<ulong>(ThenReadXItems);
			ThenReadXListItems_D = new Action<ulong>(ThenReadXListItems);
			ThenReadStringListItem_D = new Action<BufferSegment>(ThenReadStringListItem);
			ThenReadBufferArrayItem_D = new Action<BufferSegment>(ThenReadBufferArrayItem);
		}

		/// <summary>
		/// Sets up the underlying data buffer.
		/// </summary>
		/// <returns></returns>
		public byte[] SetupData()
		{
			Data = new byte[BinaryBufferPool.BufferSize];
			DataContainer = new BufferedBytes(Data, Data.Length);
			return Data;
		}

		/// <summary>
		/// Called to start receiving data. newData is always at least 1 byte long.
		/// </summary>
		/// <param name="length"></param>
		public void OnReceiveData(int length)
		{
			DataLength = length;
			LastMessageAt = Time.GlobalTime;
			Index = 0;

			if (WebsocketMask != null)
			{
				// Specialised websocket mode.
				// Need to read off websocket headers and also unmask the data.

				if (BytesUntilWebsocketHeader >= length)
				{
					// Receive them all:
					BytesUntilWebsocketHeader -= length;
					OnReceiveXorData(0, length);				
				}
				else
				{
					if (BytesUntilWebsocketHeader > 0)
					{
						// Receive the final bytes of the previous msg:
						OnReceiveXorData(0, BytesUntilWebsocketHeader);
					}

					var currentIndex = BytesUntilWebsocketHeader;

					// Next, burn the websocket header.
					// We might've only recvd some of its bytes though.
					// However, we know for sure there is at least one.
					while (currentIndex < length)
					{
						WebsocketMaskIndex = 0;
						var opcode = Data[currentIndex++] & 15;
						BytesUntilWebsocketHeader = Data[currentIndex++] & 127;

						if (BytesUntilWebsocketHeader == 126)
						{
							// 2 byte length follows.
							BytesUntilWebsocketHeader = Data[currentIndex++] << 8 | Data[currentIndex++];
						}
						else if (BytesUntilWebsocketHeader == 127)
						{
							// 8 byte length follows.
							ulong len = (ulong)(
								Data[currentIndex++] << 56 | Data[currentIndex++] << 48 | Data[currentIndex++] << 40 | Data[currentIndex++] << 32 |
								Data[currentIndex++] << 24 | Data[currentIndex++] << 16 | Data[currentIndex++] << 8 | Data[currentIndex++]
							);

							BytesUntilWebsocketHeader = (int)len;
						}

						// 4 mask bytes are next:
						WebsocketMask[0] = Data[currentIndex++];
						WebsocketMask[1] = Data[currentIndex++];
						WebsocketMask[2] = Data[currentIndex++];
						WebsocketMask[3] = Data[currentIndex++];

						var msgBytes = length - currentIndex;

						if (msgBytes > BytesUntilWebsocketHeader)
						{
							msgBytes = BytesUntilWebsocketHeader;
							BytesUntilWebsocketHeader = 0;
						}
						else
						{
							BytesUntilWebsocketHeader -= msgBytes;
						}

						// Receive the msg bytes (if there were any):
						OnReceiveXorData(currentIndex, msgBytes);

						currentIndex += msgBytes;
					}

				}

				return;
			}

			if (PendingCallback != null)
			{
				// How many bytes are we waiting for and how many do we now have?
				if (WaitingFor <= DataLength)
				{
					// Can now fully satisfy the pending read.

					Index += WaitingFor;

					var pc = PendingCallback;
					var fpb = FirstPendingBuffer;
					var lpb = LastPendingBuffer;
					FirstPendingBuffer = null;
					LastPendingBuffer = null;
					PendingCallback = null;

					if (fpb != null)
					{

						// Copy the last chunk of data into the pending buffer:
						lpb.CopyFrom(Data, 0, WaitingFor);

						// Run the read response now:
						pc(new BufferSegment()
						{
							FirstBuffer = fpb,
							CurrentBuffer = fpb,
							LastBuffer = lpb,
							Offset = 0,
							Length = PendingBufferIndex + WaitingFor,
							IsCopy = true
						});

					}
					else
					{
						// Run the read response now:
						pc(new BufferSegment()
						{
							FirstBuffer = DataContainer,
							CurrentBuffer = DataContainer,
							LastBuffer = DataContainer,
							Offset = 0,
							Length = WaitingFor,
							IsCopy = false
						});
					}

				}
				else
				{
					// Push all of data into pendingBuffers and keep waiting.
					if (LastPendingBuffer != null)
					{
						// It's null if we're skipping this data.
						LastPendingBuffer = LastPendingBuffer.CopyFrom(Data, 0, DataLength);
					}

					PendingBufferIndex += DataLength;
					WaitingFor -= DataLength;

					// Set index:
					Index = DataLength;
				}


			}
			else
			{
				ReadOpcode();
			}

		}

		/// <summary>Receives a particular segment of Data.</summary>
		public void OnReceiveXorData(int offset, int length)
		{
			DataLength = length + offset;
			Index = offset;

			// Unmask it:
			for (var i = offset; i < DataLength; i++)
			{
				Data[i] ^= WebsocketMask[WebsocketMaskIndex++];
				if (WebsocketMaskIndex == 4)
				{
					WebsocketMaskIndex = 0;
				}
			}

			if (PendingCallback != null)
			{
				// How many bytes are we waiting for and how many do we now have?
				if (WaitingFor <= DataLength)
				{
					// Can now fully satisfy the pending read.

					Index += WaitingFor;

					var pc = PendingCallback;
					var fpb = FirstPendingBuffer;
					var lpb = LastPendingBuffer;
					FirstPendingBuffer = null;
					LastPendingBuffer = null;
					PendingCallback = null;

					if (fpb != null)
					{

						// Copy the last chunk of data into the pending buffer:
						lpb.CopyFrom(Data, 0, WaitingFor);

						// Run the read response now:
						pc(new BufferSegment()
						{
							FirstBuffer = fpb,
							CurrentBuffer = fpb,
							LastBuffer = lpb,
							Offset = 0,
							Length = PendingBufferIndex + WaitingFor,
							IsCopy = true
						});

					}
					else
					{
						// Run the read response now:
						pc(new BufferSegment()
						{
							FirstBuffer = DataContainer,
							CurrentBuffer = DataContainer,
							LastBuffer = DataContainer,
							Offset = 0,
							Length = WaitingFor,
							IsCopy = false
						});
					}

				}
				else
				{
					// Push all of data into pendingBuffers and keep waiting.
					if (LastPendingBuffer != null)
					{
						// It's null if we're skipping this data.
						LastPendingBuffer = LastPendingBuffer.CopyFrom(Data, 0, DataLength);
					}

					PendingBufferIndex += DataLength;
					WaitingFor -= DataLength;

					// Set index:
					Index = DataLength;
				}


			}
			else
			{
				ReadOpcode();
			}

		}

		/// <summary>
		/// Called to 'wait' for the next opcode.
		/// </summary>
		public void StartNextOpcode()
		{
			if (Index == DataLength)
			{
				// Wait for another opcode:
				PendingCallback = null;
				return;
			}

			// Yes - immediately read it:
			ReadOpcode();
		}

		/// <summary>
		/// Reads a multibyte opcode and runs the opcodes own handler.
		/// </summary>
		public virtual void ReadOpcode() { }

		/// <summary>
		/// Internal - Reads a 2 byte int from a buffer. 
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadShort(BufferSegment buffer)
		{
			var cb = (Action<ulong>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 3 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadU24(BufferSegment buffer)
		{
			var cb = (Action<ulong>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8) | ((ulong)buffer.Next << 16);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 4 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadU32(BufferSegment buffer)
		{
			var cb = (Action<ulong>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8) | ((ulong)buffer.Next << 16) | ((ulong)buffer.Next << 24);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 6 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadU48(BufferSegment buffer)
		{
			var cb = (Action<ulong>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8) | ((ulong)buffer.Next << 16) | ((ulong)buffer.Next << 24) | ((ulong)buffer.Next << 32) | ((ulong)buffer.Next << 40);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 5 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadU40(BufferSegment buffer)
		{
			var cb = (Action<ulong>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8) | ((ulong)buffer.Next << 16) | ((ulong)buffer.Next << 24) | ((ulong)buffer.Next << 32);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 8 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadU64(BufferSegment buffer)
		{
			var cb = (Action<ulong>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8) | ((ulong)buffer.Next << 16) | ((ulong)buffer.Next << 24) |
					((ulong)buffer.Next << 32) | ((ulong)buffer.Next << 40) | ((ulong)buffer.Next << 48) | ((ulong)buffer.Next << 56);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 2 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadI16(BufferSegment buffer)
		{
			var cb = (Action<long>)Pop();
			var value = (long)buffer.Next | ((long)buffer.Next << 8);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 3 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadI24(BufferSegment buffer)
		{
			var cb = (Action<long>)Pop();
			var value = (long)buffer.Next | ((long)buffer.Next << 8) | ((long)buffer.Next << 16);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 4 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadI32(BufferSegment buffer)
		{
			var cb = (Action<long>)Pop();
			var value = (long)buffer.Next | ((long)buffer.Next << 8) | ((long)buffer.Next << 16) | ((long)buffer.Next << 24);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		///  Internal - Reads an 8 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadI64(BufferSegment buffer)
		{
			var cb = (Action<long>)Pop();
			var value = (long)buffer.Next | ((long)buffer.Next << 8) | ((long)buffer.Next << 16) | ((long)buffer.Next << 24) |
					((long)buffer.Next << 32) | ((long)buffer.Next << 40) | ((long)buffer.Next << 48) | ((long)buffer.Next << 56);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads an 8 byte int from a buffer. 
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadDateTime(BufferSegment buffer)
		{
			var cb = (Action<DateTime>)Pop();
			var ticks = (long)buffer.Next | ((long)buffer.Next << 8) | ((long)buffer.Next << 16) | ((long)buffer.Next << 24) |
					((long)buffer.Next << 32) | ((long)buffer.Next << 40) | ((long)buffer.Next << 48) | ((long)buffer.Next << 56);
			buffer.Release();
			// struct - not an allocation:
			cb(new DateTime(ticks));
		}
		
		/// <summary>
		/// Internal - Reads an 8 byte int from a buffer as a nullable datetime 
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadNDateTime(BufferSegment buffer)
		{
			var cb = (Action<DateTime?>)Pop();
			var ticks = (long)buffer.Next | ((long)buffer.Next << 8) | ((long)buffer.Next << 16) | ((long)buffer.Next << 24) |
					((long)buffer.Next << 32) | ((long)buffer.Next << 40) | ((long)buffer.Next << 48) | ((long)buffer.Next << 56);
			buffer.Release();
			// struct - not an allocation:
			cb(new DateTime(ticks));
		}

		/// <summary>
		/// Internal - Reads a 4 byte float from a buffer
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadFloat(BufferSegment buffer)
		{
			var cb = (Action<float>)Pop();
			var value = (uint)buffer.Next | ((uint)buffer.Next << 8) | ((uint)buffer.Next << 16) | ((uint)buffer.Next << 24);
			buffer.Release();
			cb(new FloatBits(value).Float);
		}

		/// <summary>
		/// Internal - Reads an 8 byte float from a buffer. 
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadDouble(BufferSegment buffer)
		{
			var cb = (Action<double>)Pop();
			var value = (ulong)buffer.Next | ((ulong)buffer.Next << 8) | ((ulong)buffer.Next << 16) | ((ulong)buffer.Next << 24) |
					((ulong)buffer.Next << 32) | ((ulong)buffer.Next << 40) | ((ulong)buffer.Next << 48) | ((ulong)buffer.Next << 56);
			buffer.Release();
			cb(new DoubleBits(value).Double);
		}

		/// <summary>
		/// Internal - Switches based on a packed integer
		/// </summary>
		/// <param name="c"></param>
		private void ThenReadPackedSwitch(byte c)
		{

			Action<ulong> cb;

			switch (c)
			{
				case 251:
					// 2 bytes needed.

#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Compressed, 3);
#endif

					if ((Index + 2) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<ulong>)Pop();

						BytesRead += 2;
						cb((ulong)(Data[Index++] | (Data[Index++] << 8)));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(2, ThenReadShort_D);
					}
					break;
				case 252:
					// 3 bytes needed.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Compressed, 4);
#endif
					if ((Index + 3) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<ulong>)Pop();

						BytesRead += 3;
						cb((ulong)(Data[Index++] | (Data[Index++] << 8) | (Data[Index++] << 16)));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(3, ThenReadU24_D);
					}
					break;
				case 253:
					// 4 bytes needed.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Compressed, 5);
#endif
					if ((Index + 4) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<ulong>)Pop();

						BytesRead += 4;
						cb((ulong)(Data[Index++] | (Data[Index++] << 8) | (Data[Index++] << 16) | (Data[Index++] << 24)));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(4, ThenReadU32_D);
					}
					break;
				case 254:

					// 8 bytes needed.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Compressed, 9);
#endif
					if ((Index + 8) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<ulong>)Pop();
						BytesRead += 8;

						cb((ulong)Data[Index++] | ((ulong)Data[Index++] << 8) | ((ulong)Data[Index++] << 16) | ((ulong)Data[Index++] << 24) |
						((ulong)Data[Index++] << 32) | ((ulong)Data[Index++] << 40) | ((ulong)Data[Index++] << 48) | ((ulong)Data[Index++] << 56));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(8, ThenReadU64_D);
					}

					break;
				default:
					cb = (Action<ulong>)Pop();
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Compressed, 1);
#endif
					cb(c);
					break;
			}

		}

		/// <summary>
		/// Internal - Switches based on a packed integer
		/// </summary>
		/// <param name="c"></param>
		private void ThenReadPackedIntSwitch(byte c)
		{

			Action<long> cb;

			switch (c)
			{
				case 251:
					// -1:
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Packed, 1);
#endif
					cb = (Action<long>)Pop();
					cb(-1);
					break;
				case 252:
					// 2 bytes needed.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Packed, 3);
#endif
					if ((Index + 2) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<long>)Pop();
						BytesRead += 2;
						cb((long)(Data[Index++] | (Data[Index++] << 8)));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(2, ThenReadI16_D);
					}
					break;
				case 253:
					// 3 bytes needed.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Packed, 4);
#endif
					if ((Index + 3) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<long>)Pop();
						BytesRead += 3;
						cb((long)(Data[Index++] | (Data[Index++] << 8) | (Data[Index++] << 16)));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(3, ThenReadI24_D);
					}
					break;
				case 254:

					// 8 bytes needed.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Packed, 9);
#endif
					if ((Index + 8) <= DataLength)
					{
						// Available immediately (increase index):
						cb = (Action<long>)Pop();
						BytesRead += 8;
						cb((long)Data[Index++] | ((long)Data[Index++] << 8) | ((long)Data[Index++] << 16) | ((long)Data[Index++] << 24) |
						((long)Data[Index++] << 32) | ((long)Data[Index++] << 40) | ((long)Data[Index++] << 48) | ((long)Data[Index++] << 56));
					}
					else
					{
						// Must wait - readBytes handles index for us here:
						ReadBytesUnverified(8, ThenReadI64_D);
					}

					break;
				case 255:
					// Error state. Return -2.
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Packed, 1);
#endif
					cb = (Action<long>)Pop();
					cb(-2);
					break;
				default:
#if DEBUG && SOCKET_SERVER_PROBE_ON
					Validate(MetaFieldType.Packed, 1);
#endif
					cb = (Action<long>)Pop();
					cb(c);
					break;
			}

		}

		/// <summary>
		/// Avoid at all costs. It's only used for the MySQL server version during the DB handshake.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadNulString(Action<string> cb)
		{
			// Read bytes until we hit a NUL.
			Push(cb);
			Push(new List<byte>());
			Read(ThenCheckForNul_D);
		}

		private void ThenCheckForNul(byte charByte)
		{

			// Fortunately this only occurs during MySQL handshakes.
			List<byte> awkwardByteArray;

			if (charByte == 0)
			{
				awkwardByteArray = (List<byte>)Pop();

				var cb = (Action<string>)Pop();
				string result = System.Text.Encoding.UTF8.GetString(awkwardByteArray.ToArray());

#if DEBUG && SOCKET_SERVER_PROBE_ON
				Validate(MetaFieldType.NulString, (ulong)result.Length);
#endif

				cb(result);
				return;
			}

			// Add the byte to the pending array:
			awkwardByteArray = (List<byte>)Peek();
			awkwardByteArray.Add(charByte);


			Read(ThenCheckForNul_D);
		}

		/// <summary>
		/// Read a short.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadInt16(Action<short> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Signed, 2);
#endif

			ReadBytesUnverified(2, ThenReadInt16_D);
		}

		/// <summary>
		/// Read a 3 byte int.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadInt24(Action<int> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Signed, 3);
#endif

			ReadBytesUnverified(3, ThenReadInt24_D);
		}

		/// <summary>
		/// Read a nullable datetime (UTC).
		/// </summary>
		/// <param name="cb"></param>
		public void ReadDateTime(Action<DateTime?> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Date, 8);
#endif

			Read(ThenReadNDateTimeFlag_D);
		}

		private void ThenReadNDateTimeFlag(byte flag)
		{
			if (flag == 0)
			{
				var cb = (Action<DateTime?>)Pop();
				cb(null);
				return;
			}

			// Read a date
			ReadBytesUnverified(8, ThenReadNDateTime_D);
		}
		
		private void ThenReadNInt32Flag(byte flag)
		{
			if (flag == 0)
			{
				var cb = (Action<int?>)Pop();
				cb(null);
				return;
			}

			// Read a nullable int
			ReadBytesUnverified(4, ThenReadNInt32_D);
		}

		private void ThenReadNUInt32Flag(byte flag)
		{
			if (flag == 0)
			{
				var cb = (Action<uint?>)Pop();
				cb(null);
				return;
			}

			// Read a nullable int
			ReadBytesUnverified(4, ThenReadNUInt32_D);
		}

		/// <summary>
		/// Read a datetime (UTC).
		/// </summary>
		/// <param name="cb"></param>
		public void ReadDateTime(Action<DateTime> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Date, 8);
#endif

			ReadBytesUnverified(8, ThenReadDateTime_D);
		}

		/// <summary>
		/// Read an int.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadInt32(Action<int> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Signed, 4);
#endif

			ReadBytesUnverified(4, ThenReadInt32_D);
		}
		
		/// <summary>
		/// Read a nullable int.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadInt32(Action<int?> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Signed, 4);
#endif

			Read(ThenReadNInt32Flag_D);
		}

		/// <summary>
		/// Read a nullable uint.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadUInt32(Action<uint?> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 4);
#endif

			Read(ThenReadNUInt32Flag_D);
		}

		/// <summary>
		/// Read a long.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadInt64(Action<long> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Signed, 8);
#endif

			ReadBytesUnverified(8, ThenReadI64_D);
		}

		/// <summary>
		/// Read a ushort.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadUInt16(Action<ushort> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 2);
#endif

			ReadBytesUnverified(2, ThenReadUInt16_D);
		}

		/// <summary>
		/// Read a 3 byte uint.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadUInt24(Action<uint> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 3);
#endif

			ReadBytesUnverified(3, ThenReadUInt24_D);
		}

		/// <summary>
		/// Read a uint.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadUInt32(Action<uint> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 4);
#endif

			ReadBytesUnverified(4, ThenReadUInt32_D);
		}

		/// <summary>
		/// Read a 6 byte ulong.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadUInt48(Action<ulong> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 6);
#endif

			ReadBytesUnverified(6, ThenReadU48_D);
		}

		/// <summary>
		/// Read a ulong.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadUInt64(Action<ulong> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 8);
#endif

			ReadBytesUnverified(8, ThenReadU64_D);
		}

		/// <summary>
		/// Read a float.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadFloat(Action<float> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Float, 4);
#endif

			ReadBytesUnverified(4, ThenReadFloat_D);
		}

		/// <summary>
		/// Read a double.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadDouble(Action<double> cb)
		{
			Push(cb);

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Float, 8);
#endif

			ReadBytesUnverified(8, ThenReadDouble_D);
		}

#if DEBUG && SOCKET_SERVER_PROBE_ON
		
		/// <summary>
		/// Called at the end of validation.
		/// </summary>
		public void ValidateDone()
		{
			Validate(MetaFieldType.Done, 0);
		}

		/// <summary>
		/// Validates alignment using probe metadata.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="length"></param>
		protected void Validate(MetaFieldType type, ulong length)
		{
			if (ProbeMeta == null || ProbeMeta.LoadingProbeMessage)
			{
				return;
			}

			var msgRef = ProbeMeta.VerifiedTo >= ProbeMeta.FilledTo ? ProbeMetaEntry.Nothing : ProbeMeta.Messages[ProbeMeta.VerifiedTo++];

			// MUST match both type and length.
			if (type != msgRef.Type || length != msgRef.Length)
			{
				ProbeMeta.DebugThisNow(ProbeMeta.VerifiedTo - 1, type.ToString());

				throw new Exception("Alignment. The other end wrote a " + msgRef.Type + "(" + msgRef.Length + ") and you tried to read a " + type + "(" + length + ")");
			}
		}
#endif
		/// <summary>
		/// Internal - Reads a 2 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadUInt16(BufferSegment buffer)
		{
			var cb = (Action<ushort>)Pop();
			var value = (ushort)(buffer.Next | (buffer.Next << 8));
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 2 byte int from a buffer. 
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadInt16(BufferSegment buffer)
		{
			var cb = (Action<short>)Pop();
			var value = (short)(buffer.Next | (buffer.Next << 8));
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 3 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadUInt24(BufferSegment buffer)
		{
			var cb = (Action<uint>)Pop();
			var value = (uint)buffer.Next | ((uint)buffer.Next << 8) | ((uint)buffer.Next << 16);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 3 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadInt24(BufferSegment buffer)
		{
			var cb = (Action<int>)Pop();
			var value = (int)buffer.Next | ((int)buffer.Next << 8) | ((int)buffer.Next << 16);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		///  Internal - Reads a 4 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadUInt32(BufferSegment buffer)
		{
			var cb = (Action<uint>)Pop();
			var value = (uint)buffer.Next | ((uint)buffer.Next << 8) | ((uint)buffer.Next << 16) | ((uint)buffer.Next << 24);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a 4 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadInt32(BufferSegment buffer)
		{
			var cb = (Action<int>)Pop();
			var value = (int)buffer.Next | ((int)buffer.Next << 8) | ((int)buffer.Next << 16) | ((int)buffer.Next << 24);
			buffer.Release();
			cb(value);
		}
		
		/// <summary>
		/// Internal - Reads a nullable 4 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadNInt32(BufferSegment buffer)
		{
			var cb = (Action<int?>)Pop();
			var value = (int)buffer.Next | ((int)buffer.Next << 8) | ((int)buffer.Next << 16) | ((int)buffer.Next << 24);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Internal - Reads a nullable 4 byte int from a buffer.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenReadNUInt32(BufferSegment buffer)
		{
			var cb = (Action<uint?>)Pop();
			var value = (uint)buffer.Next | ((uint)buffer.Next << 8) | ((uint)buffer.Next << 16) | ((uint)buffer.Next << 24);
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Reads a compacted positive integer using the MySQL format. You're probably looking for ReadCompressed.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadPackedIntMySQL(Action<long> cb)
		{
			Push(cb);

			if (Index == DataLength)
			{
				// Wait for the first byte.
				ReadWait(ThenReadPackedIntSwitch_D);
			}
			else
			{
				// Read the first byte and pass to switch:
				BytesRead++;
				ThenReadPackedIntSwitch(Data[Index++]);
			}
		}

		/// <summary>
		/// Reads a compacted positive integer. Generally use this.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadCompressed(Action<ulong> cb)
		{
			Push(cb);

			if (Index == DataLength)
			{
				// Wait for the first byte.
				ReadWait(ThenReadPackedSwitch_D);
			}
			else
			{
				// Read the first byte and pass to switch:
				BytesRead++;
				ThenReadPackedSwitch(Data[Index++]);
			}
		}

		/// <summary>
		/// Slightly more specialised callback used when skipping bytes. The buffer given here is often actually junk.
		/// </summary>
		/// <param name="val"></param>
		private void ThenSkipBytes(BufferSegment val)
		{
			var cb = (Action)Pop();
			cb();
		}

		/// <summary>
		/// Skips the given number of bytes. Using this will fail validation on most links.
		/// </summary>
		/// <param name="count"></param>
		/// <param name="cb"></param>
		public void SkipBytes(int count, Action cb)
		{

			BytesRead += count;

			if ((Index + count) <= DataLength)
			{
				// Can respond instantly:
				Index += count;
				cb();
			}
			else if (Index == DataLength)
			{
				// Must wait for all of the data.
				Push(cb);
				PendingCallback = ThenSkipBytes_D;
				PendingBufferIndex = 0;
				WaitingFor = count;
			}
			else
			{
				// Must wait for some of the data.
				Push(cb);
				PendingCallback = ThenSkipBytes_D;
				FirstPendingBuffer = null;
				LastPendingBuffer = null;

				var bytesAvailable = DataLength - Index;
				WaitingFor = count - bytesAvailable;
				PendingBufferIndex = bytesAvailable;
			}
		}

		/// <summary>
		/// Reads an array of buffer segments, invoking the onEntry callback for each one.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadStringList(Action<List<string>> cb)
		{
			Push(cb);
			ReadCompressed(ThenReadXListItems_D);
		}

		/// <summary>
		/// A currently building list of str's.
		/// </summary>
		private List<string> CurrentStringList;

		/// <summary>
		/// Then reads the given number of items in an list of strings.
		/// </summary>
		/// <param name="length"></param>
		public void ThenReadXListItems(ulong length)
		{
			// Length is offset by 1. 0 indicates null, 1 is empty set.
			ArrayCount1 = length - 1;
			CurrentStringList = null;

			if (length <= 1)
			{
				var cb = (Action<List<string>>)Pop(); // cb
				cb(CurrentStringList);
				return;
			}

			ReadBytes(ThenReadStringListItem_D);
		}
		
		/// <summary>
		/// Reads an array of buffer segments, invoking the onEntry callback for each one.
		/// </summary>
		/// <param name="onEntry"></param>
		/// <param name="cb"></param>
		public void ReadBufferSegmentArray(Action<BufferSegment> onEntry, Action cb)
		{
			Push(cb);
			Push(onEntry);
			ReadCompressed(ThenReadXItems_D);
		}

		/// <summary>
		/// Array remaining counter
		/// </summary>
		private ulong ArrayCount1;

		/// <summary>
		/// Then reads the given number of items in an array.
		/// </summary>
		/// <param name="length"></param>
		public void ThenReadXItems(ulong length)
		{
			ArrayCount1 = length;

			if (length == 0)
			{
				Pop(); // entry callback
				var cb = (Action)Pop(); // cb
				cb();
				return;
			}

			ReadBytes(ThenReadBufferArrayItem_D);
		}

		/// <summary>
		/// Read a buffer item in an array.
		/// </summary>
		public void ThenReadStringListItem(BufferSegment bs)
		{
			var str = bs.GetStringUTF16();

			if (CurrentStringList == null)
			{
				CurrentStringList = new List<string>();
			}

			CurrentStringList.Add(str);
			ArrayCount1--;

			if (ArrayCount1 <= 0)
			{
				// Done!
				var cb = (Action<List<string>>)Pop(); // cb
				var list = CurrentStringList;
				cb(list);
				return;
			}

			// Otherwise, we'll read another item.
			ReadBytes(ThenReadStringListItem_D);
		}
		
		/// <summary>
		/// Read a buffer item in an array.
		/// </summary>
		public void ThenReadBufferArrayItem(BufferSegment bs)
		{
			// Run the callback:
			var entryCb = (Action<BufferSegment>)Peek();
			entryCb(bs);
			ArrayCount1--;

			if (ArrayCount1 <= 0)
			{
				// Done!
				Pop(); // entry callback
				var cb = (Action)Pop(); // cb
				cb();
				return;
			}

			// Otherwise, we'll read another item.
			ReadBytes(ThenReadBufferArrayItem_D);
		}

		/// <summary>
		/// Reads an unknown length block of bytes.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadBytes(Action<BufferSegment> cb)
		{
			// Read compressed length first:
			Push(cb);
			ReadCompressed(ThenReadXBytes_D);
		}

		/// <summary>
		/// Then reads the given number of bytes.
		/// </summary>
		/// <param name="length"></param>
		public void ThenReadXBytes(ulong length)
		{
			var cb = (Action<BufferSegment>)Pop();

			if (length == 0)
			{
				// it's null:
				cb(new BufferSegment() {
					Length = -1
				});
				return;
			}

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Buffer, (ulong)length);
#endif
			ReadBytes((int)(length - 1), cb);
		}

		/// <summary>
		/// Reads count bytes and provides them as a Buffer to the given callback. This verifies that MetaType.Bytes was written.
		/// </summary>
		/// <param name="count"></param>
		/// <param name="cb"></param>
		public void ReadBytes(int count, Action<BufferSegment> cb)
		{

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Bytes, (ulong)count);
#endif

			BytesRead += count;

			if ((Index + count) <= DataLength)
			{
				// Can respond instantly:
				Index += count;
				cb(new BufferSegment()
				{ // (struct)
					FirstBuffer = DataContainer,
					CurrentBuffer = DataContainer,
					LastBuffer = DataContainer,
					Offset = Index - count,
					Length = count,
					IsCopy = false
				});
			}
			else if (Index == DataLength)
			{
				// Must wait for all of the data.
				PendingCallback = cb;
				PendingBufferIndex = 0;
				WaitingFor = count;
			}
			else
			{
				// Must wait for some of the data. Copy required here:
				PendingCallback = cb;
				FirstPendingBuffer = BinaryBufferPool.Get();
				FirstPendingBuffer.Offset = 0;
				var bytesAvailable = DataLength - Index;
				LastPendingBuffer = FirstPendingBuffer.CopyFrom(Data, Index, bytesAvailable);
				WaitingFor = count - bytesAvailable;
				PendingBufferIndex = bytesAvailable;
			}
		}

		/// <summary>
		///  Reads count bytes and provides them as a Buffer to the given callback.
		/// </summary>
		/// <param name="count"></param>
		/// <param name="cb"></param>
		private void ReadBytesUnverified(int count, Action<BufferSegment> cb)
		{

			BytesRead += count;

			if ((Index + count) <= DataLength)
			{
				// Can respond instantly:
				Index += count;
				cb(new BufferSegment()
				{ // (struct)
					FirstBuffer = DataContainer,
					CurrentBuffer = DataContainer,
					LastBuffer = DataContainer,
					Offset = Index - count,
					Length = count,
					IsCopy = false
				});
			}
			else if (Index == DataLength)
			{
				// Must wait for all of the data.
				PendingCallback = cb;
				PendingBufferIndex = 0;
				WaitingFor = count;
			}
			else
			{
				// Must wait for some of the data. Copy required here:
				PendingCallback = cb;
				FirstPendingBuffer = BinaryBufferPool.Get();
				FirstPendingBuffer.Offset = 0;
				var bytesAvailable = DataLength - Index;
				LastPendingBuffer = FirstPendingBuffer.CopyFrom(Data, Index, bytesAvailable);
				WaitingFor = count - bytesAvailable;
				PendingBufferIndex = bytesAvailable;
			}
		}

		/// <summary>
		/// Internal; reads bytes for a buffer when a length is now known.
		/// </summary>
		/// <param name="length"></param>
		private void ThenReadBufferLong(long length)
		{
			var cb = (Action<BufferSegment>)Pop();
			ReadBytesUnverified((int)length, cb);
		}

		/// <summary>
		/// Internal; converts given buffer to a MySQL string.
		/// </summary>
		/// <param name="buffer"></param>
		private void ThenToMySQLString(BufferSegment buffer)
		{
			var cb = (Action<string>)Pop();
			// Only used during startup.
			var value = new System.Text.StringBuilder(buffer.Length);
			for (var i = 0; i < buffer.Length; i++)
			{
				value.Append((char)buffer.Next);
			}
			buffer.Release();
			cb(value.ToString());
		}

		// NOTE: Use ReadBuffer to read a string. Strings are UTF8 encoded but contain a 4 byte char count at the start.

		/// <summary>
		/// Reads a length and then that many bytes, and provides the result to the given callback as a string.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadStringMySQL(Action<string> cb)
		{
			Push(cb);
			Push(ThenToMySQLString_D);
			ReadPackedIntMySQL(ThenReadBufferLong_D);
		}

		/// <summary>
		/// Reads a MySQL string, and provides the result to the given callback as a string.
		/// </summary>
		/// <param name="length"></param>
		/// <param name="cb"></param>
		public void ReadStringMySQL(int length, Action<string> cb)
		{
			Push(cb);
			ReadBytesUnverified(length, ThenToMySQLString_D);
		}

		/// <summary>
		/// Reads a single byte after waiting.
		/// </summary>
		/// <param name="cb"></param>
		public void ReadWait(Action<byte> cb)
		{
			BytesRead++;
			// Wait for sure:
			Push(cb);
			PendingCallback = ThenReadSingleByte_D;
			WaitingFor = 1;
		}

		/// <summary>
		/// Internal. A callback which just runs another callback using the 0th byte from the given buffer.
		/// </summary>
		private void ThenReadSingleByte(BufferSegment buffer)
		{
			var cb = (Action<byte>)Pop();
			var value = buffer.Next;
			buffer.Release();
			cb(value);
		}

		/// <summary>
		/// Reads a single byte and provides it to the given callback.
		/// </summary>
		public void Read(Action<byte> cb)
		{
			BytesRead++;

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Validate(MetaFieldType.Unsigned, 1);
#endif

			if (Index == DataLength)
			{
				// Wait for sure:
				PendingCallback = ThenReadSingleByte_D;
				Push(cb);
				WaitingFor = 1;
				return;
			}

			cb(Data[Index++]);
		}

	}
}