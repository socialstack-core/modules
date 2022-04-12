namespace Api.SocketServerLibrary
{
	/// <summary>
	/// This pools the allocation of blocks of n bytes.
	/// Used primarily by messages being sent and received.
	/// </summary>
	public class BinaryBufferPool{

		/// <summary>
		/// 1024 buffers
		/// </summary>
		public static BinaryBufferPool OneKb = new BinaryBufferPool(1024);

		/// <summary>
		/// A lock for thread safety.
		/// </summary>
		public readonly object PoolLock=new object();

		/// <summary>The size of buffers in this pool.</summary>
		public readonly int BufferSize;

		/// <summary>The current front of the pool.</summary>
		public BufferedBytes First;

		/// <summary>
		/// First writer in the pool.
		/// </summary>
		public Writer FirstCached;

		/// <summary>
		/// Thread safety for the writer pool.
		/// </summary>
		public readonly object WriterPoolLock = new object();

		/// <summary>
		/// Creates a pool of units of the given size
		/// </summary>
		/// <param name="bufferSize"></param>
		public BinaryBufferPool(int bufferSize)
		{
			BufferSize = bufferSize;
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
				buff = buff.After;
			}

			return count;
		}

		/// <summary>
		/// Get a buffer from the pool, or instances once.
		/// </summary>
		public BufferedBytes Get()
		{
			BufferedBytes result;
			
			lock(PoolLock){
			
				if(First==null){
					return new BufferedBytes(new byte[BufferSize], BufferSize, this);
				}
				
				result=First;
				First=result.After;
			
			}
			
			result.After=null;
			
			return result;
		}
		
	}
}