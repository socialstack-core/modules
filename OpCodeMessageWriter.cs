using Api.Database;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.SocketServerLibrary
{
	/// <summary>
	/// Helps write messages.
	/// </summary>
	public class OpCodeMessageWriter
	{
		private uint OpCode;

		/// <summary>
		/// Creates a new message writer from the given field info.
		/// </summary>
		/// <param name="opCode">The opcode to use when sending</param>
		/// <param name="fieldInfo">The remote received field info which describes exactly what it will send us.</param>
		/// <param name="messageType">The system type of the IMessage object.</param>
		public OpCodeMessageWriter(uint opCode, List<string> fieldInfo, Type messageType)
		{
			OpCode = opCode;
			// FieldInfo is the set of fields that will be sent to us
			// Determine which we can receive, and silently drop ones we can't.

			var currentFieldType = messageType;

			if (fieldInfo == null)
			{
				return;
			}

			foreach (var fieldDescription in fieldInfo)
			{
				// FieldName=TYPEREF@FLAG1@FLAG2@..
				// TYPEREF is either C:ContentTypeId or just a number, indicating a core type.
				var switchToValue = false;
				var switchToMessage = false;

				var nameAndType = fieldDescription.Split('=');
				var fieldName = nameAndType[0];
				var typeAndSwitches = nameAndType[1];

				var typeParts = typeAndSwitches.Split('@');
				var typeRef = typeParts[0];

				Type fieldType;
				FieldTypeInfo fieldTypeInfo = null;

				if (typeRef[0] == 'C')
				{
					// Get the content type:
					fieldType = ContentTypes.GetType(int.Parse(typeRef.Substring(2)));
				}
				else
				{
					// Get the core type (int, string, bool etc - core stuff):
					fieldTypeInfo = OpCodeFieldTypes.Get(int.Parse(typeRef));
					fieldType = fieldTypeInfo?.Type;
				}

				MessageFieldMeta fm;

				if (fieldType == null)
				{
					// We don't have this type here! It's new on some other server (or completely removed).
					// We entirely ignore the message, but must still handle every field of it.
					fm = new MessageFieldMeta();
				}
				else
				{
					var fieldMetaType = typeof(MessageFieldMeta<>).MakeGenericType(new Type[] { fieldType });
					fm = Activator.CreateInstance(fieldMetaType) as MessageFieldMeta;
					fm.Field = currentFieldType.GetField(fieldName);
				}

				fm.FieldDescription = fieldDescription;

				if (fieldTypeInfo != null)
				{
					fm.OnRead = fieldTypeInfo.Reader;
					fm.SetWrite(fieldTypeInfo.Writer);
				}

				if (typeParts.Length > 1)
				{
					for (var i = 1; i < typeParts.Length; i++)
					{
						// Flag value is..
						var flag = typeParts[i];

						if (flag == "V")
						{
							switchToValue = true;
						}
						else if (flag == "M")
						{
							switchToMessage = true;
						}
					}
				}

				fm.ChangeToFieldValue = switchToValue;
				fm.ChangeToMessage = switchToMessage;
				Add(fm);

				if (switchToValue)
				{
					currentFieldType = fieldType;
				}

				if (switchToMessage)
				{
					currentFieldType = messageType;
				}
			}

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
		public Writer Write(object message)
		{
			var writer = Writer.GetPooled();
			writer.Start(OpCode);
			var current = FirstField;
			object ctx = message;

			while (current != null)
			{
				current.Write(writer, message, ref ctx);
				current = current.Next;
			}

			return writer;
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

	}
}