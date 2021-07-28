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
		/// The client this is in.
		/// </summary>
		public Client Client;

		/// <summary>
		/// Called when done sending.
		/// </summary>
		/// <param name="args"></param>
		protected override void OnCompleted(SocketAsyncEventArgs args)
		{
			Client.CompletedCurrentSend();
		}

	}
}