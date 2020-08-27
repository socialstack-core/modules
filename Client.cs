
using System;
using System.Net;

namespace Api.SocketServerLibrary
{

	/// <summary>
	/// Clients to a server.
	/// </summary>
	public partial class Client : SendQueue
	{
		/// <summary>The server object that this is a client on.</summary>
		public Server Server;
		/// <summary>The reader which is fed with data received via the socket.</summary>
		public readonly ClientReader Reader;
		/// <summary>True if hello is required.</summary>
		public bool Hello = true;
		/// <summary>The receive buffer used by this client. Just a shared reference to the SocketReader's byte array.</summary>
		public readonly byte[] ReceiveBuffer;


		/// <summary>
		/// Create a new client. Don't use this directly - it's called by the Server class.
		/// </summary>
		public Client()
		{
			Reader = new ClientReader(this);
			ReceiveBuffer = Reader.SetupData();
		}

		/// <summary>Start listening for data.</summary>
		public virtual void Start()
		{
			// Now able to send data:
			CanProcessSend = true;
			
			try
			{
				Socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, 0, OnReceiveData, this);
			}
			catch
			{
				Close();
			}
		}

		/// <summary>Remote IP.</summary>
		public IPAddress IP
		{
			get
			{
				if (Socket == null)
				{
					return null;
				}

				return ((IPEndPoint)(Socket.RemoteEndPoint)).Address;
			}
		}

		/// <summary>
		/// Used for the very first message. It helps detect websockets.
		/// </summary>
		private void OnReceiveData(IAsyncResult ar)
		{
			int bytesRead;

			try
			{
				bytesRead = Socket.EndReceive(ar);
			}
			catch
			{
				bytesRead = 0;
			}

			if (bytesRead == 0)
			{
				Close();
				return;
			}

			// Received some data into the buffer
			// Specifically 0->bytesRead:
			Reader.OnReceiveData(bytesRead);

			//Receive again:
			try
			{
				Socket.BeginReceive(ReceiveBuffer, 0, ReceiveBuffer.Length, 0, OnReceiveData, this);
			}
			catch
			{
				Close();
			}
		}
	}

}