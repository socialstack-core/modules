using System;


namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Pointer to a particular segment of a byte[]. Pooled and used by contexts.
	/// </summary>
	public struct BufferSegment{
		
		/// <summary>Linked list of buffers from the pool</summary>
		public BufferedBytes FirstBuffer;
		/// <summary>Linked list of buffers from the pool</summary>
		public BufferedBytes LastBuffer;
		/// <summary>
		/// The current buffer in the linked list being pointed at.
		/// </summary>
		public BufferedBytes CurrentBuffer;
		/// <summary>
		/// Offset to start of data.
		/// </summary>
		public int Offset;
		/// <summary>
		/// Length of the data in this buffer segment.</summary>
		public int Length;
		/// <summary>
		/// True if this data is a copy (i.e. it will only go out of memory when it is explicitly released).
		/// </summary>
		public bool IsCopy;
		
		/// <summary>
		/// Get the next byte.
		/// </summary>
		public byte Next{
			get{
				if(Offset >= BinaryBufferPool.BufferSize){
					CurrentBuffer = CurrentBuffer.After;
					Offset = 0;
				}
				return CurrentBuffer.Bytes[Offset++];
			}
		}
		
		/// <summary>
		/// Resets pointer back to start of this buffer.
		/// </summary>
		public void Reset() {
			Offset = 0;
			CurrentBuffer = FirstBuffer;
		}
		
		/// <summary>
		/// Skips the given number of bytes, advancing the pointer.
		/// </summary>
		public void Skip(int count)
		{
			for (var i = 0; i < count; i++) {
				if (Offset >= BinaryBufferPool.BufferSize)
				{
					CurrentBuffer = CurrentBuffer.After;
					Offset = 0;
				}
				Offset++;
			}
		}
		
		/// <summary>
		/// Reads a compact number, advancing the pointer.
		/// </summary>
		public ulong ReadCompressed()
		{
			var c = Next;

			switch (c)
			{
				case 251:
					// 2 bytes needed.
					return GetUInt16();
				case 252:
					// 3 bytes needed.
					return GetUInt24();
				case 253:
					// 4 bytes needed.
					return GetUInt32();
				case 254:

					// 8 bytes needed.
					return GetUInt64();
				default:
					return c;
			}
		}

		/// <summary>
		/// Reads a UInt16, advancing the pointer.
		/// </summary>
		public ushort GetUInt16()
		{
			return (ushort)(Next | (ushort)(Next << 8));
		}

		/// <summary>
		/// Reads a UInt24, advancing the pointer.
		/// </summary>
		public uint GetUInt24()
		{
			return (uint)Next | ((uint)Next << 8) | ((uint)Next << 16);
		}

		/// <summary>
		/// Reads a UInt32, advancing the pointer.
		/// </summary>
		public uint GetUInt32()
		{
			return (uint)Next | ((uint)Next << 8) | ((uint)Next << 16) | ((uint)Next << 24);
		}

		/// <summary>
		/// Reads a UInt40, advancing the pointer.
		/// </summary>
		public ulong GetUInt40()
		{
			return (ulong)Next | ((ulong)Next << 8) | ((ulong)Next << 16) | ((ulong)Next << 24) | ((ulong)Next << 32);
		}
		
		/// <summary>
		/// Reads a UInt64, advancing the pointer.
		/// </summary>
		public ulong GetUInt64()
		{
			return (ulong)Next | ((ulong)Next << 8) | ((ulong)Next << 16) | ((ulong)Next << 24) | ((ulong)Next << 32) | ((ulong)Next << 40) | ((ulong)Next << 48) | ((ulong)Next << 56);
		}

		/// <summary>
		/// Allocates the contents of this buffer as a string.
		/// </summary>
		/// <returns></returns>
		public string GetString()
		{
			var buffer = AllocatedBuffer();
			if (buffer == null)
			{
				return null;
			}
			return System.Text.Encoding.UTF8.GetString(buffer);
		}

		/// <summary>
		/// Slow/ allocated block of bytes.
		/// </summary>
		/// <returns></returns>
		public byte[] AllocatedBuffer()
		{
			if(Length == -1)
			{
				return null;
			}

			var result = new byte[Length];
			for (var i = 0; i < Length; i++) {
				result[i] = Next;
			}
			return result;
		}

		/// <summary>
		/// Ensures this object is a copy. Note that non-copy objects always fit within one bufferedbyte block.
		/// </summary>
		public void Copy() {
			if(IsCopy){
				return;
			}
			var newBuffer = BinaryBufferPool.Get();
			newBuffer.Offset = 0;
			if (Length != -1)
			{
				Array.Copy(FirstBuffer.Bytes, Offset, newBuffer.Bytes, 0, Length);
			}
			Offset = 0;
			IsCopy = true;
			FirstBuffer = newBuffer;
			LastBuffer = newBuffer;
			CurrentBuffer = newBuffer;
		}
		
		/// <summary>
		/// Copies Length bytes from this buffer segment into the given target buffer.
		/// Note that it MUST have space, and you must not have used Next.
		/// </summary>
		public void CopyInto(byte[] targetBuffer, int atIndex){
			if (Length == -1)
			{
				return;
			}

			var bytesRemaining = BinaryBufferPool.BufferSize - Offset;
			
			if(Length <= bytesRemaining){
				// One buffer only:
				Array.Copy(CurrentBuffer.Bytes, Offset, targetBuffer, atIndex, Length);
				Offset += Length;
				return;
			}
			
			// Copy the first block of bytes from current buffer:
			Array.Copy(CurrentBuffer.Bytes, Offset, targetBuffer, atIndex, bytesRemaining);
			atIndex += bytesRemaining;
			
			// Bytes to go is..
			bytesRemaining = Length - bytesRemaining;
			
			// For each full buffer:
			while(bytesRemaining > BinaryBufferPool.BufferSize){
				CurrentBuffer = CurrentBuffer.After;
				Array.Copy(CurrentBuffer.Bytes, 0, targetBuffer, atIndex, BinaryBufferPool.BufferSize);
				bytesRemaining -= BinaryBufferPool.BufferSize;
				atIndex += BinaryBufferPool.BufferSize;
			}
			
			if(bytesRemaining > 0){
				CurrentBuffer = CurrentBuffer.After;
				Array.Copy(CurrentBuffer.Bytes, 0, targetBuffer, atIndex, bytesRemaining);
				Offset = bytesRemaining;
			}else{
				Offset = BinaryBufferPool.BufferSize;
			}
			
		}

		/// <summary>
		/// Gets a long number from a textual value in this buffer. Ignores any characters other than digits.
		/// </summary>
		/// <returns></returns>
		public ulong GetTextualNumberUnsigned()
		{
			byte num;
			ulong result = 0;
			
			for (var i = 0; i < Length; i++)
			{
				num = Next;
				result = result * 10 + (ulong)(num - '0');
			}
			
			return result;
		}

		/// <summary>
		/// Gets a long number from a textual value in this buffer. Ignores any characters other than digits and -.
		/// </summary>
		/// <returns></returns>
		public long GetTextualNumber()
		{
			byte num = Next;
			bool negative = false;
			long result = 0;

			// Handle leading sign:

			if (num == '-')
			{
				negative = true;
			}
			else {
				// First digit:
				result = num - '0';
			}

			for (var i = 1; i < Length; i++)
			{
				num = Next;
				result = result * 10 + (num - '0');
			}

			if (negative)
			{
				return -result;
			}
			return result;
		}

		/// <summary>
		/// View the contents of this buffer as a simple MySQL flavour string. Generally for debug use only.
		/// </summary>
		public string AsStringMySQL(){
			if (Length == -1)
			{
				return null;
			}
			return System.Text.Encoding.UTF8.GetString(FirstBuffer.Bytes, Offset, Length);
		}

		/// <summary>
		/// Please avoid unless absolutely necessary! Virtually everything supports raw byte buffers so do that instead.
		/// </summary>
		/// <returns></returns>
		public string AsHex(){
			if (Length == -1)
			{
				return null;
			}

			var sb = new System.Text.StringBuilder();

			var currentBuffer = FirstBuffer;
			int index = Offset;

			for (var i = 0; i < Length; i++)
			{
				if (index == BinaryBufferPool.BufferSize)
				{
					index = 0;
					currentBuffer = currentBuffer.After;
				}

				var byteV = currentBuffer.Bytes[index++];

				if (i != 0) {
					sb.Append(' ');
				}

				sb.Append(Hex.Lookup[byteV]);

			}

			return sb.ToString();
		}

		/// <summary>
		/// Releases the buffers from this segment.
		/// </summary>
		public void Release(){
			if(IsCopy){
				// Release all the buffers now.
				
				// Block release:
				lock(BinaryBufferPool.PoolLock){
					LastBuffer.After = BinaryBufferPool.First;
					BinaryBufferPool.First = FirstBuffer;
				}
				
				LastBuffer = null;
			}
		}
	}
	
}