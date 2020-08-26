using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using Api.SocketServerLibrary;
using Api.Database;

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
		/// The numeric opcode.
		/// </summary>
		public uint Code;

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
		/// Returns a list of the fields in the order they'll be written.
		/// </summary>
		/// <returns></returns>
		public virtual List<string> FieldList()
		{
			return null;
		}

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
		/// Called to read the field from the message.
		/// </summary>
		public Action<ClientReader> OnRead;
		
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
		/// The first field to read/ write.
		/// </summary>
		public MessageFieldMeta FirstField;
		/// <summary>
		/// Last field to read/ write.
		/// </summary>
		public MessageFieldMeta LastField;


		/// <summary>
		/// Writes the given message uisng the opcode field meta.
		/// </summary>
		/// <param name="message"></param>
		public Writer Write(T message)
		{
			var writer = Writer.GetPooled();
			writer.Start(Code);
			var current = FirstField;
			object ctx = message;

			while (current != null)
			{
				current.Write(writer, message, ref ctx);
				current = current.Next;
			}

			return writer;
		}

		/// <summary>
		/// Returns a list of the fields in the order they'll be written.
		/// </summary>
		/// <returns></returns>
		public override List<string> FieldList()
		{
			var fields = new List<string>();

			var current = FirstField;

			while (current != null)
			{
				fields.Add(current.FieldDescription);
				current = current.Next;
			}

			return fields;
		}

		/// <summary>
		/// Registers a field
		/// </summary>
		/// <param name="field"></param>
		/// <param name="prefix"></param>
		private void RegisterField(FieldInfo field, string prefix)
		{
			var fieldType = field.FieldType;
			
			var fieldMetaType = typeof(MessageFieldMeta<>).MakeGenericType(new Type[] { fieldType });
			var fm = Activator.CreateInstance(fieldMetaType) as MessageFieldMeta;
			fm.Field = field;
			
			// Get sys type:
			var fieldTypeInfo = OpCodeFieldTypes.Get(fieldType);

			if (fieldTypeInfo != null)
			{
				fm.FieldDescription = field.Name + "=" + fieldTypeInfo.Id;
				
				fm.OnRead = fieldTypeInfo.Reader;
				fm.SetWrite(fieldTypeInfo.Writer);
				Add(fm);
			}
			else if (prefix == "" && ContentTypes.IsContentType(fieldType))
			{
				// Register the fields of this sub-object as if it was inline with everything else.
				fm.FieldDescription =  field.Name + "=C:" + ContentTypes.GetId(fieldType.Name) + "@V";
				fm.ChangeToFieldValue = true;
				Add(fm);

				// Register all of the objects fields now:
				RegisterFields(fieldType, field.Name + ".");

				// NB: This works even if it registered no fields.
				LastField.ChangeToMessage = true;
				LastField.FieldDescription += "@M";
			}
			else
			{
				throw new Exception("A content type has an invalid field type which wasn't recognised: " + fieldType.ToString());
			}
		}

		private void Add(MessageFieldMeta fm)
		{
			if (LastField == null)
			{
				FirstField = LastField = fm;
			}
			else
			{
				LastField.Next = fm;
				LastField = fm;
			}
		}

		/// <summary>
		/// Auto field IO
		/// </summary>
		public void RegisterFields()
		{
			RegisterFields(typeof(T), "");
		}

		/// <summary>
		/// Auto field IO
		/// </summary>
		public void RegisterFields(Type fromType, string prefix)
		{
			// Get the public field set:
			var fieldSet = fromType.GetFields();

			foreach (var field in fieldSet)
			{
				RegisterField(field, prefix);
			}

		}

		/// <summary>
		/// Runs when the given message has started to be received.
		/// </summary>
		/// <param name="message"></param>
		public override void OnStartReceive(IMessage message)
		{
			// We have full control of the current client.
			// This must read the messages fields, then call OnReceive.

			if (FirstField == null)
			{
				OnReceive(message);
				return;
			}

			var reader = message.Client.Reader;
			reader.CurrentField = FirstField;
			FirstField.OnRead(reader);
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