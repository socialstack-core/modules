using System;
using System.Collections.Generic;

namespace Api.SocketServerLibrary
{
	
	/// <summary>
	/// Handles reading socket messages for a particular Client.
	/// </summary>
	public class ClientReader : SocketReader
	{
		/// <summary>
		/// The underlying client.
		/// </summary>
		public Client Source;

		private readonly Action<ushort> OnReadRequestId_D;
		private readonly Action<uint> OnReadUserId_D;
		/// <summary>
		/// Sets i32 to current field
		/// </summary>
		public readonly Action<int> ThenSetField_Int32_D;
		/// <summary>
		/// Sets nullable i32 to current field
		/// </summary>
		public readonly Action<int?> ThenSetField_NInt32_D;
		/// <summary>
		/// Sets nullable ui32 to current field
		/// </summary>
		public readonly Action<uint?> ThenSetField_NUInt32_D;
		/// <summary>
		/// Sets str to current field
		/// </summary>
		public readonly Action<BufferSegment> ThenSetField_String_D;
		/// <summary>
		/// Sets bool to current field
		/// </summary>
		public readonly Action<byte> ThenSetField_Bool_D;
		/// <summary>
		/// Sets byte to current field
		/// </summary>
		public readonly Action<byte> ThenSetField_Byte_D;
		/// <summary>
		/// Sets long to current field
		/// </summary>
		public readonly Action<long> ThenSetField_Int64_D;
		/// <summary>
		/// Sets ushort to current field
		/// </summary>
		public readonly Action<ushort> ThenSetField_UShort_D;
		/// <summary>
		/// Sets uint to current field
		/// </summary>
		public readonly Action<uint> ThenSetField_UInt32_D;
		/// <summary>
		/// Sets ulong to current field
		/// </summary>
		public readonly Action<ulong> ThenSetField_UInt64_D;
		/// <summary>
		/// Sets date to current field
		/// </summary>
		public readonly Action<DateTime> ThenSetField_DateTime_D;
		/// <summary>
		/// Sets date to current field
		/// </summary>
		public readonly Action<DateTime?> ThenSetField_NDateTime_D;
		/// <summary>
		/// Sets float to current field
		/// </summary>
		public readonly Action<float> ThenSetField_Float_D;
		/// <summary>
		/// Sets double to current field
		/// </summary>
		public readonly Action<double> ThenSetField_Double_D;
		/// <summary>
		/// Sets short to current field
		/// </summary>
		public readonly Action<short> ThenSetField_Short_D;
		/// <summary>
		/// Sets short to current field
		/// </summary>
		public readonly Action<BufferSegment> ThenSetField_ByteArray_D;
		/// <summary>
		/// Sets List of strings to current field
		/// </summary>
		public readonly Action<List<string>> ThenSetField_ListString_D;
		/// <summary>
		/// Advances to next field.
		/// </summary>
		public readonly Action ThenReadNextField_D;

		/// <summary>
		/// Current message being received.
		/// </summary>
		private IMessage CurrentMessage;

		/// <summary>
		/// Current object being written to when reading fields.
		/// </summary>
		private object CurrentObject;

		/// <summary>
		/// The current field being outputted to
		/// </summary>
		public MessageFieldMeta CurrentField;

		/// <summary>
		/// Read the next field
		/// </summary>
		public void ReadNextField()
		{
			if (CurrentField.ChangeToMessage)
			{
				CurrentObject = CurrentMessage;
			}

			CurrentField = CurrentField.Next;
			if (CurrentField == null)
			{
				CurrentMessage.OpCode.OnReceive(CurrentMessage);
			}
			else
			{
				if (CurrentField.ChangeToFieldValue)
				{
					// Instance a new field value and change current object to it:
					var obj = Activator.CreateInstance(CurrentField.Field.FieldType);
					CurrentField.Field.SetValue(CurrentObject, obj);
					CurrentObject = obj;
				}

				if (CurrentField.OnRead != null)
				{
					CurrentField.OnRead(this);
				}
				else
				{
					// Onward!
					ReadNextField();
				}
			}
		}

		/// <summary>
		/// Sets an int to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Int32(int value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets a nulalble int to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_NInt32(int? value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets a nulalble uint to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_NUInt32(uint? value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets a long to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Int64(long value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets an uint to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_UInt32(uint value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets a ulong to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_UInt64(ulong value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}

		/// <summary>
		/// Sets str to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_String(BufferSegment value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value.GetStringUTF16());
			}
			ReadNextField();
		}

		/// <summary>
		/// Sets bool to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Bool(byte value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, (value == 1));
			}
			ReadNextField();
		}

		/// <summary>
		/// Sets byte to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Byte(byte value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}

		/// <summary>
		/// Sets date to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_DateTime(DateTime value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets nullable date to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_NDateTime(DateTime? value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets float to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Float(float value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets double to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Double(double value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets short to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_Short(short value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets ushort to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_UShort(ushort value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets byte[] to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_ByteArray(BufferSegment value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value.AllocatedBuffer());
			}
			ReadNextField();
		}
		
		/// <summary>
		/// Sets List of strings to the current field value
		/// </summary>
		/// <param name="value"></param>
		public void ThenSetField_ListString(List<string> value)
		{
			if (CurrentField.Field != null)
			{
				CurrentField.Field.SetValue(CurrentObject, value);
			}
			ReadNextField();
		}
		
		/*
		/// <summary>
		/// Pushes a string into the current field List value
		/// </summary>
		/// <param name="value"></param>
		public void ThenPushField_String(BufferSegment value)
		{
			if (CurrentField.Field != null)
			{
				var str = value.GetStringUTF16();
				CurrentField.Field.SetValue(CurrentObject, value.AllocatedBuffer());
			}
			ReadNextField();
		}
		*/

		/// <summary>
		/// Creates a new client reader.
		/// </summary>
		/// <param name="source"></param>
		public ClientReader(Client source){
			Source = source;
			OnReadRequestId_D = new Action<ushort>(OnReadRequestId);
			OnReadUserId_D = new Action<uint>(OnReadUserId);
			ThenSetField_Int32_D = new Action<int>(ThenSetField_Int32);
			ThenSetField_NInt32_D = new Action<int?>(ThenSetField_NInt32);
			ThenSetField_NUInt32_D = new Action<uint?>(ThenSetField_NUInt32);
			ThenSetField_Int64_D = new Action<long>(ThenSetField_Int64);
			ThenSetField_String_D = new Action<BufferSegment>(ThenSetField_String);
			ThenSetField_Bool_D = new Action<byte>(ThenSetField_Bool);
			ThenSetField_Byte_D = new Action<byte>(ThenSetField_Byte);
			ThenSetField_DateTime_D = new Action<DateTime>(ThenSetField_DateTime);
			ThenSetField_NDateTime_D = new Action<DateTime?>(ThenSetField_NDateTime);
			ThenSetField_Float_D = new Action<float>(ThenSetField_Float);
			ThenSetField_Double_D = new Action<double>(ThenSetField_Double);
			ThenSetField_Short_D = new Action<short>(ThenSetField_Short);
			ThenSetField_ByteArray_D = new Action<BufferSegment>(ThenSetField_ByteArray);
			ThenSetField_ListString_D = new Action<List<string>>(ThenSetField_ListString);
			ThenReadNextField_D = new Action(ReadNextField);
			ThenSetField_UShort_D = new Action<ushort>(ThenSetField_UShort);
			ThenSetField_UInt32_D = new Action<uint>(ThenSetField_UInt32);
			ThenSetField_UInt64_D = new Action<ulong>(ThenSetField_UInt64);
		}

		private void OnReadRequestId(ushort value)
		{
			CurrentMessage.RequestId = value;
			if (CurrentMessage.OpCode.RequiresUserId)
			{
				// Also read user ID:
				ReadUInt32(OnReadUserId_D);
			}
			else
			{
				// Run opcode now:
				CurrentMessage.OpCode.OnStartReceive(CurrentMessage);
			}
		}
		
		private void OnReadUserId(uint value)
		{
			CurrentMessage.UserId = value;
			CurrentMessage.OpCode.OnStartReceive(CurrentMessage);
		}

		/// <summary>
		/// Called to read the next opcode.
		/// </summary>
		public override void ReadOpcode(){
			// Opcodes are stored as packed ints:
			ReadCompressed(ThenReadOpcode);
		}

		private void ThenReadOpcode(ulong opcode) {
			// Get the target opcode:
			OpCode target;

			if (Source.Server.FastOpCodeMap != null && ((int)opcode) < Source.Server.FastOpCodeMap.Length)
			{
				// Read from opcode map:
				target = Source.Server.FastOpCodeMap[(int)opcode];
			}
			else
			{
				Source.Server.OpCodeMap.TryGetValue((uint)opcode, out target);
			}

			if(target == null || (Source.Hello && !target.IsHello))
			{
				// Invalid opcode.
				Console.WriteLine("Invalid opcode received: " + opcode + ". " + Source.Hello + ", " + (target == null ? "[Not found]" : target.IsHello));
				Source.Socket.Close();
				return;
			}

			// Create a message for this opcode:
			CurrentMessage = target.GetAMessageInstance();
			CurrentMessage.Client = Source;
			CurrentObject = CurrentMessage;

			if (target.RequestId)
			{
				// Read context.RequestId first.
				ReadUInt16(OnReadRequestId_D);
			}
			else if (target.RequiresUserId)
			{
				// Read context.UserId. This is the actual user ID, not a network ID.
				ReadUInt32(OnReadUserId_D);
			}
			else
			{
				// Run msg receiving:
				target.OnStartReceive(CurrentMessage);
			}
		}
		
	}
}