using System;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// A block of bytes which can be stored in a pool.
	/// </summary>
	public class BufferedBytes {

		/// <summary>The length of Bytes.</summary>
		public int Length;
		/// <summary>The starting point in bytes to send from.</summary>
		public int Offset;
		/// <summary>The bytes themselves.</summary>
		public byte[] Bytes;
		/// <summary>When this object is in the pool, this is the object after.</summary>
		public BufferedBytes After;
		/// <summary>
		/// The pool this block came from.
		/// </summary>
		public BinaryBufferPool Pool;

		/// <summary>
		/// Instances a new block of bytes.
		/// </summary>
		public BufferedBytes()
		{ }

		/// <summary>
		/// Instances a new block of bytes.
		/// </summary>
		public BufferedBytes(byte[] bytes, int length, BinaryBufferPool pool){
			Bytes = bytes;
			Length = length;
			Pool = pool;
		}
		
		/// <summary>
		/// Instances a new block of bytes.
		/// </summary>
		public void Init(byte[] bytes, int length, BinaryBufferPool pool){
			Bytes = bytes;
			Length = length;
			Pool = pool;
		}
		
		/// <summary>
		/// Instances a new block of bytes.
		/// </summary>
		public BufferedBytes(byte[] bytes, int length, int offset, BinaryBufferPool pool)
		{
			Bytes=bytes;
			Length=length;
			Offset=offset;
			Pool = pool;
		}
		
		/// <summary>Returns this bytes object to the pool it came from.</summary>
		public void Release(){
			var pool = Pool;
			lock(pool.PoolLock){
			
				if(pool.First==null){
					pool.First=this;
					After=null;
				}else{
					After= pool.First;
					pool.First=this;
				}
				
			}
			
		}

		/// <summary>Copies data from the given buffer into these buffers.
		/// Uses Offset on these buffer objects to track how full they are.</summary>
		/// <returns>May internally add to the linked list of buffers. 
		/// Returns the last buffer in the list (which might just be this if no extra ones were needed).</returns>
		public BufferedBytes CopyFrom(byte[] buffer, int offset, int count){
			var pool = Pool;
			// Space in this buffer is..
			var availableSpace = pool.BufferSize - Offset;
			
			if(availableSpace >= count){
				// Space in this buffer - copy now:
				Array.Copy(buffer, offset, Bytes, Offset, count);
				Offset += count;
				return this;
			}
			
			// Not enough space in this buffer. 
			// We'll need to allocate at least one other buffer after this one (and return it).
			
			// First though, fill the remaining space:
			Array.Copy(buffer, offset, Bytes, Offset, availableSpace);
			Offset = pool.BufferSize;
			count -= availableSpace;
			offset += availableSpace;
			
			var currentBuffer = this;

			// While we can fill entire buffers..
			var bufferSpaceSize = pool.BufferSpaceSize; // BufferSize minus any reserved space at the start.

			while(count > bufferSpaceSize)
			{
				currentBuffer.After = pool.Get();
				currentBuffer = currentBuffer.After;
				var startOffset = currentBuffer.Offset; // Buffer pools can specify a reserved piece of space at the start - this is the startOffset.
				currentBuffer.Offset = pool.BufferSize;

				// Fill the entire thing:
				Array.Copy(buffer, offset, currentBuffer.Bytes, startOffset, bufferSpaceSize);
				offset += bufferSpaceSize;
				count -= bufferSpaceSize;
			}
			
			// If count is non-zero, we'll need another buffer:
			if(count > 0){
				currentBuffer.After = pool.Get();
				currentBuffer = currentBuffer.After;
				currentBuffer.Offset += count;
				
				// Fill the last few bytes:
				Array.Copy(buffer, offset, currentBuffer.Bytes, pool.StartOffset, count);
			}
			
			return currentBuffer;
		}
		
	}
}