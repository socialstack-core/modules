
using System.Net.Sockets;

namespace Api.SocketServerLibrary
{
	/// <summary>
	/// A queue of messages waiting to be sent.
	/// It queues to avoid threading issues caused by multiple 
	/// threads attempting to send to the same client simultaneously.
	/// </summary>
	public class SendQueue : RequestIdStack
	{

		/// <summary>The underlying socket.</summary>
		public Socket Socket;

		/// <summary>Args used when sending messages.</summary>
		private readonly SocketAsyncArgs AsyncArgs;

		/// <summary>
		/// True if sending can be processed (i.e. the queue is empty).
		/// </summary>
		public bool CanProcessSend = true;

		/// <summary>
		/// The first writer to send in the linked list queue.
		/// </summary>
		private Writer PendingSendQueueFirst;

		/// <summary>
		/// The last writer to send in the linked list queue.
		/// </summary>
		private Writer PendingSendQueueLast;

		/// <summary>
		/// The active send buffer.
		/// </summary>
		private BufferedBytes SendingBuffer;

		/// <summary>
		/// A threading lock for this send queue.
		/// </summary>
		private readonly object SendLocker = new object();


		/// <summary>
		/// Creates a new send queue.
		/// </summary>
		public SendQueue() {
			AsyncArgs = new SocketAsyncArgs();
			AsyncArgs.SendQueue = this;
		}

		/// <summary>
		/// Shuts down the socket that this sendqueue is associated to.
		/// </summary>
		public virtual void Close()
		{
			// Destroy the link:
			if (Socket != null)
			{
				try
				{
					Socket.Shutdown(SocketShutdown.Both);
					Socket.Close();
					Socket = null;
				}
				catch { }
			}
		}

		/// <summary>
		/// Send the first hello message.
		/// </summary>
		/// <param name="writer"></param>
		public void SendHello(Writer writer)
		{
			CanProcessSend = true;
			Send(writer);
		}

		/// <summary>
		/// Called when a request ID is known.
		/// </summary>
		protected override void OnIdAvailable()
		{
			if (CanProcessSend || PendingSendQueueFirst == null)
			{
				return;
			}

			// Request IDs had been saturated but aren't anymore.
			// When that happens, sending completely shuts down.
			// This event indicates an ID is now available though, so sending can start up again.

			// Pop from queue and send:
			CanProcessSend = true;
			var writer = PendingSendQueueFirst;
			PendingSendQueueFirst = writer.NextInLine;
			Send(writer);
		}

		/// <summary>
		/// Sends a writer, potentially adding it to the queue.
		/// </summary>
		/// <param name="writer"></param>
		/// <returns></returns>
		public bool Send(Writer writer)
		{
			if (Socket == null)
			{
				// Nope! This server is unavailable.
				return false;
			}

			lock (SendLocker)
			{
				if (CanProcessSend)
				{
					// Send right now:
					CanProcessSend = false;

					if(writer.ContextForRequestId != null){
						var id = GetRequestId(writer.ContextForRequestId);
						
						if(id == -1){
							// Too many requests in flight on this connection. 
							// This writer should waiting in the queue until an ID is available.
							if (PendingSendQueueFirst == null)
							{
								// Add to queue (only entry)
								PendingSendQueueFirst = writer;
								PendingSendQueueLast = writer;
							}
							else
							{
								// Add to queue (at end)
								PendingSendQueueLast.NextInLine = writer;
								PendingSendQueueLast = writer;
								writer.NextInLine = null;
							}
							return true;
						}
						
						// First 2 bytes after the opcode are the request ID.
						writer.SetRequestId(id);
					}
					
					var buffer = writer.FirstBuffer;
					SendingBuffer = buffer;
					writer.SentRelease();
					
					AsyncArgs.SetBuffer(buffer.Bytes, buffer.Offset, buffer.Length);
					
					if (!Socket.SendAsync(AsyncArgs))
					{
						// It completed immediately
						SendNextBlock();
					}
				}
				else if (PendingSendQueueFirst == null)
				{
					// Add to queue (only entry)
					PendingSendQueueFirst = writer;
					PendingSendQueueLast = writer;
				}
				else
				{
					// Add to queue (at end)
					PendingSendQueueLast.NextInLine = writer;
					PendingSendQueueLast = writer;
					writer.NextInLine = null;
				}
			}
			return true;
		}

		/// <summary>
		/// Sends the next buffer in the outgoing queue.
		/// </summary>
		public void SendNextBlock()
		{
			var buffer = SendingBuffer.After;
			SendingBuffer.Release();

			if (buffer == null)
			{
				// Send next writer from queue if there is one.
				lock (SendLocker)
				{
					if (PendingSendQueueFirst == null)
					{
						CanProcessSend = true;
						return;
					}
					
					// Pop from queue:
					var writer = PendingSendQueueFirst;
					
					if(writer.ContextForRequestId != null){
						var id = GetRequestId(writer.ContextForRequestId);
						
						if(id == -1){
							// Too many requests in flight on this connection. 
							// This writer should continue waiting in the queue until an ID is available.
							return;
						}
						
						// First 2 bytes after the opcode are the request ID.
						writer.SetRequestId(id);
					}
					
					PendingSendQueueFirst = writer.NextInLine;
					buffer = writer.FirstBuffer;
					writer.SentRelease();
				}
			}
			
			SendingBuffer = buffer;
			AsyncArgs.SetBuffer(buffer.Bytes, buffer.Offset, buffer.Length);

			if (Socket.SendAsync(AsyncArgs))
			{
				return;
			}

			// It completed immediately
			SendNextBlock();
		}

		/// <summary>
		/// Sends an empty message which simply checks if the link is alive.
		/// </summary>
		public void Heartbeat(){
			try{
				Socket.Send(EmptyBytes,0,0);
			}catch{
				// It's disconnected and inactive - just drop the link now.
				Close();
			}
		}
		
		private static readonly byte[] EmptyBytes=new byte[0];
	}
}