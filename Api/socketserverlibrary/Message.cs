using Api.Startup;
using System;
using System.Threading.Tasks;

namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// A particular message received by a socket. These objects are pooled.
	/// </summary>
	public class Message<T> where T : Message<T>, new()
	{

		/// <summary>
		/// Handles thread access to the pool.
		/// </summary>
		protected static object PoolLock = new object();

		/// <summary>
		/// First pooled object
		/// </summary>
		protected static Message<T> First;

		/// <summary>
		/// Releases this message object back to the pool.
		/// </summary>
		public void Release()
		{
			if (Pooled)
			{
				return;
			}

			Pooled = true;
			Client = null;
			OpCode = null;

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
		public static T Get()
		{
			T msg;

			lock (PoolLock)
			{
				if (First == null)
				{
					msg = new T();
				}
				else
				{
					msg = (T)First;
					First = msg.After;
					msg.Pooled = false;
				}
			}

			return msg;
		}

		private static byte[] basicHeader = new byte[5]; // Opcode followed by 4 byte size.
		private static BoltReaderWriter<T> _boltIO;

		/// <summary>
		/// Writes this message to a writer.
		/// </summary>
		/// <returns></returns>
		public Writer Write(byte opcode, Writer w = null)
		{
			byte[] buff;

			if (w == null)
			{
				w = Writer.GetPooled();
				w.Start(basicHeader);
				buff = w.FirstBuffer.Bytes;
				buff[0] = opcode;
			}
			else
			{
				buff = w.FirstBuffer.Bytes;
				w.Write(opcode);
			}
			
			// Get the type description:
			if (_boltIO == null)
			{
				_boltIO = BoltReaderWriter.Get<T>();
			}

			_boltIO.Write((T)this, w);

			// Get total length, minus the 5 byte header size:
			var length = w.Length - 5;

			buff[1] = (byte)length;
			buff[2] = (byte)(length >> 8);
			buff[3] = (byte)(length >> 16);
			buff[4] = (byte)(length >> 24);

			return w;
		}

		/// <summary>
		/// The opcode this message is for.
		/// </summary>
		[BoltIgnore]
		public OpCode<T> OpCode;

		/// <summary>
		/// The reader that read this message.
		/// </summary>
		[BoltIgnore]
		public Client Client;

		/// <summary>
		/// Next message when this one is in the pool.
		/// </summary>
		private Message<T> After;

		/// <summary>
		/// True if this object is currently pooled.
		/// </summary>
		private bool Pooled;


		/// <summary>
		/// Instances a new message. These are often pooled.
		/// </summary>
		public Message() {
			AsyncHandler = OnHandleAsync;
		}

		/// <summary>
		/// Cached delegate to avoid instancing one.
		/// </summary>
		[BoltIgnore]
		public Func<ValueTask> AsyncHandler;

		private async ValueTask OnHandleAsync()
		{
			await OpCode.OnRequestAsync(Client, (T)this);
			Release();
		}

	}

}