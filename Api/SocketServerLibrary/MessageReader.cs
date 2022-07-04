namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// Handles reading bytes from the stream for the current message.
	/// Note that these are shared instances and must not store any reader-specific state.
	/// </summary>
	public class MessageReader {

		/// <summary>
		/// Process is called when the reader has enough data available.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="client"></param>
		public virtual void Process(ref RecvStackFrame frame, Client client)
		{

		}

		/// <summary>
		/// Number of bytes required for phase 0 to start reading.
		/// </summary>
		public int FirstDataRequired;
		
	}

}