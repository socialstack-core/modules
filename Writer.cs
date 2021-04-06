using System;
using System.Collections.Generic;

namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// A streamable message which writes to buffer pool.
	/// You must use GetPooled() and then a Start(..) overload to set it up.
	/// </summary>
	public class Writer{
		
		/// <summary>
		/// First writer in the pool.
		/// </summary>
		private static Writer FirstCached;
		
		/// <summary>
		/// Thread safety for the pool.
		/// </summary>
		private static readonly object PoolLock = new object();
		
		
		/// <summary>
		/// Gets a pooled writer.
		/// Generally use this rather than new Writer.
		/// </summary>
		public static Writer GetPooled(){
			Writer result;

			lock (PoolLock)
			{
				if (FirstCached == null)
				{
					return new Writer();
				}

				result = FirstCached;
				FirstCached = result.NextInLine;

			}

			result.NextInLine = null;
			return result;
		}
		
		/// <summary>
		/// Base length.
		/// </summary>
		public int BaseLength;
		private int BlockCount=1;
		private int Fill;
		private int ReplyIdOffset;
		/// <summary>The linked list of buffers that form this message.</summary>
		public BufferedBytes FirstBuffer;
		/// <summary>The linked list of buffers that form this message.</summary>
		public BufferedBytes LastBuffer;
		/// <summary>Next writer in a queue after this one.</summary>
		public Writer NextInLine;
		/// <summary>
		/// The context to use to grab the request ID.
		/// </summary>
		public IMessage ContextForRequestId;
		private byte[] _LastBufferBytes;
		
		
		/// <summary>When using writers you must use a start method.</summary>
		public void Start(uint opcode){
			BaseLength = 0;
			BlockCount = 1;
			Fill = 0;
			var buffer = BinaryBufferPool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;
			buffer.Offset = 0;
			
			// Write the opcode:
			WriteCompressed(opcode);
			ContextForRequestId = null;
		}
		
		/// <summary>When using writers you must use a start method.</summary>
		public void Start(uint opcode, IMessage replyContext, Action<byte> onDone){
			BaseLength = 0;
			BlockCount = 1;
			Fill = 0;
			var buffer = BinaryBufferPool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;
			buffer.Offset = 0;
			
			// Write the opcode:
			WriteCompressed(opcode);
			ReplyIdOffset = Fill;
			
			// 2 bytes for the reply ID.
			Fill+=2;
			
			// replyContext.Push(onDone);
			ContextForRequestId = replyContext;
		}

		/// <summary>When using writers you must use a start method.</summary>
		public void StartWithLength()
		{
			BaseLength = 0;
			BlockCount = 1;
			Fill = 0;
			var buffer = BinaryBufferPool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;
			buffer.Offset = 0;

			// Length and sequence:
			Fill += 4;

			// Make sure sequence is set to 0:
			_LastBufferBytes[3] = (byte)0;
			ContextForRequestId = null;
		}

		/// <summary>When using writers you must use a start method.</summary>
		public void Start(byte[] template)
		{
			BaseLength = 0;
			BlockCount = 1;
			Fill = 0;
			var buffer = BinaryBufferPool.Get();
			LastBuffer = buffer;
			FirstBuffer = buffer;
			_LastBufferBytes = buffer.Bytes;
			buffer.Offset = 0;
			ContextForRequestId = null;
			Write(template);
		}

		private Writer() {
			// Use the GetPooled() method and Start() instead.
		}

		/// <summary>
		/// Total fill.
		/// </summary>
		public int Length{
			get{
				return BaseLength + Fill;
			}
		}

		/// <summary>
		/// Sets the last buffers fill amount to the writers fill.
		/// </summary>
		public void SetLastFill() {
			LastBuffer.Length = Fill;
		}
		
		/// <summary>
		/// Sets the request ID to a writer which was started with space for one.
		/// </summary>
		/// <param name="requestId"></param>
		public void SetRequestId(int requestId){
			FirstBuffer.Bytes[ReplyIdOffset] = (byte)requestId;
			FirstBuffer.Bytes[ReplyIdOffset+1] = (byte)(requestId>>8);
		}
		
		/// <summary>
		/// Used when the current buffer is totally full and a new one is required.
		/// </summary>
		private void NextBuffer(){
			BaseLength+=Fill;
			LastBuffer.Length = Fill;
			var buffer = BinaryBufferPool.Get();
			LastBuffer.After = buffer;
			LastBuffer = buffer;
			buffer.Offset = 0;
			_LastBufferBytes = buffer.Bytes;
			BlockCount++;
			Fill=0;
		}

		/// <summary>Called when a writer is no longer needed and should now be fully released.</summary>
		public void Release()
		{
			// Release all buffers back to the pool and pool the writer itself too.
			lock (PoolLock)
			{
				// Shove into the pool:
				NextInLine = FirstCached;
				FirstCached = this;
			}

			// Release all the buffers:
			lock (BinaryBufferPool.PoolLock)
			{
				LastBuffer.After = BinaryBufferPool.First;
				BinaryBufferPool.First = FirstBuffer;
			}

			LastBuffer = null;
			FirstBuffer = null;
			_LastBufferBytes = null;
		}
		
		/// <summary>Called when a writer was sent out and the writer object itself can now be released.</summary>
		public void SentRelease()
		{
			// Set last fill:
			LastBuffer.Length = Fill;

			// Just clear the buffer refs in this case - the buffers will be released as they are sent individually:
			LastBuffer = null;
			FirstBuffer = null;
			_LastBufferBytes = null;

			// Release the writer itself:
			lock (PoolLock)
			{
				// Shove into the pool:
				NextInLine = FirstCached;
				FirstCached = this;
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

			for (var i = BlockCount-1; i >=0; i--)
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
		/// Writes a segment to this writer.
		/// </summary>
		/// <param name="segment"></param>
		public void Write(BufferSegment segment)
		{
			// Move our copy (because it's a struct) to the start of the segment:
			segment.Reset();
			var count = segment.Length;
			for (var i = 0; i < count; i++)
			{
				Write(segment.Next);
			}
		}

		/// <summary>
		/// Writes an array of strings to the writer.
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
				WriteCompressed((ulong)value.Count);

				foreach (var val in value)
				{
					Write(val);
				}
			}
		}

		/// <summary>Write a single byte to the message. Write a block of bytes instead of using this if you can.</summary>
		public void Write(byte val){
			if(Fill == BinaryBufferPool.BufferSize){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=val;
		}
		
		/// <summary>Write a 2 byte unsigned value to the message.</summary>
		public void Write(ushort value){
			if(Fill > (BinaryBufferPool.BufferSize-2)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
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

		/// <summary>Write a 2 byte signed value to the message.</summary>
		public void Write(short value){
			if(Fill > (BinaryBufferPool.BufferSize-2)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
		}
		
		/// <summary>Write a 4 byte unsigned value to the message.</summary>
		public void Write(uint value){
			if(Fill > (BinaryBufferPool.BufferSize-4)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
		}

		/// <summary>
		/// Write a nullable int32 value.
		/// </summary>
		/// <param name="val"></param>
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
		
		/// <summary>Write a 4 byte signed value to the message.</summary>
		public void Write(int value){
			if(Fill > (BinaryBufferPool.BufferSize-4)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
		}
		
		/// <summary>Write a 4 byte unsigned value to the message.</summary>
		public void WriteUInt24(uint value){
			if(Fill > (BinaryBufferPool.BufferSize-3)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
		}
		
		/// <summary>Write a 3 byte signed value to the message.</summary>
		public void WriteInt24(int value){
			if(Fill > (BinaryBufferPool.BufferSize-3)){
				NextBuffer();
			}
			
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
		}
		
		/// <summary>Write a float value.</summary>
		public void Write(float val){
			if(Fill > (BinaryBufferPool.BufferSize-4)){
				NextBuffer();
			}
			
			uint value = new FloatBits(val).Int;
			_LastBufferBytes[Fill++]=(byte)value;
			_LastBufferBytes[Fill++]=(byte)(value>>8);
			_LastBufferBytes[Fill++]=(byte)(value>>16);
			_LastBufferBytes[Fill++]=(byte)(value>>24);
		}

		/// <summary>Write a double value.</summary>
		public void Write(double val)
		{
			if (Fill > (BinaryBufferPool.BufferSize - 8))
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

		/// <summary>Write a date value.</summary>
		public void Write(DateTime val)
		{
			if (Fill > (BinaryBufferPool.BufferSize - 8))
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

		/// <summary>Write an 8 byte unsigned value to the message.</summary>
		public void Write(ulong value){
			if(Fill > (BinaryBufferPool.BufferSize-8)){
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
		public void Write(long value){
			if(Fill > (BinaryBufferPool.BufferSize-8)){
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
		public void WriteUInt40(ulong value)
		{
			if (Fill > (BinaryBufferPool.BufferSize - 5))
			{
				NextBuffer();
			}

			_LastBufferBytes[Fill++] = (byte)value;
			_LastBufferBytes[Fill++] = (byte)(value >> 8);
			_LastBufferBytes[Fill++] = (byte)(value >> 16);
			_LastBufferBytes[Fill++] = (byte)(value >> 24);
			_LastBufferBytes[Fill++] = (byte)(value >> 32);
		}

		/// <summary>Write a 6 byte signed value to the message.</summary>
		public void WriteUInt48(ulong value)
		{
			if (Fill > (BinaryBufferPool.BufferSize - 6))
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

		/// <summary>Write a compressed value to the message.</summary>
		public void WriteCompressed(ulong value){
			
			if(Fill > (BinaryBufferPool.BufferSize - 9)){
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
		}

		/// <summary>Write a compressed value to the message.</summary>
		public void WritePackedInt(ulong value)
		{

			if (Fill > (BinaryBufferPool.BufferSize - 9))
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
		}

		/// <summary>
		/// Only used by MySQL during startup. Writes a given number of nul bytes.
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
		public void WriteBuffer(byte[] bytes)
		{
			if (bytes == null)
			{
				WriteCompressed(0);
				return;
			}
			WriteCompressed((ulong)(bytes.Length + 1));
			Write(bytes);
		}

		/// <summary>
		/// Writes a string and its length.
		/// </summary>
		/// <param name="str"></param>
		public void Write(string str)
		{
			if (str == null)
			{
				WriteCompressed(0);
				return;
			}

			var strBytes = System.Text.Encoding.UTF8.GetBytes(str);
			WriteCompressed((ulong)(strBytes.Length + 1));
			Write(strBytes);
		}

		/// <summary>
		/// A nul-terminated string. Only used by MySQL connections during startup.
		/// </summary>
		/// <param name="str"></param>
		public void WriteNulString(string str)
		{
			if (str != null)
			{
				var strBytes = System.Text.Encoding.UTF8.GetBytes(str);
				Write(strBytes);
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
			var strBytes = System.Text.Encoding.UTF8.GetBytes(str);
			WritePackedInt((ulong)strBytes.Length);
			Write(strBytes);
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

		/// <summary>Write a whole block of bytes to this message.</summary>
		public void Write(byte[] buffer){
			Write(buffer,0,buffer.Length);
		}
		
		/// <summary>Write a specific range of bytes to this message.</summary>
		public void Write(byte[] buffer, int offset, int length){
			if(BinaryBufferPool.BufferSize == Fill){
				NextBuffer();
			}
			
			int space = BinaryBufferPool.BufferSize - Fill;
			
			if(length <= space){
				// Copy the bytes in:
				Array.Copy(buffer, offset, _LastBufferBytes, Fill, length);
				Fill += length;
				return;
			}
			
			// Fill the first buffer:
			Array.Copy(buffer, offset, _LastBufferBytes, Fill, space);
			Fill = BinaryBufferPool.BufferSize;
			length -= space;
			offset += space;
			
			// Fill full size buffers:
			while(length >= BinaryBufferPool.BufferSize){
				NextBuffer();
				Array.Copy(buffer, offset, _LastBufferBytes, 0, BinaryBufferPool.BufferSize);
				offset += BinaryBufferPool.BufferSize;
				length -= BinaryBufferPool.BufferSize;
			}
			
			if(length > 0){
				NextBuffer();
				Array.Copy(buffer, offset, _LastBufferBytes, 0, length);
				Fill = length;
			}
			
		}
		
	}
}