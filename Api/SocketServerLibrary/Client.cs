using Api.Contexts;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// A socket client.
	/// </summary>
	public class Client {

		/// <summary>
		/// An ID for this client assigned by the parent server on creation.
		/// </summary>
		public uint Id;

		/// <summary>The underlying socket.</summary>
		public Socket Socket;

		/// <summary>Args used when sending messages.</summary>
		private readonly SocketAsyncArgs AsyncArgs;
		
		/// <summary>True if hello is required.</summary>
		public bool Hello = true;

		/// <summary>
		/// Current byte index in the buffer.
		/// </summary>
		public int CurrentBufferPointer;

		/// <summary>
		/// First buffer in a chain of pending buffers.
		/// </summary>
		public BufferedBytes First;

		/// <summary>
		/// True if sending can happen. It's false (and sent messages will be queued) if e.g. the connection hasn't completed yet or a send is currently in progress.
		/// </summary>
		public bool CanProcessSend = true;

		/// <summary>
		/// Task to wait for before processing anything else, if it exists.
		/// </summary>
		public Task WaitForTaskBeforeReceive;

		/// <summary>
		/// Sets the context on this client.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public virtual ValueTask SetContext(Context context)
		{
			Context = context;
			return new ValueTask();
		}

		/// <summary>
		/// Last buffer in a chain of pending buffers.
		/// </summary>
		public BufferedBytes Last;

		/// <summary>
		/// Front of the send queue.
		/// </summary>
		private SendStackFrame FirstSendFrame;

		/// <summary>
		/// Back of the send queue.
		/// </summary>
		private SendStackFrame LastSendFrame;

		/// <summary>
		/// Scratch space whilst reading values from the stream.
		/// </summary>
		public BufferedBytes ScratchSpace;

		/// <summary>
		/// The server this reader is related to.
		/// </summary>
		public Server Server;

		/// <summary>
		/// The context of this client.
		/// </summary>
		public Context Context = new Context();

		/// <summary>
		/// # of bytes in the buffer.
		/// </summary>
		public int BytesInBuffer;

		/// <summary>Remote IP.</summary>
		public IPAddress IP
		{
			get
			{
				if (Socket == null)
				{
					return null;
				}

				return ((IPEndPoint)(Socket.RemoteEndPoint)).Address;
			}
		}
		
		/// <summary>
		/// Current amount of unprocessed bytes in the buffers.
		/// </summary>
		public int BytesAvailable {
			get
			{
				// When not in WS mode, BytesUntilWebsocketHeader is int.MaxValue.
				return BytesInBuffer < BytesUntilWebsocketHeader ? BytesInBuffer : BytesUntilWebsocketHeader;
			}
		}

		/// <summary>
		/// WS header used when sending out messages via a websocket. The header is sent when a queue starts being processed.
		/// </summary>
		private byte[] WebsocketHeader = new byte[10] { 2 | 128, 0, 0, 0, 0, 0, 0, 0, 0, 10 };


		/// <summary>
		/// Sets up the websocket header, and returns the number of bytes that should be sent.
		/// </summary>
		/// <param name="length"></param>
		public int SetupWebsocketHeader(long length)
		{
			// Payload length with mask bit (highest bit).
			if (length <= 125)
			{
				WebsocketHeader[1] = (byte)length;
				return 2;
			}
			else if (length <= ushort.MaxValue)
			{
				WebsocketHeader[1] = 126;
				WebsocketHeader[2] = (byte)((length >> 8) & 255);
				WebsocketHeader[3] = (byte)(length & 255);
				return 4;
			}

			WebsocketHeader[1] = 127;
			WebsocketHeader[2] = (byte)((length >> 56) & 255);
			WebsocketHeader[3] = (byte)((length >> 48) & 255);
			WebsocketHeader[4] = (byte)((length >> 40) & 255);
			WebsocketHeader[5] = (byte)((length >> 32) & 255);
			WebsocketHeader[6] = (byte)((length >> 24) & 255);
			WebsocketHeader[7] = (byte)((length >> 16) & 255);
			WebsocketHeader[8] = (byte)((length >> 8) & 255);
			WebsocketHeader[9] = (byte)(length & 255);
			return 10;
		}

		/// <summary>
		/// Current index of the top of the recv stack. The stack always has an opcode frame on it, so the pointer starts at 0.
		/// </summary>
		public int RecvStackPointer = 0;

		/// <summary>
		/// The stack of things being received.
		/// </summary>
		public RecvStackFrame[] RecvStack = new RecvStackFrame[5]; // [0] is always an opcode frame.

		/// <summary>
		/// Websocket mask info used if in websocket mode.
		/// </summary>
		public byte[] WebsocketMask = null;

		/// <summary>
		/// Current # of bytes until next WS header.
		/// </summary>
		public int BytesUntilWebsocketHeader = int.MaxValue;

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

		/// <summary>
		/// Pops from the receive stack, excluding websocket header readers.
		/// </summary>
		public void Pop()
		{
			if (RecvStackPointer == 0)
			{
				return;
			}

			if (RecvStack[RecvStackPointer].Reader is WsHeaderReader)
			{
				// Relocate top of stack over the one below it.
				RecvStack[RecvStackPointer - 1] = RecvStack[RecvStackPointer];
			}

			RecvStackPointer--;
		}
		
		/// <summary>
		/// Pops from the receive stack.
		/// </summary>
		public void PopIgnoreWsHeader()
		{
			if (RecvStackPointer == 0)
			{
				return;
			}
			RecvStackPointer--;
		}

		/// <summary>
		/// Sends the given writer contents.
		/// </summary>
		/// <param name="writer"></param>
		public bool Send(Writer writer)
		{
			// Add the writer to the send queue:

			// If not actively sending, send now.

			if (Socket == null)
			{
				// Nope! This connection is unavailable.
				return false;
			}

			// Add the writer to the queue:
			writer.StartSending();

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
				if (WebsocketMask != null)
				{
					// Must send WS header first.
					// This is identified in CompletedCurrent via the Current buffer being null.
					var headerSize = SetupWebsocketHeader(writer.Length);
					AsyncArgs.SetBuffer(WebsocketHeader, 0, headerSize);

					if (Socket.SendAsync(AsyncArgs))
					{
						return true;
					}

					// It completed immediately. Allow the following to happen.

				}
				
				frame.Current = writer.FirstBuffer;

				var buffer = frame.Current;
				AsyncArgs.SetBuffer(buffer.Bytes, buffer.Offset, buffer.Length);

				if (!Socket.SendAsync(AsyncArgs))
				{
					// It completed immediately
					CompletedCurrentSend();
				}
			}

			return true;
		}

		/// <summary>
		/// Shuts down the socket that this sendqueue is associated to.
		/// </summary>
		public virtual void Close()
		{
			// Destroy the link:
			if (Socket != null)
			{
				try
				{
					lock (this)
					{
						if (Socket != null)
						{
							Socket.Shutdown(SocketShutdown.Both);
							Socket.Close();
							Socket = null;
						}
					}
				}
				catch {
				}
			}
		}
		
		/// <summary>
		/// Called when the current send operation has completed.
		/// Proceeds to send the next thing in the queue.
		/// </summary>
		public void CompletedCurrentSend()
		{
			if (Socket == null)
			{
				return;
			}

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
						// Need to consider sending a websocket header.
						if (WebsocketMask != null)
						{
							// We'll send a header for the whole queue:
							var stackFrame = frame.After;
							var totalSize = frame.Writer.Length;

							// Any following frames have their current set now to indicate that they have been included in a WS header.
							while (stackFrame != null)
							{
								totalSize += stackFrame.Writer.Length;
								stackFrame.Current = stackFrame.Writer.FirstBuffer;
								stackFrame = stackFrame.After;
							}

							var headerSize = SetupWebsocketHeader(totalSize);
							AsyncArgs.SetBuffer(WebsocketHeader, 0, headerSize);

							if (Socket == null || Socket.SendAsync(AsyncArgs))
							{
								return;
							}
						}

						// WS header sent or wasn't needed. Set the current to the first buffer:
						frame.Current = frame.Writer.FirstBuffer;

						if (frame.Current == null)
						{
							Console.WriteLine("Writer appears to have been released prematurely. " + frame.Writer.SendQueueCount);
							Close();
							return;
						}

					}

				}

				// Process next piece.
				var buffer = frame.Current;
				AsyncArgs.SetBuffer(buffer.Bytes, buffer.Offset, buffer.Length);

				if (Socket == null || Socket.SendAsync(AsyncArgs))
				{
					return;
				}
			}
		}

		/// <summary>
		/// Next byte without the WS mask.
		/// </summary>
		/// <returns></returns>
		public byte NextNoMask()
		{
			var result = First.Bytes[CurrentBufferPointer];
			CurrentBufferPointer++;
			BytesInBuffer--;

			if (CurrentBufferPointer == First.Length)
			{
				// We've read everything from this buffer. Can now release it.
				CurrentBufferPointer = 0;
				var next = First.After;
				First.Offset = 0;
				First.After = null;
				First.Release();
				First = next;

				if (next == null)
				{
					// Clear last as well. This read buffer is completely empty.
					Last = null;
				}
			}

			return result;
		}

		/// <summary>
		/// Transfers a block of bytes
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="intoWriter"></param>
		public void BlockTransfer(int bytes, Writer intoWriter)
		{
			for (var i = 0; i < bytes; i++)
			{
				intoWriter.Write(Next());
			}
		}

		/// <summary>
		/// Gets the next byte in the current buffer.
		/// </summary>
		/// <returns></returns>
		public byte Next()
		{
			var result = First.Bytes[CurrentBufferPointer];
			CurrentBufferPointer++;
			BytesInBuffer--;

			if (CurrentBufferPointer == First.Length)
			{
				// We've read everything from this buffer. Can now release it.
				CurrentBufferPointer = 0;
				var next = First.After;
				First.Offset = 0;
				First.After = null;
				First.Release();
				First = next;

				if (next == null)
				{
					// Clear last as well. This read buffer is completely empty.
					Last = null;
				}

			}

			if (WebsocketMask != null)
			{
				BytesUntilWebsocketHeader--;
				result ^= WebsocketMask[WebsocketMaskIndex++];
				if (WebsocketMaskIndex == 4)
				{
					WebsocketMaskIndex = 0;
				}

				if (BytesUntilWebsocketHeader == 0)
				{
					// Push a header reader onto the stack.
					RecvStackPointer++;
					RecvStack[RecvStackPointer] = new RecvStackFrame()
					{
						Reader = WsHeaderReader.Instance,
						Phase = 0,
						BytesRequired = 2
					};
				}

			}

			return result;
		}

		/// <summary>
		/// Creates a new client.
		/// </summary>
		public Client()
		{
			AsyncArgs = new SocketAsyncArgs();
			AsyncArgs.Client = this;

			RecvStack[0] = new RecvStackFrame() {
				Reader = OpCodeReader.Instance,
				Phase = 0,
				BytesRequired = 1
			};
		}

		/// <summary>Start listening for data.</summary>
		public virtual void Start()
		{
			CanProcessSend = true;
			DoReceive();
		}

		/// <summary>
		/// Receive the next frame. Never call this directly except in very specific circumstances.
		/// </summary>
		private void DoReceive() {

			if (Socket == null)
			{
				// Receive closed.
				return;
			}

			if (Last == null || Last.Offset == Last.Length)
			{
				// Buffer is full, or there isn't one.
				// Get another buffer.
				var next = BinaryBufferPool.OneKb.Get();
				next.After = null;

				if (Last == null)
				{
					First = next;
				}
				else
				{
					Last.After = next;
				}

				Last = next;
			}

			try
			{
				Socket.BeginReceive(Last.Bytes, Last.Offset, Last.Length - Last.Offset, 0, OnReceiveData, this);
			}
			catch(Exception e)
			{
				Console.WriteLine(e.ToString());
				Close();
			}
		}

		/// <summary>
		/// Used for handling all data.
		/// </summary>
		private void OnReceiveData(IAsyncResult ar)
		{
			int bytesRead;

			if (Socket == null)
			{
				Close();
				return;
			}

			try
			{
				bytesRead = Socket.EndReceive(ar);
			}
			catch
			{
				bytesRead = 0;
			}

			if (bytesRead == 0)
			{
				Close();
				return;
			}

			Last.Offset += bytesRead;
			BytesInBuffer += bytesRead;

			// Process the stack next.
			while (BytesInBuffer >= RecvStack[RecvStackPointer].BytesRequired)
			{
				if (Socket == null)
				{
					// Process requested the socket to close.
					return;
				}

				// Ask the top of stack to process the data:
				RecvStack[RecvStackPointer].Reader.Process(ref RecvStack[RecvStackPointer], this);

				if (WaitForTaskBeforeReceive != null && !WaitForTaskBeforeReceive.IsCompleted)
				{
					// The task will call TaskCompletedContinueReceive when it is done.
					return;
				}
			}

			// Receive again. If there is no more space, queue up another buffer.
			DoReceive();
		}

		/// <summary>
		/// If you set WaitForTaskBeforeReceive to true, call this when you're done in order to continue receiving the latest message.
		/// </summary>
		public void TaskCompletedContinueReceive()
		{
			if (WaitForTaskBeforeReceive == null)
			{
				// E.g. The task completed inline and we don't actually have to wait for it.
				return;
			}

			WaitForTaskBeforeReceive = null;

			while (BytesInBuffer >= RecvStack[RecvStackPointer].BytesRequired)
			{
				if (Socket == null)
				{
					// Process requested the socket to close.
					return;
				}

				// Ask the top of stack to process the data:
				RecvStack[RecvStackPointer].Reader.Process(ref RecvStack[RecvStackPointer], this);

				if (WaitForTaskBeforeReceive != null && !WaitForTaskBeforeReceive.IsCompleted)
				{
					// The task will call TaskCompletedContinueReceive when it is done. Halt and wait for it to call.
					return;
				}
			}

			// Receive again. If there is no more space, queue up another buffer.
			DoReceive();
		}

		/// <summary>
		/// Reads a bool.
		/// </summary>
		/// <returns></returns>
		public bool ReadBool()
		{
			return Next() == 1;
		}

		/// <summary>
		/// Reads a nullable bool
		/// </summary>
		/// <returns></returns>
		public bool? ReadNBool()
		{
			var b = Next();

			if (b == 0)
			{
				return null;
			}

			return b == 2;
		}

		/// <summary>
		/// Reads a byte.
		/// </summary>
		/// <returns></returns>
		public byte ReadByte()
		{
			return Next();
		}

		/// <summary>
		/// Reads an int16.
		/// </summary>
		/// <returns></returns>
		public short ReadInt16()
		{
			return (short)(Next() | (Next() << 8));
		}

		/// <summary>
		/// Reads a uint16.
		/// </summary>
		/// <returns></returns>
		public ushort ReadUInt16()
		{
			return (ushort)(Next() | (Next() << 8));
		}

		/// <summary>
		/// Reads an int32.
		/// </summary>
		/// <returns></returns>
		public int ReadInt32()
		{
			return (int)(Next()) | (int)(Next() << 8) | (int)(Next() << 16) | (int)(Next() << 24);
		}

		/// <summary>
		/// Reads a uint32.
		/// </summary>
		/// <returns></returns>
		public uint ReadUInt32()
		{
			return (uint)(Next()) | (uint)(Next() << 8) | (uint)(Next() << 16) | (uint)(Next() << 24);
		}

		/// <summary>
		/// Reads an int64.
		/// </summary>
		/// <returns></returns>
		public long ReadInt64()
		{
			return (long)Next() | ((long)Next() << 8) | ((long)Next() << 16) | ((long)Next() << 24) |
					((long)Next() << 32) | ((long)Next() << 40) | ((long)Next() << 48) | ((long)Next() << 56);
		}

		/// <summary>
		/// Reads a uint64.
		/// </summary>
		/// <returns></returns>
		public ulong ReadUInt64()
		{
			return (ulong)Next() | ((ulong)Next() << 8) | ((ulong)Next() << 16) | ((ulong)Next() << 24) |
					((ulong)Next() << 32) | ((ulong)Next() << 40) | ((ulong)Next() << 48) | ((ulong)Next() << 56);
		}

		/// <summary>
		/// Reads a 4 byte float.
		/// </summary>
		/// <returns></returns>
		public float ReadFloat()
		{
			var bitField = (uint)Next() | ((uint)Next() << 8) | ((uint)Next() << 16) | ((uint)Next() << 24);
			return new FloatBits(bitField).Float;
		}

		/// <summary>
		/// Reads an 8 byte double.
		/// </summary>
		/// <returns></returns>
		public double ReadDouble()
		{
			var bitField = (ulong)Next() | ((ulong)Next() << 8) | ((ulong)Next() << 16) | ((ulong)Next() << 24) |
					((ulong)Next() << 32) | ((ulong)Next() << 40) | ((ulong)Next() << 48) | ((ulong)Next() << 56);
			return new DoubleBits(bitField).Double;
		}

		/// <summary>
		/// Reads an 8 byte datetime.
		/// </summary>
		/// <returns></returns>
		public DateTime ReadDateTime()
		{
			var ticks = (long)Next() | ((long)Next() << 8) | ((long)Next() << 16) | ((long)Next() << 24) |
					((long)Next() << 32) | ((long)Next() << 40) | ((long)Next() << 48) | ((long)Next() << 56);
			return new DateTime(ticks);
		}

		/// <summary>
		/// Reads a nullable int16.
		/// </summary>
		/// <returns></returns>
		public short? ReadNInt16()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}
			
			return (short)(Next() | (Next() << 8));
		}

		/// <summary>
		/// Reads a nullable uint16.
		/// </summary>
		/// <returns></returns>
		public ushort? ReadNUInt16()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}

			return (ushort)(Next() | (Next() << 8));
		}

		/// <summary>
		/// Reads a nullable int32.
		/// </summary>
		/// <returns></returns>
		public int? ReadNInt32()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}

			return (int)(Next()) | (int)(Next() << 8) | (int)(Next() << 16) | (int)(Next() << 24);
		}

		/// <summary>
		/// Reads a nullable uint32.
		/// </summary>
		/// <returns></returns>
		public uint? ReadNUInt32()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}
			
			return (uint)(Next()) | (uint)(Next() << 8) | (uint)(Next() << 16) | (uint)(Next() << 24);
		}

		/// <summary>
		/// Reads a nullable int64.
		/// </summary>
		/// <returns></returns>
		public long? ReadNInt64()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}

			return (long)Next() | ((long)Next() << 8) | ((long)Next() << 16) | ((long)Next() << 24) |
					((long)Next() << 32) | ((long)Next() << 40) | ((long)Next() << 48) | ((long)Next() << 56);
		}

		/// <summary>
		/// Reads a nullable uint64.
		/// </summary>
		/// <returns></returns>
		public ulong? ReadNUInt64()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}

			return (ulong)Next() | ((ulong)Next() << 8) | ((ulong)Next() << 16) | ((ulong)Next() << 24) |
					((ulong)Next() << 32) | ((ulong)Next() << 40) | ((ulong)Next() << 48) | ((ulong)Next() << 56);
		}

		/// <summary>
		/// Reads a nullable 4 byte float.
		/// </summary>
		/// <returns></returns>
		public float? ReadNFloat()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}
			
			var bitField = (uint)Next() | ((uint)Next() << 8) | ((uint)Next() << 16) | ((uint)Next() << 24);
			return new FloatBits(bitField).Float;
		}

		/// <summary>
		/// Reads a nullable 8 byte double.
		/// </summary>
		/// <returns></returns>
		public double? ReadNDouble()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}
			
			var bitField = (ulong)Next() | ((ulong)Next() << 8) | ((ulong)Next() << 16) | ((ulong)Next() << 24) |
					((ulong)Next() << 32) | ((ulong)Next() << 40) | ((ulong)Next() << 48) | ((ulong)Next() << 56);
			return new DoubleBits(bitField).Double;
		}

		/// <summary>
		/// Reads a nullable 8 byte datetime.
		/// </summary>
		/// <returns></returns>
		public DateTime? ReadNDateTime()
		{
			var b = Next();
			if (b == 0)
			{
				return null;
			}

			var ticks = (long)Next() | ((long)Next() << 8) | ((long)Next() << 16) | ((long)Next() << 24) |
					((long)Next() << 32) | ((long)Next() << 40) | ((long)Next() << 48) | ((long)Next() << 56);
			return new DateTime(ticks);
		}

		/// <summary>
		/// Reads a UTF16 string, allocating it once.
		/// </summary>
		/// <returns></returns>
		public string ReadUTF16()
		{
			var buffer = ReadBytes();
			if (buffer == null)
			{
				return null;
			}
			var span = MemoryMarshal.Cast<byte, char>(buffer.AsSpan());
			return new string(span);
		}

		/// <summary>
		/// Reads a compressed number.
		/// </summary>
		/// <returns></returns>
		public ulong ReadCompressed()
		{
			var first = Next();
			switch (first)
			{
				case 251:
					// 2 bytes:
					return (ulong)(Next() | (Next() << 8));
				case 252:
					// 3 bytes:
					return (ulong)(Next() | (Next() << 8) | (Next() << 16));
				case 253:
					// 4 bytes:
					return (ulong)(Next() | (Next() << 8) | (Next() << 16) | (Next() << 24));
				case 254:
					// 8 bytes:
					return (ulong)Next() | ((ulong)Next() << 8) | ((ulong)Next() << 16) | ((ulong)Next() << 24) |
						((ulong)Next() << 32) | ((ulong)Next() << 40) | ((ulong)Next() << 48) | ((ulong)Next() << 56);
				default:
					return first;
			}
		}

		/// <summary>
		/// Reads a ustring.
		/// </summary>
		/// <returns></returns>
		public ustring ReadUString()
		{
			var buffer = ReadBytes();
			if (buffer == null)
			{
				return null;
			}
			return ustring.Make(buffer);
		}

		/// <summary>
		/// Reads a block of bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] ReadBytes()
		{
			var size = ReadCompressed();

			if (size == 0)
			{
				return null;
			}

			var alloc = new byte[(int)size - 1];
			for (var i = 0; i < alloc.Length; i++)
			{
				alloc[i] = Next();
			}

			return alloc;
		}

		/// <summary>
		/// Skips x bytes.
		/// </summary>
		/// <returns></returns>
		public void Skip(int x)
		{
			for (var i = 0; i < x; i++)
			{
				Next();
			}
		}

	}


}