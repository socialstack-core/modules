using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Api.SocketServerLibrary;
using Api.Database;
using System.Threading.Tasks;
using Api.Contexts;
//using Api.WebSockets;

namespace Api.SocketServerLibrary {

	/// <summary>
	/// An opcode for a message to/ from the server.
	/// </summary>
	public class OpCode
	{
		/// <summary>
		/// The numeric opcode.
		/// </summary>
		public uint Code;

		/// <summary>
		/// A message reader. Often this is a generated class, but can be custom crafted.
		/// </summary>
		public MessageReader MessageReader;

		/// <summary>
		/// True if this opcode has some sort of handler. Set this false to ignore an opcode entirely.
		/// </summary>
		public bool HasSomethingToDo = true;

		/// <summary>
		/// True if this opcode is for a hello message.
		/// </summary>
		public bool IsHello;
		
		/// <summary>
		/// Starts handling this opcode.
		/// </summary>
		/// <returns></returns>
		public virtual void Start(Client client)
		{
			// Reset the opcode frame:
			client.RecvStack[0].Phase = 0;
			client.RecvStack[0].BytesRequired = 1;

			// Push a stack frame which processes this message. It MUST pop itself.
			var msgReader = MessageReader;

			if (msgReader != null)
			{
				client.RecvStackPointer++;
				client.RecvStack[client.RecvStackPointer].Phase = 0;
				client.RecvStack[client.RecvStackPointer].Reader = msgReader;
				client.RecvStack[client.RecvStackPointer].BytesRequired = msgReader.FirstDataRequired;
			}
		}

	}

	/// <summary>
	/// Meta about a field in a message.
	/// </summary>
	public class MessageFieldMeta<T> : MessageFieldMeta
	{
		/// <summary>
		/// Called to write the field value to the message.
		/// </summary>
		public Action<Writer, T> OnWrite;


		/// <summary>
		/// Sets the write method handler.
		/// </summary>
		/// <param name="action">An Action with writer and a field of the value type T.</param>
		public override void SetWrite(object action)
		{
			OnWrite = (Action<Writer, T>)action;
		}

		/// <summary>
		/// Writes to the given writer
		/// </summary>
		/// <param name="writer">The writer to write to.</param>
		/// <param name="message">The message object.</param>
		/// <param name="currentObject">The current object being sent.</param>
		public override void Write(Writer writer, object message, ref object currentObject)
		{
			// Get the underlying value:
			var srcValue = (T)Field.GetValue(currentObject);

			if (ChangeToFieldValue)
			{
				// Context change to a child object. Must not be null.
				currentObject = srcValue;
			}

			if (OnWrite != null)
			{
				OnWrite(writer, srcValue);
			}

			if (ChangeToMessage)
			{
				// The last field in an object set
				currentObject = message;
			}

		}

	}

	/// <summary>
	/// Meta about a field in a message.
	/// </summary>
	public class MessageFieldMeta
	{
		/// <summary>
		/// A textual description of the field such that a 
		/// remote host can establish how to read it.
		/// </summary>
		public string FieldDescription;

		/// <summary>
		/// Changes the current context to the field's value.
		/// </summary>
		public bool ChangeToFieldValue;
		
		/// <summary>
		/// Changes the current context back to the message.
		/// </summary>
		public bool ChangeToMessage;

		/// <summary>
		/// Get/ set the value here.
		/// </summary>
		public FieldInfo Field;

		/// <summary>
		/// Next field
		/// </summary>
		public MessageFieldMeta Next;

		/// <summary>
		/// Writes to the given write stream
		/// </summary>
		/// <param name="writer">The writer to write to.</param>
		/// <param name="message">The message object.</param>
		/// <param name="currentObject">The current object being sent.</param>
		public virtual void Write(Writer writer, object message, ref object currentObject)
		{
		}

		/// <summary>
		/// Sets the write method handler.
		/// </summary>
		/// <param name="action">An Action with writer and a field of the value type T.</param>
		public virtual void SetWrite(object action)
		{
		}
	}

	/// <summary>
	/// Used when reading a complete message as-is into a writer.
	/// </summary>
	public class CompleteMessageOpCode : OpCode
	{
		/// <summary>
		/// The action to use
		/// </summary>
		public Action<Client, Writer> OnRequest;

		/// <summary>
		/// Start receive
		/// </summary>
		/// <param name="client"></param>
		public override void Start(Client client)
		{
			// Reset the opcode frame:
			client.RecvStack[0].Phase = 0;
			client.RecvStack[0].BytesRequired = 1;

			// Push a stack frame which processes this message. It MUST pop itself.
			var msgReader = MessageReader;

			// Start a writer with the opcode in it:
			var writer = Writer.GetPooled();
			writer.Start(Code);

			client.RecvStackPointer++;
			client.RecvStack[client.RecvStackPointer].Phase = 0;
			client.RecvStack[client.RecvStackPointer].Reader = msgReader;
			client.RecvStack[client.RecvStackPointer].TargetObject = writer; // can be null
			client.RecvStack[client.RecvStackPointer].BytesRequired = msgReader.FirstDataRequired;
		}
	}

	/// <summary>
	/// An opcode handling the given message type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class OpCode<T> : OpCode where T: Message<T>, new()
	{
		/// <summary>
		/// The delegate which runs when this opcode triggers.
		/// </summary>
		public Action<Client, T> OnRequest;

		/// <summary>
		/// The delegate which runs when this opcode triggers (async callback)
		/// </summary>
		public Func<Client, T, ValueTask> OnRequestAsync;

		/// <summary>
		/// Creates a new opcode.
		/// </summary>
		public OpCode() {

		}

		/// <summary>
		/// True if this opcode uses the async mode, where OnReceiveAsync will be called. Note that it MUST have a message type.
		/// </summary>
		public bool Async;

		/// <summary>
		/// Starts handling this opcode.
		/// </summary>
		/// <returns></returns>
		public override void Start(Client client)
		{
			// Reset the opcode frame:
			client.RecvStack[0].Phase = 0;
			client.RecvStack[0].BytesRequired = 1;

			// Create a message for this opcode:
			var msg = Message<T>.Get();
			msg.OpCode = this;
			msg.Client = client;
			
			// Push a stack frame which processes this message. It MUST pop itself.
			var msgReader = MessageReader;

			if (msgReader == null)
			{
				// Nothing else to read.
				if (HasSomethingToDo)
				{
					// trigger the callback, which usually happens on a task pool thread if it needs to do some work.
					OnReceive(client, msg);
				}
			}
			else
			{
				client.RecvStackPointer++;
				client.RecvStack[client.RecvStackPointer].Phase = 0;
				client.RecvStack[client.RecvStackPointer].Reader = msgReader;
				client.RecvStack[client.RecvStackPointer].TargetObject = msg; // can be null
				client.RecvStack[client.RecvStackPointer].BytesRequired = msgReader.FirstDataRequired;
			}
		}

		/// <summary>
		/// Runs when the given message has been completely received. The message object can be null.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="message"></param>
		public void OnReceive(Client client, T message)
		{
			// A message type is always required on asyncs.
			// That's because the callback delegate is stored on the message itself.
			if (Async)
			{
				if (message != null)
				{
					// The message object is used as a state holder. This async handler will ultimately call our OnRequestAsync func.
					_ = message.AsyncHandler();
				}
				return;
			}

			// Run the inline request:
			OnRequest(client, message);
			
			// Release the message:
			message.Release();
		}

	}
}