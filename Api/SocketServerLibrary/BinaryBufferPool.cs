using System;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// This pools the allocation of blocks of n bytes.
	/// Used primarily by messages being sent and received.
	/// </summary>
	public class BinaryBufferPool
	{

		/// <summary>
		/// 1024 buffers
		/// </summary>
		public static BinaryBufferPool<BufferedBytes> OneKb = new BinaryBufferPool<BufferedBytes>(1024);

		/// <summary>
		/// A lock for thread safety.
		/// </summary>
		public readonly object PoolLock=new object();

		/// <summary>The size of buffers in this pool.</summary>
		public readonly int BufferSize;
		
		/// <summary>The size of buffers in this pool minus the start offset.</summary>
		public readonly int BufferSpaceSize;

		/// <summary>
		/// When a buffer is taken from this pool, this is the offset to use.
		/// </summary>
		public readonly int StartOffset = 0;

		/// <summary>
		/// First writer in the pool.
		/// </summary>
		public Writer FirstCached;

		/// <summary>
		/// Thread safety for the writer pool.
		/// </summary>
		public readonly object WriterPoolLock = new object();

		/// <summary>
		/// True if the buffers are pinned.
		/// </summary>
		protected readonly bool Pinned;

		/// <summary>The current front of the pool.</summary>
		public BufferedBytes First;

		/// <summary>
		/// Creates a pool of units of the given size
		/// </summary>
		/// <param name="bufferSize"></param>
		/// <param name="pinned"></param>
		/// <param name="startOffset"></param>
		protected BinaryBufferPool(int bufferSize, bool pinned = false, int startOffset = 0)
		{
			BufferSize = bufferSize;
			Pinned = pinned;
			StartOffset = startOffset;
			BufferSpaceSize = bufferSize - startOffset;
		}

		/// <summary>
		/// Clears the writer pool.
		/// </summary>
		public void Clear()
		{
			lock (WriterPoolLock)
			{
				FirstCached = null;
			}

			lock (PoolLock)
			{
				First = null;
			}
		}

		/// <summary>
		/// Counts the current size of buffers in the pool.
		/// </summary>
		/// <returns></returns>
		public int WriterPoolSize()
		{
			int c = 0;
			var fc = FirstCached;
			while (fc != null)
			{
				c++;
				fc = fc.NextInLine;
			}

			return c;
		}

		/// <summary>
		/// Counts the current size of buffers in the pool.
		/// </summary>
		/// <returns></returns>
		public int BufferPoolSize()
		{
			int c = 0;
			var fc = First;
			while (fc != null)
			{
				c++;
				fc = fc.After;
			}

			return c;
		}

		/// <summary>
		/// Gets a writer which builds a series of the buffers from this pool.
		/// </summary>
		/// <returns></returns>
		public Writer GetWriter()
		{
			Writer result;

			lock (WriterPoolLock)
			{
				if (FirstCached == null)
				{
					return new Writer(this);
				}

				result = FirstCached;
				FirstCached = result.NextInLine;

			}

			result.PoolReset();
			return result;
		}

		/// <summary>
		/// Get or create a buffer from this pool.
		/// </summary>
		/// <returns></returns>
		public virtual BufferedBytes Get()
		{
			return null;
		}

	}

	/// <summary>
	/// This pools the allocation of blocks of n bytes.
	/// Used primarily by messages being sent and received.
	/// </summary>
	public class BinaryBufferPool<T> : BinaryBufferPool where T:BufferedBytes, new()
	{
		/// <summary>
		/// Create a new pool.
		/// </summary>
		/// <param name="bufferSize"></param>
		/// <param name="pinned"></param>
		/// <param name="startOffset"></param>
		public BinaryBufferPool(int bufferSize, bool pinned = false, int startOffset = 0) : base(bufferSize, pinned, startOffset)
		{
		}

		/// <summary>
		/// Finds the current pool size.
		/// </summary>
		/// <returns></returns>
		public int PoolSize()
		{
			var count = 0;
			var buff = First;
			while (buff != null)
			{
				count++;
				buff = buff.After as T;
			}

			return count;
		}

		/// <summary>
		/// Gets a buffer from this pool.
		/// </summary>
		/// <returns></returns>
		public override BufferedBytes Get()
		{
			BufferedBytes result;

			lock (PoolLock)
			{

				if (First == null)
				{
					result = new T();

					if (Pinned)
					{
						byte[] buffer = GC.AllocateUninitializedArray<byte>(length: BufferSize, pinned: true);
						result.Init(buffer, BufferSize, this);
					}
					else
					{
						result.Init(new byte[BufferSize], BufferSize, this);
					}

					result.Offset = StartOffset;
					return result;
				}

				result = First;
				First = result.After;

			}

			result.After = null;
			result.Offset = StartOffset;
			return result;
		}

	}
}