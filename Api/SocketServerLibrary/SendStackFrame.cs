namespace Api.SocketServerLibrary
{
	/// <summary>
	/// A frame on the read stack. Tracks the state of the data read head.
	/// </summary>
	public class SendStackFrame
	{
		private static object PoolLock = new object();
		
		/// <summary>
		/// First pooled object
		/// </summary>
		protected static SendStackFrame First;

		/// <summary>
		/// Releases this frame back to the pool.
		/// </summary>
		public void Release()
		{
			// Remove from Q:
			Writer.RemoveFromSendQueue();
			Writer = null;

			// Pool the context object now:
			lock (PoolLock)
			{
				After = First;
				First = this;
			}
		}
		
		/// <summary>
		/// Gets a message of this type from a pool.
		/// </summary>
		/// <returns></returns>
		public static SendStackFrame Get()
		{
			SendStackFrame frame;

			lock (PoolLock)
			{
				if (First == null)
				{
					frame = new SendStackFrame();
				}
				else
				{
					frame = First;
					First = frame.After;
				}
			}

			frame.After = null;
			return frame;
		}

		
		/// <summary>
		/// Linked list of frames.
		/// </summary>
		public SendStackFrame After;
		
		/// <summary>
		/// Current buffer in the writer.
		/// </summary>
		public BufferedBytes Current;
		
		/// <summary>
		/// The reader which will process the available bytes.
		/// </summary>
		public Writer Writer;
	}
}