using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace Api.SocketServerLibrary {

	/// <summary>
	/// Used when a context is provided to a callback.
	/// </summary>
	/// <param name="message"></param>
	public delegate void MessageDelegate<T>(T message);

	/// <summary>
	/// An opcode for a message to/ from the server.
	/// </summary>
	public class OpCode
	{
		/// <summary>
		/// True if this opcode is a request/ response system, and the message includes a request ID.
		/// </summary>
		public bool RequestId;
		
		/// <summary>
		/// True if this opcode is for a hello message.
		/// </summary>
		public bool IsHello;
		
		/// <summary>
		/// True if this opcode requires a user ID.
		/// </summary>
		public bool RequiresUserId;

		private object PoolLock = new object();

		/// <summary>
		/// Runs when the given message has started to be received.
		/// </summary>
		/// <param name="message"></param>
		public virtual void OnStartReceive(IMessage message)
		{
		}
		
		/// <summary>
		/// Runs when the given message has been completely received.
		/// </summary>
		/// <param name="message"></param>
		public virtual void OnReceive(IMessage message)
		{
		}

		/// <summary>
		/// Gets a message instance to use for this opcode.
		/// </summary>
		/// <returns></returns>
		public virtual IMessage GetAMessageInstance()
		{
			return null;
		}

		/*
		/// <summary>
		/// Uses ResponseCode. If it's 0, a Success response is sent. 
		/// Otherwise, a Fail one is sent instead. Optionally then releases the context.
		/// </summary>
		public void Respond(bool release = true)
		{
			if(ResponseCode > 0)
			{
				Fail((byte)ResponseCode, release);
			}
			else
			{
				Success(release);
			}
		}
		*/

		/// <summary>
		/// Used to setup a writer for user/ responseId. The writer is also set to the Writer property of the context.
		/// </summary>
		/// <returns></returns>
		public Writer SetupResponseWriter(IMessage message)
		{
			var writer = Writer.GetPooled();
			// 40 indicates a successful request reply.
			writer.Start(40);
			
			// Request ID is always first:
			writer.Write(message.RequestId);
			
			return writer;
		}

		/// <summary>
		/// This replies to a particular RequestId with a simple "done" success. Optionally then releases the context.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="release"></param>
		public void Success(IMessage message, bool release = true)
		{
			// 40 indicates a successful request reply.
			var writer = Writer.GetPooled();
			writer.Start(40);

			// Request ID is always first:
			writer.Write(message.RequestId);

			// Send it now:
			message.Client.Send(writer);

			if (release)
			{
				Release(message);
			}
		}

		/// <summary>
		/// Releases the given message object into this opcode pool.
		/// </summary>
		/// <param name="message"></param>
		public void Release(IMessage message)
		{
			if (message.Pooled)
			{
				return;
			}

			message.Pooled = true;

			// Pool the context object now:
			lock (PoolLock)
			{

				/*
				if (First == null)
				{
					First = this;
					message.After = null;
				}
				else
				{
					message.After = First;
					First = message;
				}
				*/
			}
		}

		/// <summary>
		/// This replies to a particular RequestId with an error code. Optionally releases the context.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="errorCode"></param>
		/// <param name="release"></param>
		public void Fail(IMessage message, byte errorCode, bool release = true)
		{
			// 41 indicates a failed request reply.
			var writer = Writer.GetPooled();
			writer.Start(41);
			
			// Request ID is always first:
			writer.Write(message.RequestId);
			
			writer.Write(errorCode);

			// Send it now:
			message.Client.Send(writer);
			
			if(release){
				Release(message);
			}
		}

		/// <summary>
		/// Kill closes the source and recycles the context in one easy call.
		/// </summary>
		public void Kill(IMessage message)
		{
			message.Client.Close();
			Done(message, true);
		}

		///<summary>Done method. Reads any following opcode or waits for one.</summary>
		public void DoneUnvalidated(IMessage message, bool alsoRelease)
		{
			// Message is done.
			if (alsoRelease && !message.Pooled)
			{
				// Pool the object now:
				message.Pooled = true;
				
				lock (PoolLock)
				{

					/*
					if (First == null)
					{
						First = this;
						message.After = null;
					}
					else
					{
						message.After = First;
						First = message;
					}
					*/
				}
			}

			if (message.Client.Socket != null)
			{
				// Got another opcode available already?
				message.Client.Reader.StartNextOpcode();
			}
		}

		/// <summary>
		/// The done method. Reads any following opcode or waits for one.
		/// </summary>
		public void Done(IMessage message, bool alsoRelease) {
			// Message is done.

#if DEBUG && SOCKET_SERVER_PROBE_ON
			Reader.ValidateDone();
#endif

			if (alsoRelease && !message.Pooled)
			{
				message.Pooled = true;

				// Pool the object now:
				lock (PoolLock)
				{
					/*
					if (First == null)
					{
						First = this;
						message.After = null;
					}
					else
					{
						message.After = First;
						First = message;
					}
					*/
				}
			}
			
			if(message.Client.Socket != null)
			{
				// Got another opcode available already?
				message.Client.Reader.StartNextOpcode();
			}
		}
	}

	/// <summary>
	/// An opcode handling the given message type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OpCode<T> : OpCode where T:IMessage, new()
	{
		/// <summary>
		/// The delegate which runs when this opcode triggers.
		/// </summary>
		public MessageDelegate<T> OnRequest;

		/// <summary>
		/// Creates a new opcode.
		/// </summary>
		public OpCode() {
			
		}

		/// <summary>
		/// Runs when the given message has started to be received.
		/// </summary>
		/// <param name="message"></param>
		public override void OnStartReceive(IMessage message)
		{
			// We have full control of the current client.
			// This must read the messages fields, then call OnReceive.
		}

		/// <summary>
		/// Runs when the given message has been completely received.
		/// </summary>
		/// <param name="message"></param>
		public override void OnReceive(IMessage message)
		{
			OnRequest((T)message);
		}

		/// <summary>
		/// Gets a message instance to use for this opcode.
		/// </summary>
		/// <returns></returns>
		public override IMessage GetAMessageInstance()
		{
			// Pool these.
			var msg = new T();
			msg.OpCode = this;
			return msg;
		}
	}
}