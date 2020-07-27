using System;
using System.Net;
using System.Net.Sockets;


namespace Api.SocketServerLibrary
{	
	/// <summary>
	/// SocketAsyncEventArgs for a particular sendqueue.
	/// </summary>
	public class SocketAsyncArgs : SocketAsyncEventArgs
	{
		/// <summary>
		/// The sendqueue that this is stored in.
		/// </summary>
		public SendQueue SendQueue;

		/// <summary>
		/// Called when done sending.
		/// </summary>
		/// <param name="args"></param>
		protected override void OnCompleted(SocketAsyncEventArgs args)
		{
			SendQueue.SendNextBlock();
		}

	}
}