using Api.Startup;
using Api.Startup.Utf8Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// A streamable message which writes to buffer pool.
	/// You must use GetPooled() and then a Start(..) overload to set it up.
	/// </summary>
	public class Writer
	{	
		/// <summary>
		/// Escape the given control char. The given control char value must be true for Unicode.IsControl.
		/// </summary>
		/// <param name="control"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] EscapedControl(byte control)
		{
			if (_controlMap == null)
			{
				GetControlMap();
			}

			return _controlMap[control];
		}

		/// <summary>
		/// Array of 128 pre-serialised control characters.
		/// </summary>
		private static byte[][] _controlMap;

		private static byte[][] GetControlMap()
		{
			if (_controlMap != null)
			{
				return _controlMap;
			}

			var map = new byte[160][];

			for (uint i = 0; i < 160; i++)
			{
				if (Startup.Utf8Helpers.Unicode.IsControl(i))
				{
					byte[] escapedChar;

					if (i == (int)'\r')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'r' };
					}
					else if (i == (int)'\n')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'n' };
					}
					else if (i == (int)'\t')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'t' };
					}
					else if (i == (int)'\b')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'b' };
					}
					else if (i == (int)'\f')
					{
						escapedChar = new byte[] { (byte)'\\', (byte)'f' };
					}
					else
					{
						// hex representation. "\u followed by 4 hex characters".
						escapedChar = new byte[6];
						escapedChar[0] = (byte)'\\';
						escapedChar[1] = (byte)'u';
						escapedChar[2] = (byte)'0';
						escapedChar[3] = (byte)'0';

						// Upper nibble:
						var n = (i >> 4);
						n += (n > 9u) ? 87u : 48u; // ('a'- 10) : '0';
						escapedChar[4] = (byte)n;

						// Lower nibble:
						n = (i & 15);
						n += (n > 9u) ? 87u : 48u; // ('a' - 10) : '0';
						escapedChar[5] = (byte)n;
					}

					map[i] = escapedChar;
				}
			}

			_controlMap = map;
			return map;
		}

		/// <summary>
		/// Don't call this - it's for a pooled writer to reset itself.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void PoolReset()
		{
			NextInLine = null;
			SendQueueCount = 0;
			ReleaseOnSent = false;
		}

		/// <summary>
		/// Gets a pooled writer.
		/// Generally use this rather than new Writer.
		/// </summary>
		public static Writer GetPooled()
		{
			// Uses the 1kb buffer size:
			return BinaryBufferPool.OneKb.GetWriter();
		}

		/// <summary>
		/// Starts sending this writer by increasing its send queue counter.
		/// </summary>
		public void StartSending()
		{
			lock (sendQueueCountLock)
			{
				SendQueueCount++;
				LastBuffer.Length = Fill;
			}
		}

		/// <summary>
		/// Removes this writer from a send queue.
		/// </summary>
		public void RemoveFromSendQueue()
		{
			var count = 0;

			lock (sendQueueCountLock)
			{
				SendQueueCount--;
				count = SendQueueCount;
			}
			
			if (count == 0 && ReleaseOnSent)
			{
				Release();
			}
		}

		/// <summary>
		/// Number of send queues this writer is currently in.
		/// Releasing the writer will wait for this number to go down to 0.
		/// </summary>
		public int SendQueueCount = 0;

		/// <summary>
		/// True if this writer should be released when it's done being sent.
		/// </summary>
		private bool ReleaseOnSent;

		/// <summary>
		/// Base length.
		/// </summary>
		public int BaseLength;
		private int _bufferCount=1;
		private int Fill;
		/// <summary>The linked list of buffers that form this message.</summary>
		public BufferedBytes FirstBuffer;
		/// <summary>The linked list of buffers that form this message.</summary>
		public BufferedBytes LastBuffer;
		/// <summary>Next writer in a queue after this one.</summary>
		public Writer NextInLine;
		private byte[] _LastBufferBytes;

		/// <summary>
		/// The number of buffers in this writer.
		/// </summary>
		public int BufferCount => _bufferCount;
		
		/// <summary>When using writers you must use a start method.</summary>
		public void Start(uint opcode){
			BaseLength = 0;
			_bufferCount = 1;
			Fill = 0;
			var buffer = Pool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;
			
			// Write the opcode:
			WriteCompressed(opcode);
		}
		
		/// <summary>When using writers you must use a start method.</summary>
		public void StartWithLength()
		{
			BaseLength = 0;
			_bufferCount = 1;
			Fill = 0;
			var buffer = Pool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;

			// Length and sequence:
			Fill += 4;

			// Make sure sequence is set to 0:
			_LastBufferBytes[3] = 0;
		}

		/// <summary>When using writers you must use a start method.</summary>
		public void Start(byte[] template)
		{
			BaseLength = 0;
			_bufferCount = 1;
			Fill = 0;
			var buffer = Pool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;

			if (template != null)
			{
				WriteNoLength(template);
			}
		}

		private readonly BinaryBufferPool Pool;

		/// <summary>
		/// Don't call directly - use aBinaryBufferPool.GetWriter(); instead. For the common 1kb buffer pool, use (static) Writer.GetPooled();
		/// That's the same as calling BinaryBufferPool.OneKb.GetWriter();
		/// </summary>
		/// <param name="pool"></param>
		public Writer(BinaryBufferPool pool)
		{
			Pool = pool;
		}

		/// <summary>
		/// Total fill including any headers.
		/// </summary>
		public int Length
		{
			get
			{
				return BaseLength + Fill;
			}
		}

		/// <summary>
		/// Length excluding pool headers.
		/// </summary>
		public int LengthWithoutHeaders
		{
			get
			{
				return BaseLength + Fill - (Pool.StartOffset * _bufferCount);
			}
		}

		/// <summary>
		/// Used when the current buffer is totally full and a new one is required.
		/// </summary>
		private void NextBuffer(){
			BaseLength += Fill;
			LastBuffer.Length = Fill;
			var buffer = Pool.Get();
			LastBuffer.After = buffer;
			LastBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;
			_bufferCount++;
			Fill = buffer.Offset; // Buffers may reserve some space at the start for headers.
		}

		/// <summary>
		/// The current Fill value.
		/// </summary>
		public int CurrentFill => Fill;

		/// <summary>Reset by clearing the internal buffers and calls Start(template) such that the writer is ready to be used again.</summary>
		public void Reset(byte[] template)
		{
			if (SendQueueCount > 0)
			{
				throw new Exception("Can't reset a writer that is still being actively sent");
			}
			
			SendQueueCount = 0;
			ReleaseOnSent = false;

			// Release all the buffers, if there are any:
			if (FirstBuffer != null)
			{
				lock (Pool.PoolLock)
				{
					LastBuffer.After = Pool.First;
					Pool.First = FirstBuffer;
				}
			}

			// Clear:
			LastBuffer = null;
			FirstBuffer = null;
			_LastBufferBytes = null;

			// Start again:
			Start(template);
		}

		private object sendQueueCountLock = new object();

		/// <summary>Called when a writer is no longer needed and should now be fully released.</summary>
		public void Release()
		{
			lock (sendQueueCountLock)
			{
				if (SendQueueCount > 0)
				{
					ReleaseOnSent = true;
					return;
				}
			}

			// Release all buffers back to the pool and pool the writer itself too.

			// Release all the buffers:
			if (FirstBuffer != null)
			{
				lock (Pool.PoolLock)
				{
					LastBuffer.After = Pool.First;
					Pool.First = FirstBuffer;
				}
			}

			LastBuffer = null;
			FirstBuffer = null;
			_LastBufferBytes = null;

			lock (Pool.WriterPoolLock)
			{
				// Shove into the pool:
				NextInLine = Pool.FirstCached;
				Pool.FirstCached = this;
			}
		}

		/// <summary>Gets the buffer and relative index for the given overall index.</summary>
		public int GetLocalIndex(int overallIndex, out BufferedBytes buffer){
			
			int currentOverallIndex = 0;
			
			buffer = FirstBuffer;
			
			while(buffer != null){
				int nextOverallIndex = currentOverallIndex + ((buffer == LastBuffer) ? Fill : buffer.Length);
				
				if(nextOverallIndex > overallIndex){
					// It's in this buffer:
					return overallIndex - currentOverallIndex;
				}
				
				buffer = buffer.After;
			}
			
			buffer = null;
			return -1;
		}

		/// <summary>Gets a CRC32 for the given range of data.</summary>
		public uint GetCrc32(int start, int length){
			LastBuffer.Length = Fill;
			var table = Crc32.GetTable();
			uint crc = Crc32.DefaultSeed;
			
			BufferedBytes currentBuffer;
			int bufferIndex = GetLocalIndex(start, out currentBuffer);
			byte[] currentBytes = currentBuffer.Bytes;
			int bufferLength = currentBuffer.Length;
			
			for(int i=0; i < length; i++){
				
				// Apply current byte:
				crc = (crc >> 8) ^ table[ (crc ^ currentBytes[bufferIndex++]) & 0xFF ];
				
				// Cycle the buffer if we need to:
				if(bufferIndex == bufferLength){
					
					currentBuffer = currentBuffer.After;

					if (currentBuffer == null)
					{
						// Happens if we're creating the CRC of the entire writer's content
						return crc;
					}

					currentBytes = currentBuffer.Bytes;
					bufferLength = currentBuffer.Length;
					bufferIndex = 0;
					
				}
				
			}
			
			return crc;
		}

		/// <summary>
		/// Writes this data to the given file stream.
		/// </summary>
		/// <param name="fs"></param>
		public void WriteTo(System.IO.FileStream fs)
		{
			var buffer = FirstBuffer;

			for (var i = _bufferCount-1; i >=0; i--)
			{
				if (i == 0)
				{
					// Use fill here:
					fs.Write(buffer.Bytes, 0, Fill);
				}
				else
				{
					// Use its length:
					fs.Write(buffer.Bytes, 0, buffer.Length);
				}

				buffer = buffer.After;
			}
		}

		/// <summary>
		/// Sets up the internal buffers with the given info
		/// </summary>
		/// <param name="blockCount"></param>
		/// <param name="totalLength"></param>
		/// <param name="first"></param>
		/// <param name="last"></param>
		public void Start(int blockCount, int totalLength, BufferedBytes first, BufferedBytes last)
		{
			_bufferCount = blockCount;
			FirstBuffer = first;
			LastBuffer = last;
			Fill = last.Length;
			BaseLength = totalLength - Fill;
			_LastBufferBytes = last.Bytes;
		}

		/// <summary>
		///  Writes a bool to this writer.
		/// </summary>
		/// <param name="value"></param>
		public void Write(bool value)
		{
			Write((byte)(value ? 1 : 0));
		}

		/// <summary>
		/// Writes an array of strings to the writer. Each one is written as a UTF16.
		/// </summary>
		/// <param name="value"></param>
		public void Write(List<string> value)
		{
			if (value == null)
			{
				WriteCompressed(0);
			}
			else
			{
				WriteCompressed((ulong)(value.Count + 1));

				foreach (var val in value)
				{
					WriteUTF16(val);
				}
			}
		}

		/// <summary>Write a single byte to the message. Write a block of bytes instead of using this if you can.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(byte val){
			if(Fill == Pool.BufferSize){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=val;
		}

		/// <summary>Write a 2 byte unsigned value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ushort value){
			if(Fill > (Pool.BufferSize-2)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
		}

		/// <summary>Write a 2 byte unsigned value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteBE(ushort value)
		{
			if (Fill > (Pool.BufferSize - 2))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)value;
		}

		/*
		/// <summary>Writes a signature (as a regular buffer, with the length before it).</summary>
		public void Sign(string message){
			var block = BinaryBufferPool.Get();
			Crypto.Signer.Sign(message, block.Bytes, 0, Crypto.Keypair.ServerKey.Parameters);
			
			// It's 64 bytes long - write that many bytes now:
			Write((byte)64);
			
			Write(block.Bytes, 0, 64);
			
			// And release the block:
			block.Release();
		}
		*/

		/// <summary>
		///  Writes a nullable bool to this writer.
		/// </summary>
		/// <param name="value"></param>
		public void Write(bool? value)
		{
			if (value == null)
			{
				Write((byte)0);
			}
			else
			{
				Write((byte)(value.Value ? 2 : 1));
			}
		}
		
		/// <summary>Write a 2 byte signed value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(short value){
			if(Fill > (Pool.BufferSize-2)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
		}

		/// <summary>Write a 4 byte unsigned value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(uint value){
			if(Fill > (Pool.BufferSize-4)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
		}
		
		/// <summary>Write a 4 byte unsigned value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteBE(uint value){
			if(Fill > (Pool.BufferSize-4)){
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++]=(byte)value;
		}

		/// <summary>
		/// Write a nullable int16 value.
		/// </summary>
		/// <param name="val"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(short? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}

		/// <summary>
		/// Write a nullable int32 value.
		/// </summary>
		/// <param name="val"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(int? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}

		/// <summary>
		/// Write a nullable uint32 value.
		/// </summary>
		/// <param name="val"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(uint? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}

		/// <summary>
		/// Write a nullable uint16 value.
		/// </summary>
		/// <param name="val"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ushort? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}

		/// <summary>Write a 4 byte signed value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(int value){
			if(Fill > (Pool.BufferSize-4)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
		}

		/// <summary>Write a 4 byte unsigned value to the message. Big endian.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteUInt24BE(uint value)
		{
			if (Fill > (Pool.BufferSize - 3))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)value;
		}

		/// <summary>Write a 4 byte unsigned value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteUInt24(uint value){
			if(Fill > (Pool.BufferSize-3)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
		}

		/// <summary>Write a 3 byte signed value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteInt24(int value){
			if(Fill > (Pool.BufferSize-3)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
		}

		/// <summary>Write a float value.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(float val){
			if(Fill > (Pool.BufferSize-4)){
				NextBuffer();
			}
			
			uint value = new FloatBits(val).Int;
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
		}

		/// <summary>Write a float value.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteBE(float val)
		{
			if (Fill > (Pool.BufferSize - 4))
			{
				NextBuffer();
			}

			uint value = new FloatBits(val).Int;
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)value;
		}

		/// <summary>Write a double value.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(double val)
		{
			if (Fill > (Pool.BufferSize - 8))
			{
				NextBuffer();
			}

			ulong value = new DoubleBits(val).Int;
			_LastBufferBytes[Fill++] = (byte)value;
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
			_LastBufferBytes[Fill++] = (byte)(value >> 40);
			_LastBufferBytes[Fill++] = (byte)(value >> 48);
			_LastBufferBytes[Fill++] = (byte)(value >> 56);
		}

		/// <summary>Write a double value.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteBE(double val)
		{
			if (Fill > (Pool.BufferSize - 8))
			{
				NextBuffer();
			}

			ulong value = new DoubleBits(val).Int;
			_LastBufferBytes[Fill++] = (byte)(value >> 56);
			_LastBufferBytes[Fill++] = (byte)(value >> 48);
			_LastBufferBytes[Fill++] = (byte)(value >> 40);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)value;
		}

		/// <summary>Write a date value.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(DateTime val)
		{
			if (Fill > (Pool.BufferSize - 8))
			{
				NextBuffer();
			}

			var value = val.Ticks;
			_LastBufferBytes[Fill++] = (byte)value;
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
			_LastBufferBytes[Fill++] = (byte)(value >> 40);
			_LastBufferBytes[Fill++] = (byte)(value >> 48);
			_LastBufferBytes[Fill++] = (byte)(value >> 56);
		}

		/// <summary>Write a float value.</summary>
		public void Write(float? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}

		/// <summary>Write a double value.</summary>
		public void Write(double? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}
		
		/// <summary>
		/// Write a nullable date value.
		/// </summary>
		/// <param name="val"></param>
		public void Write(DateTime? val)
		{
			if (val == null)
			{
				Write((byte)0);
				return;
			}
			Write((byte)1);
			Write(val.Value);
		}

		/// <summary>
		/// Writes the given short ascii string to this writer.
		/// </summary>
		/// <param name="str"></param>
		public void WriteASCII(string str)
		{
			if (Fill > (Pool.BufferSize - str.Length))
			{
				NextBuffer();
			}

			for (var i = 0; i < str.Length; i++)
			{
				_LastBufferBytes[Fill++] = (byte)str[i];
			}
		}

		/// <summary>
		/// Writes a series of bytes from the given buffer as 2 letters. Somewhat like hex, but only uses letters.
		/// a = 0, b = 1, c = 2 etc.
		/// </summary>
		/// <param name="srcBuffer"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		public void WriteAlphaChar(byte[] srcBuffer, int offset, int length)
		{
			if (Fill > (Pool.BufferSize - (length * 2)))
			{
				NextBuffer();
			}

			var target = _LastBufferBytes;
			
			for (var i = 0; i < length; i++)
			{
				var value = srcBuffer[offset + i];
				target[Fill++] = (byte)((value & 15) + 97);
				target[Fill++] = (byte)((value >> 4) + 97);
			}
		}

		/// <summary>
		/// Writes the given number out textually, allocates. Avoid unless necessary.
		/// </summary>
		/// <param name="f"></param>
		public void WriteS(float f)
		{
			// TODO: Replace this (+decimal and double) with a copy from the internal method to avoid the str alloc:
			// https://github.com/dotnet/runtime/blob/927b1c54956ddb031a2e1a3eddb94ccc16004c27/src/libraries/System.Private.CoreLib/src/System/Number.Formatting.cs#L520
			WriteS(f.ToString());
		}

		/// <summary>
		/// Writes the given number out textually, allocates. Avoid unless necessary.
		/// </summary>
		/// <param name="f"></param>
		public void WriteS(decimal f)
		{
			WriteS(f.ToString());
		}

		/// <summary>
		/// Writes the given number out textually, allocates. Avoid unless necessary.
		/// </summary>
		/// <param name="d"></param>
		public void WriteS(double d)
		{
			WriteS(d.ToString());
		}

		/// <summary>
		/// Writes the given number out textually without allocating. Avoid unless necessary.
		/// </summary>
		/// <param name="n"></param>
		public void WriteS(uint n)
		{
			if (n == 0)
			{
				Write((byte)'0');
				return;
			}

			// Build up the output in this stack frame:
			Span<byte> outputStr = stackalloc byte[10];
			var index = 9;
			
			do
			{
				uint quotient = n / 10;
				outputStr[index--] = (byte)('0' + (n - (quotient * 10)));
				n = quotient;
			}
			while (n != 0);

			Write(outputStr, index + 1, 9 - index);
		}

		/// <summary>
		/// The bytes for "null"
		/// </summary>
		public static readonly byte[] NullBytes = new byte[] { 110, 117, 108, 108 };
		
		/// <summary>
		/// Writes an escaped string surrounded in ", utf-8 encoded. Avoid whenever possible (used by e.g. JSON serialisation).
		/// </summary>
		/// <param name="str"></param>
		public void WriteEscaped(ustring str)
		{
			if (str == null)
			{
				Write(NullBytes, 0, 4);
				return;
			}

			Write((byte)'"');
			Write((byte)'"');
			throw new NotSupportedException();
		}

		/// <summary>
		/// Writes an escaped string surrounded in ", utf-8 encoded. Avoid whenever possible (used by e.g. JSON serialisation).
		/// </summary>
		/// <param name="str"></param>
		public void WriteEscaped(string str)
		{
			if (str == null)
			{
				Write(NullBytes, 0, 4);
				return;
			}

			Write((byte)'"');

			// Grab a reference to the char span:
			var charStream = str.AsSpan();
			var max = charStream.Length;

			for (var i = 0; i < max; i++)
			{
				var current = charStream[i];
				uint rune;
				int runeBytesInUtf8;

				if (char.IsHighSurrogate(current))
				{
					i++;

					if (i != max)
					{
						var low = charStream[i];

						if (char.IsLowSurrogate(low))
						{
							// Remap the codepoint.
							rune = (uint)char.ConvertToUtf32(current, low);

							// Note: don't ever get escape-worthy chars here. Always outputted as-is.
							runeBytesInUtf8 = Utf8.RuneLen(rune);

							if (Fill > (Pool.BufferSize - runeBytesInUtf8))
							{
								NextBuffer();
							}

							Utf8.EncodeRune(rune, _LastBufferBytes, Fill);
							Fill += runeBytesInUtf8;
						}
					}

					continue;
				}
				
				// Codepoint is as-is:
				rune = (uint)current;

				// If it's a control character or any of the escapee's..
				if (Unicode.IsControl(rune))
				{
					var ctrl = EscapedControl((byte)rune);
					if (ctrl != null)
					{
						WriteNoLength(ctrl);
					}
					continue;
				}
				else if (rune == (uint)'"' || rune == (uint)'\\')
				{
					Write((byte)'\\');
				}

				// output the character:
				runeBytesInUtf8 = Utf8.RuneLen(rune);

				if (Fill > (Pool.BufferSize - runeBytesInUtf8))
				{
					NextBuffer();
				}

				Utf8.EncodeRune(rune, _LastBufferBytes, Fill);
				Fill += runeBytesInUtf8;

			}
			
			Write((byte)'"');
		}
		/// <summary>
		/// Writes the given date as the number of milliseconds from year 0, UTC. Negative values are permitted, although C# DateTime doesn't support BC anyway.
		/// The JS epoch is in 1970, so a constant offset (62135596800000) can be applied to quickly convert to a JS date unambiguously.
		/// </summary>
		/// <param name="date"></param>
		public void WriteS(DateTime date)
		{
			// Unix epoch constant: new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).Ticks / 10000
			WriteS((ulong)(date.Ticks / 10000));
		}

		/// <summary>
		/// Writes the given number out textually without allocating. Avoid unless necessary.
		/// </summary>
		/// <param name="n"></param>
		public void WriteS(int n)
		{
			if (n >= 0)
			{
				WriteS((uint)n);
				return;
			}

			// It's negative.
			Write((byte)'-');
			WriteS((uint)-n);
		}

		/// <summary>
		/// Ulong mod 1e9
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static uint Int64DivMod1E9(ref ulong value)
		{
			uint rem = (uint)(value % 1000000000);
			value /= 1000000000;
			return rem;
		}

		/// <summary>
		/// Writes the given number out textually without allocating. Avoid unless necessary.
		/// </summary>
		/// <param name="n"></param>
		public void WriteS(ulong n)
		{
			if (n == 0)
			{
				Write((byte)'0');
				return;
			}

			// Build up the output in this stack frame:
			Span<byte> outputStr = stackalloc byte[20];
			var index = 19;

			var digits = 1;

			while (((n & 0xFFFFFFFF00000000) >> 32) != 0)
			{
                UInt32ToDecChars(ref index, outputStr, Int64DivMod1E9(ref n), 9);
				digits -= 9;

			}

			UInt32ToDecChars(ref index, outputStr, (uint)n, digits);

			Write(outputStr, index + 1, 19 - index);
		}

		internal static void UInt32ToDecChars(ref int index, Span<byte> target, uint n, int digits)
		{
			while (--digits >= 0 || n != 0)
			{
				uint quotient = n / 10;
				target[index--] = (byte)('0' + (n - (quotient * 10)));
				n = quotient;
			}
		}

		/// <summary>
		/// Writes the given number out textually without allocating. Avoid unless necessary.
		/// </summary>
		/// <param name="n"></param>
		public void WriteS(long n)
		{
			if (n >= 0)
			{
				WriteS((ulong)n);
				return;
			}

			// It's negative.
			Write((byte)'-');
			WriteS((ulong)-n);
		}

		/// <summary>Write an 8 byte unsigned value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ulong value){
			if(Fill > (Pool.BufferSize-8)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
			_LastBufferBytes[Fill++]=(byte)(value>>32);
			_LastBufferBytes[Fill++]=(byte)(value>>40);
			_LastBufferBytes[Fill++]=(byte)(value>>48);
			_LastBufferBytes[Fill++]=(byte)(value>>56);
		}

		/// <summary>Write an 8 byte signed value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(long value){
			if(Fill > (Pool.BufferSize-8)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
			_LastBufferBytes[Fill++]=(byte)(value>>32);
			_LastBufferBytes[Fill++]=(byte)(value>>40);
			_LastBufferBytes[Fill++]=(byte)(value>>48);
			_LastBufferBytes[Fill++]=(byte)(value>>56);
		}

		/// <summary>Write a 5 byte signed value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteUInt40(ulong value)
		{
			if (Fill > (Pool.BufferSize - 5))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)value;
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
		}

		/// <summary>Write a 5 byte signed value to the message (Big endian).</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteUInt40BE(ulong value)
		{
			if (Fill > (Pool.BufferSize - 5))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)(value >> 32);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)value;
		}

		/// <summary>Write a 6 byte signed value to the message.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteUInt48(ulong value)
		{
			if (Fill > (Pool.BufferSize - 6))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)value;
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
			_LastBufferBytes[Fill++] = (byte)(value >> 40);
		}
		
		/// <summary>Write a 6 byte signed value to the message. Big endian.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteUInt48BE(ulong value)
		{
			if (Fill > (Pool.BufferSize - 6))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)(value >> 40);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)value;
		}

		/// <summary>Write a compressed value to the message.</summary>
		public void WriteCompressed(ulong value){
			
			if(Fill > (Pool.BufferSize - 9)){
				NextBuffer();
			}
			
			if (value< 251){
				
				// Single byte:
				_LastBufferBytes[Fill++]=(byte)value;
				
			}else if (value <= ushort.MaxValue){
				
				// Status 251 for a 2 byte num:
				_LastBufferBytes[Fill++]=(byte)251;
				_LastBufferBytes[Fill++]=(byte)value;
				_LastBufferBytes[Fill++]=(byte)(value>>8);
				
			}else if (value < 16777216L){
				
				// Status 252 for a 3 byte num:
				_LastBufferBytes[Fill++]=(byte)252;
				_LastBufferBytes[Fill++]=(byte)value;
				_LastBufferBytes[Fill++]=(byte)(value>>8);
				_LastBufferBytes[Fill++]=(byte)(value>>16);
				
			}else if(value <= uint.MaxValue){
				
				// Status 253 for a 4 byte num:
				_LastBufferBytes[Fill++]=(byte)253;
				_LastBufferBytes[Fill++]=(byte)value;
				_LastBufferBytes[Fill++]=(byte)(value>>8);
				_LastBufferBytes[Fill++]=(byte)(value>>16);
				_LastBufferBytes[Fill++]=(byte)(value>>24);
				
			}else{
				
				// Status 254 for an 8 byte num:
				_LastBufferBytes[Fill++]=(byte)254;
				_LastBufferBytes[Fill++]=(byte)value;
				_LastBufferBytes[Fill++]=(byte)(value>>8);
				_LastBufferBytes[Fill++]=(byte)(value>>16);
				_LastBufferBytes[Fill++]=(byte)(value>>24);
				_LastBufferBytes[Fill++]=(byte)(value>>32);
				_LastBufferBytes[Fill++]=(byte)(value>>40);
				_LastBufferBytes[Fill++]=(byte)(value>>48);
				_LastBufferBytes[Fill++]=(byte)(value>>56);
				
			}

			// 255 for n byte numbers, where n is the following byte +8.
		}

		/// <summary>Write an invertible compressed nullable value to the message.</summary>
		public void WriteInvertibleCompressedSigned(long? value)
		{
			if (!value.HasValue)
			{
				WriteInvertibleCompressed(0);
				return;
			}

			ulong v2;
			
			if (value < 0)
			{
				v2 = ((ulong)-value) << 1 | 1;
			}
			else
			{
				v2 = (ulong)value << 1;
			}

			WriteInvertibleCompressed(v2+1);
		}
		
		/// <summary>Write an invertible compressed nullable value to the message. The value was originally nullable.</summary>
		public void WriteInvertibleCompressedSignedNullable(long value)
		{
			ulong v2;
			
			if (value < 0)
			{
				v2 = ((ulong)-value) << 1 | 1;
			}
			else
			{
				v2 = (ulong)value << 1;
			}

			WriteInvertibleCompressed(v2+1);
		}

		/// <summary>Write an invertible compressed nullable value to the message.</summary>
		public void WriteInvertibleCompressed(ulong? value)
		{
			if (!value.HasValue)
			{
				WriteInvertibleCompressed(0);
				return;
			}

			WriteInvertibleCompressed(value.Value + 1);
		}

		/// <summary>Write an invertible compressed value to the message.</summary>
		public void WriteInvertibleCompressedSigned(long value)
		{
			ulong v2;

			if (value < 0)
			{
				v2 = ((ulong)-value) << 1 | 1;
			}
			else
			{
				v2 = (ulong)value << 1;
			}

			WriteInvertibleCompressed(v2);
		}

		/// <summary>Write an invertible compressed value to the message.</summary>
		public void WriteInvertibleCompressed(ulong value)
		{

			if (Fill > (Pool.BufferSize - 10))
			{
				NextBuffer();
			}

			if (value < 251)
			{
				// Single byte:
				_LastBufferBytes[Fill++] = (byte)value;
			}
			else if (value <= ushort.MaxValue)
			{
				// Status 251 for a 2 byte num:
				_LastBufferBytes[Fill++] = (byte)251;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)251;
			}
			else if (value < 16777216L)
			{
				// Status 252 for a 3 byte num:
				_LastBufferBytes[Fill++] = (byte)252;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)(value >> 16);
				_LastBufferBytes[Fill++] = (byte)252;
			}
			else if (value <= uint.MaxValue)
			{
				// Status 253 for a 4 byte num:
				_LastBufferBytes[Fill++] = (byte)253;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)(value >> 16);
				_LastBufferBytes[Fill++] = (byte)(value >> 24);
				_LastBufferBytes[Fill++] = (byte)253;
			}
			else
			{
				// Status 254 for an 8 byte num:
				_LastBufferBytes[Fill++] = (byte)254;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)(value >> 16);
				_LastBufferBytes[Fill++] = (byte)(value >> 24);
				_LastBufferBytes[Fill++] = (byte)(value >> 32);
				_LastBufferBytes[Fill++] = (byte)(value >> 40);
				_LastBufferBytes[Fill++] = (byte)(value >> 48);
				_LastBufferBytes[Fill++] = (byte)(value >> 56);
				_LastBufferBytes[Fill++] = (byte)254;
			}

			// 255 for n byte numbers, where n is the following byte +8.
		}

		/// <summary>
		/// Use this to read a compressed number in the first block of a writer.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ulong ReadCompressedAt(ref int index)
		{
			var buff = FirstBuffer.Bytes;

			var first = buff[index++];
			switch (first)
			{
				case 251:
					// 2 bytes:
					return (ulong)(buff[index++] | (buff[index++] << 8));
				case 252:
					// 3 bytes:
					return (ulong)(buff[index++] | (buff[index++] << 8) | (buff[index++] << 16));
				case 253:
					// 4 bytes:
					return (ulong)(buff[index++] | (buff[index++] << 8) | (buff[index++] << 16) | (buff[index++] << 24));
				case 254:
					// 8 bytes:
					return (ulong)buff[index++] | ((ulong)buff[index++] << 8) | ((ulong)buff[index++] << 16) | ((ulong)buff[index++] << 24) |
						((ulong)buff[index++] << 32) | ((ulong)buff[index++] << 40) | ((ulong)buff[index++] << 48) | ((ulong)buff[index++] << 56);
				// 255 is n bytes
				default:
					return first;
			}

		}

		/// <summary>Write a compressed value to the message.</summary>
		public void WritePackedInt(ulong value)
		{

			if (Fill > (Pool.BufferSize - 9))
			{
				NextBuffer();
			}
			
			if (value < 251)
			{

				// Single byte:
				_LastBufferBytes[Fill++] = (byte)value;

			}
			else if (value < 16777216L)
			{

				// Status 253 for a 3 byte num:
				_LastBufferBytes[Fill++] = (byte)253;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)(value >> 16);

			}
			else if (value <= uint.MaxValue)
			{
				// Status 252 for a 4 byte num:
				_LastBufferBytes[Fill++] = (byte)252;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)(value >> 16);
				_LastBufferBytes[Fill++] = (byte)(value >> 24);
			}
			else
			{

				// Status 254 for an 8 byte num:
				_LastBufferBytes[Fill++] = (byte)254;
				_LastBufferBytes[Fill++] = (byte)value;
				_LastBufferBytes[Fill++] = (byte)(value >> 8);
				_LastBufferBytes[Fill++] = (byte)(value >> 16);
				_LastBufferBytes[Fill++] = (byte)(value >> 24);
				_LastBufferBytes[Fill++] = (byte)(value >> 32);
				_LastBufferBytes[Fill++] = (byte)(value >> 40);
				_LastBufferBytes[Fill++] = (byte)(value >> 48);
				_LastBufferBytes[Fill++] = (byte)(value >> 56);

			}

			// 255 for n bytes
		}

		/// <summary>
		/// Writes a given number of nul bytes. Only used by MySQL during startup. 
		/// </summary>
		/// <param name="count"></param>
		public void Skip(int count)
		{
			for (var i = 0; i < count; i++)
			{
				Write((byte)0);
			}
		}

		/// <summary>
		/// Writes bytes and its length.
		/// </summary>
		/// <param name="bytes"></param>
		public void Write(byte[] bytes)
		{
			if (bytes == null)
			{
				WriteCompressed(0);
				return;
			}
			WriteCompressed((ulong)(bytes.Length + 1));
			WriteNoLength(bytes);
		}

		/// <summary>
		/// Writes the given string as UTF8 bytes, but without the length.
		/// </summary>
		/// <param name="str"></param>
		public void WriteS(string str)
		{
			if (str == null)
			{
				return;
			}
			
			var charStream = str.AsSpan();

			// Next, write the chars:
			WriteCharStream(charStream);
		}

		/// <summary>
		/// Non-allocating string write, encoded in utf8.
		/// </summary>
		/// <param name="str"></param>
		public void WriteUTF8(string str)
		{
			if (str == null)
			{
				WriteCompressed(0);
				return;
			}

			// Grab a reference to the char span:
			var charStream = str.AsSpan();
			var max = charStream.Length;

			// First, how long will it be? This greatly helps the receive end.
			ulong length = 0;

			for (var i = 0; i < max; i++)
			{
				var current = charStream[i];

				if (char.IsHighSurrogate(current))
				{
					i++;

					if (i != max)
					{
						var low = charStream[i];

						if (char.IsLowSurrogate(low))
						{
							// Remap the codepoint.
							length += (ulong)Utf8.RuneLen((uint)char.ConvertToUtf32(current, low));
						}
					}
				}
				else
				{
					length += (ulong)Utf8.RuneLen((uint)current);
				}
			}

			// Write the length (offset by 1 for null):
			WriteCompressed(length+1);

			// Next, write the chars:
			WriteCharStream(charStream);
		}

		/// <summary>
		/// Writes an invertible byte array
		/// </summary>
		/// <param name="bytes"></param>
		public void WriteInvertible(byte[] bytes)
		{
			if (bytes == null)
			{
				WriteInvertibleCompressed(0);
				return;
			}

			// Write the length (offset by 1 for null):
			var length = (uint)(bytes.Length + 1);
			WriteInvertibleCompressed(length);
			WriteNoLength(bytes);
			WriteInvertibleCompressed(length);
		}

		/// <summary>
		/// Non-allocating string write, encoded in utf8, which has the length at both ends when necessary.
		/// </summary>
		/// <param name="str"></param>
		public void WriteInvertibleUTF8(string str)
		{
			if (str == null)
			{
				WriteInvertibleCompressed(0);
				return;
			}

			// Grab a reference to the char span:
			var charStream = str.AsSpan();
			var max = charStream.Length;

			// First, how long will it be? This greatly helps the receive end.
			ulong length = 0;

			for (var i = 0; i < max; i++)
			{
				var current = charStream[i];

				if (char.IsHighSurrogate(current))
				{
					i++;

					if (i != max)
					{
						var low = charStream[i];

						if (char.IsLowSurrogate(low))
						{
							// Remap the codepoint.
							length += (ulong)Utf8.RuneLen((uint)char.ConvertToUtf32(current, low));
						}
					}
				}
				else
				{
					length += (ulong)Utf8.RuneLen((uint)current);
				}
			}

			// Write the length (offset by 1 for null):
			WriteInvertibleCompressed(length + 1);

			// Next, write the chars:
			WriteCharStream(charStream);

			// Write the length again (offset by 1 for null), unless it was 0:
			if (length != 0)
			{
				WriteInvertibleCompressed(length + 1);
			}
		}

		/// <summary>
		/// Non-allocating write of a utf8 string.
		/// </summary>
		/// <param name="str"></param>
		public void Write(ustring str)
		{
			if (str == null)
			{
				WriteCompressed(0);
				return;
			}

			var s = str.AsSpan();
			WriteCompressed((ulong)(s.Length + 1));
			Write(s, 0, s.Length);
		}

		/// <summary>
		/// Writes a string in its raw, UTF-16 byte format. Does not allocate.
		/// </summary>
		/// <param name="str"></param>
		public void WriteUTF16(string str)
		{
			if (str == null)
			{
				WriteCompressed(0);
				return;
			}

			var s = MemoryMarshal.AsBytes(str.AsSpan());
			WriteCompressed((ulong)(s.Length+1));
			Write(s, 0, s.Length);
		}

		/// <summary>
		/// A nul-terminated string. Only used by MySQL connections during startup.
		/// </summary>
		/// <param name="str"></param>
		public void WriteNulString(string str)
		{
			if (str != null)
			{
				var charStream = str.AsSpan();
				WriteCharStream(charStream);
			}
			Write((byte)0);
		}
		
		/// <summary>
		/// Has the length of the string before it, packed using the MySQL format.
		/// </summary>
		/// <param name="str"></param>
		public void WriteMySQLString(string str)
		{
			if (str == null)
			{
				WritePackedInt(0);
				return;
			}

			// Grab a reference to the char span:
			var charStream = str.AsSpan();
			var max = charStream.Length;

			// First, how long will it be? This greatly helps the receive end.
			ulong length = 0;

			for (var i = 0; i < max; i++)
			{
				var current = charStream[i];

				if (char.IsHighSurrogate(current))
				{
					i++;

					if (i != max)
					{
						var low = charStream[i];

						if (char.IsLowSurrogate(low))
						{
							// Remap the codepoint.
							length += (ulong)Utf8.RuneLen((uint)char.ConvertToUtf32(current, low));
						}
					}
				}
				else
				{
					length += (ulong)Utf8.RuneLen((uint)current);
				}
			}

			// Write the length:
			WritePackedInt(length);

			// Next, write the chars:
			WriteCharStream(charStream);
		}

		private void WriteCharStream(ReadOnlySpan<char> charStream)
		{
			var max = charStream.Length;
			var bufferSize = Pool.BufferSize;

			for (var i = 0; i < max; i++)
			{
				var current = charStream[i];
				uint rune;
				int runeBytesInUtf8;

				if (char.IsHighSurrogate(current))
				{
					i++;

					if (i != max)
					{
						var low = charStream[i];

						if (char.IsLowSurrogate(low))
						{
							// Remap the codepoint.
							rune = (uint)char.ConvertToUtf32(current, low);

							// Note: don't ever get escape-worthy chars here. Always outputted as-is.
							runeBytesInUtf8 = Utf8.RuneLen(rune);

							if (Fill > (bufferSize - runeBytesInUtf8))
							{
								NextBuffer();
							}

							Utf8.EncodeRune(rune, _LastBufferBytes, Fill);
							Fill += runeBytesInUtf8;
						}
					}

					continue;
				}

				// Codepoint is as-is:
				rune = (uint)current;

				// output the character:
				runeBytesInUtf8 = Utf8.RuneLen(rune);

				if (Fill > (bufferSize - runeBytesInUtf8))
				{
					NextBuffer();
				}

				Utf8.EncodeRune(rune, _LastBufferBytes, Fill);
				Fill += runeBytesInUtf8;
			}
		}

		/// <summary>
		/// Gets the whole thing as a UTF8 string.
		/// </summary>
		/// <returns></returns>
		public string ToUTF8String()
		{
			/*
			 * If it was UTF16 in the writer:
			 * 
			string.Create(Length, this, (Span<char> chars, Writer writer) =>
			{
				var charIndex = 0;
				var currentBuffer = FirstBuffer;

				while (currentBuffer != null)
				{
					var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;

					// For each pair of 2 bytes, increase charIndex and write to chars.

					currentBuffer = currentBuffer.After;
				}
			});
			*/

			var buffer = AllocatedResultNoHeaders();
			return System.Text.Encoding.UTF8.GetString(buffer);
		}

		/// <summary>
		/// Allocates an ASCII string from the bytes of this writer.
		/// </summary>
		/// <returns></returns>
		public string ToASCIIString()
		{
			return string.Create(Length, this, (Span<char> chars, Writer writer) =>
			{
				var charIndex = 0;
				var currentBuffer = FirstBuffer;

				while (currentBuffer != null)
				{
					var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;

					// For each byte, increase charIndex and write to chars.
					for (var i = 0; i < blockSize; i++)
					{
						chars[charIndex++] = (char)currentBuffer.Bytes[i];
					}

					currentBuffer = currentBuffer.After;
				}
			});
		}

		/// <summary>
		/// Allocates the complete chain of buffers as a byte array.
		/// Avoid unless necessary.
		/// </summary>
		public byte[] AllocatedResult()
		{
			byte[] result = new byte[Length];
			int index = 0;
			var currentBuffer = FirstBuffer;

			while (currentBuffer != null)
			{
				var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
				Array.Copy(currentBuffer.Bytes, 0, result, index, blockSize);
				index += blockSize;
				currentBuffer = currentBuffer.After;
			}

			return result;
		}
		
		/// <summary>
		/// Allocates the complete chain of buffers as a byte array, excluding buffer offsets.
		/// Avoid unless necessary.
		/// </summary>
		public byte[] AllocatedResultNoHeaders()
		{
			byte[] result = new byte[LengthWithoutHeaders];
			int index = 0;
			var currentBuffer = FirstBuffer;

			while (currentBuffer != null)
			{
				var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
				blockSize -= Pool.StartOffset;
				Array.Copy(currentBuffer.Bytes, Pool.StartOffset, result, index, blockSize);
				index += blockSize;
				currentBuffer = currentBuffer.After;
			}

			return result;
		}
		
		/// <summary>
		/// Allocates the complete chain of buffers as a byte array.
		/// Avoid unless necessary.
		/// </summary>
		/// <param name="includeHeaders">Some writers/ pools add headers at the start of each buffer. Setting this to true includes those headers.</param>
		public System.IO.MemoryStream AllocateMemoryStream(bool includeHeaders = false)
		{
			System.IO.MemoryStream ms;
			var currentBuffer = FirstBuffer;

			if (includeHeaders)
			{
				ms = new System.IO.MemoryStream(Length);

				while (currentBuffer != null)
				{
					var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
					ms.Write(currentBuffer.Bytes, 0, blockSize);
					currentBuffer = currentBuffer.After;
				}
			}
			else
			{
				ms = new System.IO.MemoryStream(LengthWithoutHeaders);

				while (currentBuffer != null)
				{
					var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
					blockSize -= Pool.StartOffset;
					ms.Write(currentBuffer.Bytes, Pool.StartOffset, blockSize);
					currentBuffer = currentBuffer.After;
				}
			}

			return ms;
		}

		/// <summary>
		/// Copies this writer result to the given stream.
		/// </summary>
		/// <param name="s"></param>
		public void CopyTo(System.IO.Stream s)
		{
			var currentBuffer = FirstBuffer;

			while (currentBuffer != null)
			{
				var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
				s.Write(currentBuffer.Bytes, 0, blockSize);
				currentBuffer = currentBuffer.After;
			}
		}

		/// <summary>
		/// Copies this writer result to another writer.
		/// </summary>
		/// <param name="w"></param>
		public void CopyTo(Writer w)
		{
			var currentBuffer = FirstBuffer;

			while (currentBuffer != null)
			{
				var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
				w.Write(currentBuffer.Bytes, 0, blockSize);
				currentBuffer = currentBuffer.After;
			}
		}

		/// <summary>
		/// Copies this writer result to the given stream.
		/// </summary>
		/// <param name="s"></param>
		public async ValueTask CopyToAsync(System.IO.Stream s)
		{
			var currentBuffer = FirstBuffer;

			while (currentBuffer != null)
			{
				var blockSize = (currentBuffer == LastBuffer) ? Fill : currentBuffer.Length;
				await s.WriteAsync(currentBuffer.Bytes, 0, blockSize);
				currentBuffer = currentBuffer.After;
			}
		}

		/// <summary>Write a whole block of bytes to this message.</summary>
		public void WriteNoLength(byte[] buffer)
		{
			Write(buffer, 0, buffer.Length);
		}
		
		/// <summary>Write a specific range of bytes to this message.</summary>
		public void Write(byte[] buffer, int offset, int length)
		{
			var bufferSize = Pool.BufferSize;
			if(bufferSize == Fill){
				NextBuffer();
			}
			
			int space = bufferSize - Fill;
			
			if(length <= space){
				// Copy the bytes in:
				Array.Copy(buffer, offset, _LastBufferBytes, Fill, length);
				Fill += length;
				return;
			}
			
			// Fill the first buffer:
			Array.Copy(buffer, offset, _LastBufferBytes, Fill, space);
			Fill = bufferSize;
			length -= space;
			offset += space;

			// Fill full size buffers:
			var bufferSpace = Pool.BufferSpaceSize;

			while(length >= bufferSpace)
			{
				NextBuffer();
				Array.Copy(buffer, offset, _LastBufferBytes, Pool.StartOffset, bufferSpace);
				offset += bufferSpace;
				length -= bufferSpace;
				Fill = bufferSize;
			}
			
			if(length > 0){
				NextBuffer();
				Array.Copy(buffer, offset, _LastBufferBytes, Pool.StartOffset, length);
				Fill = Pool.StartOffset + length;
			}
			
		}

		/// <summary>Write a specific range of bytes to this message.</summary>
		public void Write(ReadOnlySpan<byte> buffer, int offset, int length)
		{
			var bufferSize = Pool.BufferSize;
			if (bufferSize == Fill)
			{
				NextBuffer();
			}

			int space = bufferSize - Fill;

			Span<byte> target;
			ReadOnlySpan<byte> src;

			if (length <= space)
			{
				// Copy the bytes in:
				target = new Span<byte>(_LastBufferBytes, Fill, length);
				src = buffer.Slice(offset, length);
				src.CopyTo(target);
				Fill += length;
				return;
			}

			// Fill the first buffer:
			target = new Span<byte>(_LastBufferBytes, Fill, space);
			src = buffer.Slice(offset, space);
			src.CopyTo(target);
			Fill = bufferSize;
			length -= space;
			offset += space;

			// Fill full size buffers:
			var bufferSpace = Pool.BufferSpaceSize;
			
			while (length >= bufferSpace)
			{
				NextBuffer();
				target = new Span<byte>(_LastBufferBytes, Pool.StartOffset, bufferSize);
				src = buffer.Slice(offset, bufferSize);
				src.CopyTo(target);
				offset += bufferSpace;
				length -= bufferSpace;
				Fill = bufferSize;
			}

			if (length > 0)
			{
				NextBuffer();
				target = new Span<byte>(_LastBufferBytes, Pool.StartOffset, length);
				src = buffer.Slice(offset, length);
				src.CopyTo(target);
				Fill = Pool.StartOffset + length;
			}

		}

	}
}