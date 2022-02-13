namespace Api.SocketServerLibrary
{
	/// <summary>
	/// This pools the allocation of blocks of 1024 bytes.
	/// Used primarily by messages being sent and received.
	/// </summary>
	public static class BinaryBufferPool{
		
		/// <summary>
		/// A lock for thread safety.
		/// </summary>
		public static readonly object PoolLock=new object();
		
		/// <summary>The size of buffers in this pool.</summary>
		public const int BufferSize=1024;
		
		/// <summary>
		/// The current front of the pool.
		/// </summary>
		public static BufferedBytes First;
		
		/// <summary>
		/// Finds the current pool size.
		/// </summary>
		/// <returns></returns>
		public static int PoolSize()
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
		public static BufferedBytes Get(){
			
			BufferedBytes result;
			
			lock(PoolLock){
			
				if(First==null){
					return new BufferedBytes(new byte[BufferSize],BufferSize);
				}
				
				result=First;
				First=result.After;
			
			}
			
			result.After=null;
			
			return result;
			
		}
		
	}
}