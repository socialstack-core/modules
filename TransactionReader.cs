
using Api.SocketServerLibrary;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Transaction reader
/// </summary>
public class TransactionReader
{
	private Stream _stream;
	private Schema _schema;
	private Action<TransactionReader> _onTransaction;

	/// <summary>
	/// Transaction reader
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="schema"></param>
	/// <param name="onTransaction"></param>
	public TransactionReader(Stream stream, Schema schema, Action<TransactionReader> onTransaction)
	{
		_stream = stream;
		_schema = schema;
		_onTransaction = onTransaction;
	}

	/// <summary>
	/// Set this to true to stop the reader.
	/// </summary>
	public bool Halt = false;

	/// <summary>
	/// The number of fields.
	/// </summary>
	public int FieldCount;

	/// <summary>
	/// The fields in this transaction. Use FieldCount to identify how many are in use.
	/// </summary>
	public FieldData[] Fields = new FieldData[64];

	/// <summary>
	/// Linked list of buffers for storing binary transaction fields whilst the transaction is being read.
	/// </summary>
	private BufferedBytes FirstBuffer;

	/// <summary>
	/// Linked list of buffers for storing binary transaction fields whilst the transaction is being read.
	/// </summary>
	private BufferedBytes LastBuffer;

	/// <summary>
	/// Current buffer fill.
	/// </summary>
	private int BufferFill;

	/// <summary>
	/// The definition for the last read transaction.
	/// </summary>
	public Definition Definition;

	/// <summary>
	/// Current transaction ID. This is either simply the byte offset at the very first byte of the transaction (the first transaction therefore has a TxId of 0)
	/// or it is the value of the Id field, if the Id common field has been specified in the transaction.
	/// </summary>
	public ulong TransactionId;

	/// <summary>
	/// True if this reader should update the schema.
	/// </summary>
	public bool UpdateSchema;
	
	/// <summary>
	/// Gets the field ordinal (its index in Fields) for the field by the given field Id.
	/// </summary>
	/// <param name="fieldId"></param>
	/// <returns></returns>
	public int GetFieldOrdinal(ulong fieldId)
	{
		for (var i = 0; i < FieldCount; i++)
		{
			if (Fields[i].Field.Id == fieldId)
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Called when a transaction is in the buffer
	/// </summary>
	private void TransactionInForwardBuffer()
	{
		// Update the schema if needed
		var definitionId = Definition == null ? 0 : Definition.Id;

		if (definitionId <= 3 && UpdateSchema)
		{
			// This definition is going to update the schema.

			// Name field:
			var name = Fields[GetFieldOrdinal(Schema.NameDefId)].GetNativeString();

			if (definitionId == Schema.FieldDefId)
			{
				// A field
				var dataType = Fields[GetFieldOrdinal(Schema.DataTypeDefId)].GetNativeString();

				_schema.DefineField(name, dataType);
			}
			else
			{
				// A type - either the root one, or a derived type
				_schema.Define(name, definitionId);
			}
		}

		_onTransaction?.Invoke(this);

		// Release all the buffered bytes if there is any:
		var bytes = FirstBuffer;
		while (bytes != null)
		{
			var after = bytes.After;
			bytes.Release();
			bytes = after;
		}

		FirstBuffer = null;
		LastBuffer = null;
	}

	/// <summary>
	/// Called when a transaction is in the buffer
	/// </summary>
	private void TransactionInBackwardBuffer()
	{
		// Reverse direction does not need to update the schema (because it would be removing things from it, which is likely unnecessary in all scenarios).

		_onTransaction?.Invoke(this);

		// Release all the buffered bytes if there is any:
		var bytes = FirstBuffer;
		while (bytes != null)
		{
			var after = bytes.After;
			bytes.Release();
			bytes = after;
		}

		FirstBuffer = null;
		LastBuffer = null;
	}


	/// <summary>
	/// Obtains a buffer to store binary field data very briefly.
	/// </summary>
	/// <returns></returns>
	public BufferedBytes ObtainBuffer()
	{
		var buff = BinaryBufferPool.OneKb.Get();
		buff.Offset = 0;
		buff.Length = BinaryBufferPool.OneKb.BufferSize;

		if (FirstBuffer == null)
		{
			FirstBuffer = LastBuffer = buff;
		}
		else
		{
			LastBuffer.After = buff;
			LastBuffer = buff;
		}

		BufferFill = 0;
		return buff;
	}

	/// <summary>
	/// Writes the given amount of data at the given offset from the given buffer into the field buffers via block transfer.
	/// </summary>
	/// <param name="buffer"></param>
	/// <param name="offset"></param>
	/// <param name="length"></param>
	/// <param name="setStart">True if DataStart should be set on the current field.</param>
	private void WriteToFieldBuffers(byte[] buffer, int offset, int length, bool setStart = true)
	{
		if (length == 0)
		{
			if (setStart)
			{
				Fields[FieldCount].DataStart = 0;
			}
			return;
		}

		var bufferSize = BinaryBufferPool.OneKb.BufferSize;

		if (LastBuffer == null || bufferSize == BufferFill)
		{
			ObtainBuffer();
		}

		if (setStart)
		{
			Fields[FieldCount].DataStart = BufferFill;
			Fields[FieldCount].FirstBuffer = LastBuffer;
		}

		int space = BinaryBufferPool.OneKb.BufferSize - BufferFill;

		if (length <= space)
		{
			// Copy the bytes in:
			Array.Copy(buffer, offset, LastBuffer.Bytes, BufferFill, length);
			BufferFill += length;
			return;
		}

		// Fill the first buffer:
		Array.Copy(buffer, offset, LastBuffer.Bytes, BufferFill, space);
		BufferFill = bufferSize;
		length -= space;
		offset += space;

		// Fill full size buffers:
		while (length >= bufferSize)
		{
			ObtainBuffer();
			Array.Copy(buffer, offset, LastBuffer.Bytes, 0, bufferSize);
			offset += bufferSize;
			length -= bufferSize;
			BufferFill = bufferSize;
		}

		if (length > 0)
		{
			ObtainBuffer();
			Array.Copy(buffer, offset, LastBuffer.Bytes, 0, length);
			BufferFill = length;
		}
	}

	private enum ReadState : int {
		CompressedNumberStart = 0,
		CompressedNumber2Bytes = 1,
		CompressedNumber3Bytes = 2,
		CompressedNumber4Bytes = 3,
		CompressedNumber8Bytes = 4,
		DefinitionIdDone = 5,
		FieldCountDone = 6,
		FieldIdDone = 7,
		FieldValueLengthDone = 8,
		FieldBytes = 9,
		SecondaryFieldValueLengthDone = 10,
		SecondaryFieldIdDone = 11,
		SecondaryFieldCountDone = 12,
		SecondaryDefinitionIdDone = 13
	}

	/// <summary>
	/// Loads the transactions from the open read stream in the backwards direction.
	/// This is useful for state loading, as you can e.g. skip setting values which are overriden by future transactions.
	/// </summary>
	/// <param name="blockchainOffset"></param>
	/// <param name="bufferSize"></param>
	public void ReadBackwards(ulong blockchainOffset = 0, int bufferSize = 16384)
	{
		bufferSize = 1024;

		var str = _stream;
		var virtualStreamPosition = str.Length;

		var readBuffer = new byte[bufferSize];

		// A transaction is:
		// [definitionId][fieldCount][[fieldType][fieldValue][fieldType]][fieldCount][definitionId]
		// Components of the transaction are duplicated for both integrity checking and also such that the transaction can be read backwards as well as forwards.
		// Virtually all of these components are the same thing - a compressed number (field values are often the same too), so reading compressed numbers quickly is important.
		ulong compressedNumber = 0;
		ulong definitionId = 0;
		int fieldCount = 0;
		ReadState state = ReadState.CompressedNumberStart;
		ReadState txState = ReadState.DefinitionIdDone;
		var part = 0;
		int fieldDataSize = 0;
		FieldDefinition field = null;
		int fieldDataSoFar = 0;
		bool customTxId = false;

		while (virtualStreamPosition > 0)
		{
			int read;

			virtualStreamPosition -= bufferSize;

			if (virtualStreamPosition < 0)
			{
				str.Position = 0;
				var toRead = bufferSize + (int)virtualStreamPosition;
				read = str.Read(readBuffer, 0, toRead);
				virtualStreamPosition = 0;

				if (toRead != read)
				{
					virtualStreamPosition += (toRead - read);
				}
			}
			else
			{
				str.Position = virtualStreamPosition;
				read = str.Read(readBuffer, 0, bufferSize);

				if (read != bufferSize)
				{
					// Offset by the amount that wasn't actually read:
					virtualStreamPosition += (bufferSize - read);
				}
			}
			
			var i = read - 1;

			while (i >= 0)
			{
				// The current byte is used based on what the current read state is.
				
				switch (state)
				{
					case ReadState.CompressedNumberStart:
						compressedNumber = readBuffer[i--];

						if (compressedNumber < 251)
						{
							// its value is just the first byte
							state = txState;
						}
						else if (compressedNumber == 251)
						{
							// 3 bytes needed
							if (i >= 2)
							{
								compressedNumber = ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 251)
								{
									throw new Exception("Integrity failure - invalid 2 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber2Bytes;
								part = 8;
							}
						}
						else if (compressedNumber == 252)
						{
							// 4 bytes needed
							if (i >= 3)
							{
								compressedNumber = ((ulong)readBuffer[i--] << 16) | ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 252)
								{
									throw new Exception("Integrity failure - invalid 3 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber3Bytes;
								part = 16;
							}
						}
						else if (compressedNumber == 253)
						{
							// 5 bytes needed
							if (i >= 4)
							{
								compressedNumber = ((ulong)readBuffer[i--] << 24) | ((ulong)readBuffer[i--] << 16) | ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 253)
								{
									throw new Exception("Integrity failure - invalid 4 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber4Bytes;
								part = 24;
							}
						}
						else if (compressedNumber == 254)
						{
							// 9 bytes needed
							if (i >= 8)
							{
								compressedNumber = ((ulong)readBuffer[i--] << 56) | ((ulong)readBuffer[i--] << 48) | ((ulong)readBuffer[i--] << 40) | ((ulong)readBuffer[i--] << 32) | 
									((ulong)readBuffer[i--] << 24) | ((ulong)readBuffer[i--] << 16) | ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 254)
								{
									throw new Exception("Integrity failure - invalid 8 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber8Bytes;
								part = 56;
							}
						}
						break;
					case ReadState.CompressedNumber2Bytes:
						// 2 byte number (partial)

						if (part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 251)
							{
								throw new Exception("Integrity failure - invalid 2 byte compressed number.");
							}

							part = -9;
						}
						else if (part == -9)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i--] << part);
							part -= 8;
						}

						break;
					case ReadState.CompressedNumber3Bytes:
						// 3 byte number (partial)

						if (part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 252)
							{
								throw new Exception("Integrity failure - invalid 3 byte compressed number.");
							}

							part = -9;
						}
						else if (part == -9)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i--] << part);
							part -= 8;
						}

						break;
					case ReadState.CompressedNumber4Bytes:
						// 4 byte number (partial)

						if (part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 253)
							{
								throw new Exception("Integrity failure - invalid 4 byte compressed number.");
							}

							part = -9;
						}
						else if (part == -9)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i--] << part);
							part -= 8;
						}

						break;
					case ReadState.CompressedNumber8Bytes:
						// 8 byte number (partial)
						if (part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 254)
							{
								throw new Exception("Integrity failure - invalid 8 byte compressed number.");
							}

							part = -9;
						}
						else if (part == -9)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i--] << part);
							part -= 8;
						}

						break;
					case ReadState.DefinitionIdDone:
						// done reading definition ID
						definitionId = compressedNumber;

						Definition = _schema.Get((int)definitionId);

						if (Definition == null)
						{
							if (definitionId <= 3)
							{
								Definition = _schema.GetTemporaryCriticalDefinition((int)definitionId);
							}
							else
							{
								throw new Exception("Definition does not exist with ID " + definitionId);
							}
						}

						txState = ReadState.FieldCountDone;
						state = ReadState.CompressedNumberStart;
						break;
					case ReadState.FieldCountDone:
						// done reading field count
						fieldCount = (int)compressedNumber;
						FieldCount = 0;

						if (fieldCount == 0)
						{
							// A 0 field count is not duplicated. The definition ID is though, so we read that next:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryDefinitionIdDone;
						}
						else
						{

							if (fieldCount > 4096)
							{
								// Too many fields in one transaction
								throw new Exception("Max of 4096 fields can be set in one transcation");
							}

							if ((int)fieldCount > Fields.Length)
							{
								Array.Resize(ref Fields, 4096);
							}

							// Read field ID:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.FieldIdDone;
						}
						break;
					case ReadState.FieldIdDone:
						// Get field definition (can be null):
						field = _schema.GetField((int)compressedNumber);

						if (field == null)
						{
							if (compressedNumber <= 3)
							{
								// This happens whilst the schema is loading.
								// Use a temporary field ref.
								field = _schema.GetTemporaryCriticalField((int)compressedNumber);
							}
							else
							{
								throw new Exception("Unknown field ID: " + compressedNumber);
							}
						}

						Fields[FieldCount].Field = field;

						// Get the field data size:
						fieldDataSize = field.FieldDataSize;

						if (fieldDataSize == -1)
						{
							// It's variable - start reading the number:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.FieldValueLengthDone;
						}
						else
						{
							// Reading a fixed number of bytes:
							Fields[FieldCount].DataLength = fieldDataSize;

							if (i+1 >= fieldDataSize)
							{
								// Already available in the buffer. Transfer it straight over
								i -= fieldDataSize;
								WriteToFieldBuffers(readBuffer, i+1, fieldDataSize);

								FieldCount++;
								state = ReadState.CompressedNumberStart;
								txState = ReadState.SecondaryFieldIdDone;
							}
							else
							{
								// Go to partial field read state:
								fieldDataSoFar = i+1;
								i = -1;
								WriteToFieldBuffers(readBuffer, 0, fieldDataSoFar);
								state = ReadState.FieldBytes;
							}
						}
						break;
					case ReadState.FieldValueLengthDone:
						if (field.SizeIsValue)
						{
							// The size we have is the base of the value that we'll be using
							Fields[FieldCount].NumericValue = compressedNumber;
							Fields[FieldCount].IsNull = false;

							// We've therefore completed reading this field value and should proceed to the next one.
							FieldCount++;
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryFieldIdDone;

							if (field.Id == Schema.IdDefId)
							{
								// The field value overwrites the TransactionId:
								TransactionId = compressedNumber;
								customTxId = true;
							}
						}
						else
						{
							// Read size bytes next, unless it is nullable.
							if (field.IsNullable)
							{
								if (compressedNumber == 0)
								{
									// It's null
									Fields[FieldCount].IsNull = true;

									// We've therefore completed reading this field value and should proceed to the next one.
									FieldCount++;
									state = ReadState.CompressedNumberStart;
									txState = ReadState.SecondaryFieldIdDone;
								}
								else
								{
									// It's not null - there are some bytes (might be 0) to read
									Fields[FieldCount].IsNull = false;
									fieldDataSize = ((int)compressedNumber) - 1;
									Fields[FieldCount].DataLength = fieldDataSize;

									if (i+1 >= fieldDataSize)
									{
										// All bytes of the field are available already.
										i -= fieldDataSize;
										WriteToFieldBuffers(readBuffer, i+1, fieldDataSize);

										FieldCount++;
										state = ReadState.CompressedNumberStart;
										txState = fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
									}
									else
									{
										// Go to partial field read state:
										fieldDataSoFar = i + 1;
										i = -1;
										WriteToFieldBuffers(readBuffer, 0, fieldDataSoFar);
										state = ReadState.FieldBytes;
									}
								}
							}
							else
							{
								Fields[FieldCount].IsNull = false;
								fieldDataSize = (int)compressedNumber;
								Fields[FieldCount].DataLength = fieldDataSize;

								if (i + 1 >= fieldDataSize)
								{
									// All bytes of the field are available already.
									i -= fieldDataSize;
									WriteToFieldBuffers(readBuffer, i + 1, fieldDataSize);

									FieldCount++;
									state = ReadState.CompressedNumberStart;
									txState = fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
								}
								else
								{
									// Go to partial field read state:
									fieldDataSoFar = i + 1;
									i = -1;
									WriteToFieldBuffers(readBuffer, 0, fieldDataSoFar);
									state = ReadState.FieldBytes;
								}

							}
						}
						break;
					case ReadState.FieldBytes:
						// Read part of a field value

						#warning wrong order!
						// We're going backwards - this appends a block of bytes at the end of the current field value

						var bytesToCopy = fieldDataSize - fieldDataSoFar;
						var bytesAvailable = i + 1;
						if (bytesToCopy > bytesAvailable)
						{
							bytesToCopy = bytesAvailable;
						}

						WriteToFieldBuffers(readBuffer, i - bytesToCopy + 1, bytesToCopy, false);
						fieldDataSoFar += bytesToCopy;
						i -= bytesToCopy;

						if (fieldDataSoFar == fieldDataSize)
						{
							// Done reading this field value. Go to duplicated field length next.
							FieldCount++;
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryFieldValueLengthDone;
						}
					break;
					case ReadState.SecondaryFieldValueLengthDone:
						// Occurs on string and byte fields - the value length is present twice.

						var compareSize = (field.IsNullable) ? (int)(compressedNumber - 1) : (int)compressedNumber;

						if (compareSize != fieldDataSize)
						{
							throw new Exception("Integrity failure - field data sizes do not match (Expected " + fieldDataSize + ", got " + compareSize + ")");
						}

						// Go to secondary field ID next:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.SecondaryFieldIdDone;
						break;
					case ReadState.SecondaryFieldIdDone:
						// All fields have 2 field IDs.

						if (compressedNumber != field.Id)
						{
							throw new Exception("Integrity failure - secondary field ID does not match the first (Expected " + field.Id + ", got " + compressedNumber + ")");
						}

						state = ReadState.CompressedNumberStart;

						// Next state - either reading the secondary field count, or the next field ID:
						txState = (fieldCount == FieldCount) ? ReadState.SecondaryFieldCountDone : ReadState.FieldIdDone;
						break;
					case ReadState.SecondaryFieldCountDone:
						// Secondary field count (if there is one - field count 0 does not get doubled up)

						if (FieldCount != (int)compressedNumber)
						{
							throw new Exception("Integrity failure - the secondary field count does not match the first (Expected " + FieldCount + ", got " + compressedNumber + ")");
						}

						// Read secondary def ID:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.SecondaryDefinitionIdDone;
						break;
					case ReadState.SecondaryDefinitionIdDone:
						// Secondary definition ID
						definitionId = Definition == null ? 0 : Definition.Id;

						if (definitionId != compressedNumber)
						{
							throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + definitionId + ", got " + compressedNumber + ")");
						}

						// Current ID:
						if (!customTxId)
						{
							TransactionId = blockchainOffset + (ulong)(virtualStreamPosition + i + 1);
						}

						TransactionInBackwardBuffer();

						if (Halt)
						{
							return;
						}

						customTxId = false;

						// Reset:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.DefinitionIdDone;

						break;
				}
			}
		}

		if (state != ReadState.SecondaryDefinitionIdDone)
		{
			throw new Exception("Integrity failure - partial bytes of a transaction at the beginning of the provided file");
		}

		// Because i was -1, the very last transaction has not been handled yet.
		definitionId = Definition == null ? 0 : Definition.Id;

		if (definitionId != compressedNumber)
		{
			throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + definitionId + ", got " + compressedNumber + ")");
		}

		// Current ID:
		if (!customTxId)
		{
			TransactionId = blockchainOffset + (ulong)virtualStreamPosition;
		}

		TransactionInBackwardBuffer();
	}

	/// <summary>
	/// Loads the transactions from the open read stream in the forwards direction.
	/// </summary>
	/// <param name="bufferSize">The size of the read buffer. Usually you don't need to change this.</param>
	/// <param name="blockchainOffset">
	/// The total size, in bytes, of other parts of the blockchain before the one that is being read from here.
	/// If the blockchain is in one file, this value is 0. If you have partial files, you can obtain it via the "Byte Offset" field in the nearest block boundary transaction.
	/// </param>
	/// <exception cref="Exception"></exception>
	public void Read(ulong blockchainOffset = 0, int bufferSize = 16384)
	{
		var str = _stream;
		str.Seek(0, SeekOrigin.Begin);

		var readBuffer = new byte[bufferSize];

		// A transaction is:
		// [definitionId][fieldCount][[fieldType][fieldValue][fieldType]][fieldCount][definitionId]
		// Components of the transaction are duplicated for both integrity checking and also such that the transaction can be read backwards as well as forwards.
		// Virtually all of these components are the same thing - a compressed number (field values are often the same too), so reading compressed numbers quickly is important.
		ulong compressedNumber = 0;
		ulong definitionId = 0;
		int fieldCount = 0;
		ReadState state = ReadState.CompressedNumberStart;
		ReadState txState = ReadState.DefinitionIdDone;
		var part = 0;
		int fieldDataSize = 0;
		FieldDefinition field = null;
		int fieldDataSoFar = 0;
		TransactionId = blockchainOffset;

		while (str.Position < str.Length)
		{
			var read = str.Read(readBuffer, 0, bufferSize);

			var i = 0;

			while (i < read)
			{
				// The current byte is used based on what the current read state is.
				switch (state)
				{
					case ReadState.CompressedNumberStart:
						compressedNumber = readBuffer[i++];

						if (compressedNumber < 251)
						{
							// its value is just the first byte
							state = txState;
						}
						else if (compressedNumber == 251)
						{
							// 3 bytes needed
							if ((i + 3) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 251)
								{
									throw new Exception("Integrity failure - invalid 2 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber2Bytes;
								part = 0;
							}
						}
						else if (compressedNumber == 252)
						{
							// 4 bytes needed
							if ((i + 4) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8) | ((ulong)readBuffer[i++] << 16);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 252)
								{
									throw new Exception("Integrity failure - invalid 3 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber3Bytes;
								part = 0;
							}
						}
						else if (compressedNumber == 253)
						{
							// 5 bytes needed
							if ((i + 5) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8) | ((ulong)readBuffer[i++] << 16) | ((ulong)readBuffer[i++] << 24);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 253)
								{
									throw new Exception("Integrity failure - invalid 4 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber4Bytes;
								part = 0;
							}
						}
						else if (compressedNumber == 254)
						{
							// 9 bytes needed
							if ((i + 9) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8) | ((ulong)readBuffer[i++] << 16) | ((ulong)readBuffer[i++] << 24) |
					((ulong)readBuffer[i++] << 32) | ((ulong)readBuffer[i++] << 40) | ((ulong)readBuffer[i++] << 48) | ((ulong)readBuffer[i++] << 56);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 254)
								{
									throw new Exception("Integrity failure - invalid 8 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber8Bytes;
								part = 0;
							}
						}
					break;
					case ReadState.CompressedNumber2Bytes:
						// 2 byte number (partial)
						
						if (part == 16)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 251)
							{
								throw new Exception("Integrity failure - invalid 2 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 24)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.CompressedNumber3Bytes:
						// 3 byte number (partial)
						
						if (part == 24)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 252)
							{
								throw new Exception("Integrity failure - invalid 3 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 32)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.CompressedNumber4Bytes:
						// 4 byte number (partial)
						
						if (part == 32)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 253)
							{
								throw new Exception("Integrity failure - invalid 4 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 40)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.CompressedNumber8Bytes:
						// 8 byte number (partial)
						if (part == 64)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 254)
							{
								throw new Exception("Integrity failure - invalid 8 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 72)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.DefinitionIdDone:
						// done reading definition ID
						definitionId = compressedNumber;

						Definition = _schema.Get((int)definitionId);

						if (Definition == null)
						{
							if (definitionId <= 3)
							{
								Definition = _schema.GetTemporaryCriticalDefinition((int)definitionId);
							}
							else
							{
								throw new Exception("Definition does not exist with ID " + definitionId);
							}
						}

						txState = ReadState.FieldCountDone;
						state = ReadState.CompressedNumberStart;
					break;
					case ReadState.FieldCountDone:
						// done reading field count
						fieldCount = (int)compressedNumber;
						FieldCount = 0;

						if (fieldCount == 0)
						{
							// A 0 field count is not duplicated. The definition ID is though, so we read that next:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryDefinitionIdDone;
						}
						else
						{

							if (fieldCount > 4096)
							{
								// Too many fields in one transaction
								throw new Exception("Max of 4096 fields can be set in one transcation");
							}

							if ((int)fieldCount > Fields.Length)
							{
								Array.Resize(ref Fields, 4096);
							}

							// Read field ID:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.FieldIdDone;
						}
					break;
					case ReadState.FieldIdDone:
						// Get field definition (can be null):
						field = _schema.GetField((int)compressedNumber);

						if (field == null)
						{
							if (compressedNumber <= 3)
							{
								// This happens whilst the schema is loading.
								// Use a temporary field ref.
								field = _schema.GetTemporaryCriticalField((int)compressedNumber);
							}
							else
							{
								throw new Exception("Unknown field ID: " + compressedNumber);
							}
						}

						Fields[FieldCount].Field = field;

						// Get the field data size:
						fieldDataSize = field.FieldDataSize;

						if (fieldDataSize == -1)
						{
							// It's variable - start reading the number:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.FieldValueLengthDone;
						}
						else
						{
							// Reading a fixed number of bytes:
							Fields[FieldCount].DataLength = fieldDataSize;

							if (i + fieldDataSize <= read)
							{
								// Already available in the buffer. Transfer it straight over
								WriteToFieldBuffers(readBuffer, i, fieldDataSize);
								i += fieldDataSize;

								FieldCount++;
								state = ReadState.CompressedNumberStart;
								txState = ReadState.SecondaryFieldIdDone;
							}
							else
							{
								// Go to partial field read state:
								fieldDataSoFar = read - i;
								WriteToFieldBuffers(readBuffer, i, fieldDataSoFar);
								i = read;
								state = ReadState.FieldBytes;
							}
						}
					break;
					case ReadState.FieldValueLengthDone:
						if (field.SizeIsValue)
						{
							// The size we have is the base of the value that we'll be using
							Fields[FieldCount].NumericValue = compressedNumber;
							Fields[FieldCount].IsNull = false;

							// We've therefore completed reading this field value and should proceed to the next one.
							FieldCount++;
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryFieldIdDone;

							if (field.Id == Schema.IdDefId)
							{
								// The field value overwrites the TransactionId:
								TransactionId = compressedNumber;
							}
						}
						else
						{
							// Read size bytes next, unless it is nullable.
							if (field.IsNullable)
							{
								if (compressedNumber == 0)
								{
									// It's null
									Fields[FieldCount].IsNull = true;

									// We've therefore completed reading this field value and should proceed to the next one.
									FieldCount++;
									state = ReadState.CompressedNumberStart;
									txState = ReadState.SecondaryFieldIdDone;
								}
								else
								{
									// It's not null - there are some bytes (might be 0) to read
									Fields[FieldCount].IsNull = false;
									fieldDataSize = ((int)compressedNumber) - 1;
									Fields[FieldCount].DataLength = fieldDataSize;

									if (i + fieldDataSize <= read)
									{
										// All bytes of the field are available already.
										WriteToFieldBuffers(readBuffer, i, fieldDataSize);
										i += fieldDataSize;

										FieldCount++;
										state = ReadState.CompressedNumberStart;
										txState = fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
									}
									else
									{
										// Go to partial field read state:
										fieldDataSoFar = read - i;
										WriteToFieldBuffers(readBuffer, i, fieldDataSoFar);
										i = read;
										state = ReadState.FieldBytes;
									}
								}
							}
							else
							{
								Fields[FieldCount].IsNull = false;
								fieldDataSize = (int)compressedNumber;
								Fields[FieldCount].DataLength = fieldDataSize;

								if (i + fieldDataSize <= read)
								{
									// All bytes of the field are available already.
									WriteToFieldBuffers(readBuffer, i, fieldDataSize);
									i += fieldDataSize;
									
									FieldCount++;
									state = ReadState.CompressedNumberStart;
									txState = fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
								}
								else
								{
									// Go to partial field read state:
									fieldDataSoFar = read - i;
									WriteToFieldBuffers(readBuffer, i, fieldDataSoFar);
									i = read;
									state = ReadState.FieldBytes;
								}

							}
						}
					break;
					case ReadState.FieldBytes:
						// Read part of a field value

						var bytesToCopy = fieldDataSize - fieldDataSoFar;
						var bytesAvailable = read - i;
						if (bytesToCopy > bytesAvailable)
						{
							bytesToCopy = bytesAvailable;
						}

						WriteToFieldBuffers(readBuffer, i, bytesToCopy, false);
						fieldDataSoFar += bytesToCopy;
						i += bytesToCopy;

						if (fieldDataSoFar == fieldDataSize)
						{
							// Done reading this field value. Go to duplicated field length next.
							FieldCount++;
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryFieldValueLengthDone;
						}
					break;
					case ReadState.SecondaryFieldValueLengthDone:
						// Occurs on string and byte fields - the value length is present twice.

						var compareSize = (field.IsNullable) ? (int)(compressedNumber - 1) : (int)compressedNumber;

						if (compareSize != fieldDataSize)
						{
							throw new Exception("Integrity failure - field data sizes do not match (Expected " + fieldDataSize + ", got " + compareSize + ")");
						}

						// Go to secondary field ID next:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.SecondaryFieldIdDone;
					break;
					case ReadState.SecondaryFieldIdDone:
						// All fields have 2 field IDs.

						if (compressedNumber != field.Id)
						{
							throw new Exception("Integrity failure - secondary field ID does not match the first (Expected " + field.Id + ", got " + compressedNumber + ")");
						}

						state = ReadState.CompressedNumberStart;

						// Next state - either reading the secondary field count, or the next field ID:
						txState = (fieldCount == FieldCount) ? ReadState.SecondaryFieldCountDone : ReadState.FieldIdDone;
					break;
					case ReadState.SecondaryFieldCountDone:
						// Secondary field count (if there is one - field count 0 does not get doubled up)

						if (FieldCount != (int)compressedNumber)
						{
							throw new Exception("Integrity failure - the secondary field count does not match the first (Expected " + FieldCount + ", got " + compressedNumber + ")");
						}

						// Read secondary def ID:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.SecondaryDefinitionIdDone;
					break;
					case ReadState.SecondaryDefinitionIdDone:
						// Secondary definition ID
						definitionId = Definition == null ? 0 : Definition.Id;

						if (definitionId != compressedNumber)
						{
							throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + definitionId + ", got " + compressedNumber + ")");
						}

						TransactionInForwardBuffer();

						if (Halt)
						{
							return;
						}

						// Reset:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.DefinitionIdDone;

						// Next tx ID:
						TransactionId = blockchainOffset + (ulong)i;

						break;
				}

			}

			blockchainOffset += (ulong)read;
		}

		if (state != ReadState.SecondaryDefinitionIdDone)
		{
			throw new Exception("Integrity failure - partial bytes of a transaction at the end of the provided file");
		}

		// Because i was -1, the very last transaction has not been handled yet.
		definitionId = Definition == null ? 0 : Definition.Id;

		if (definitionId != compressedNumber)
		{
			throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + definitionId + ", got " + compressedNumber + ")");
		}

		TransactionInForwardBuffer();
	}

	/// <summary>
	/// Checks the integrity of the given stream of transaction bytes in a writer. Throws if the integrity check fails.
	/// This method is thread safe. Note that it does not update the schema; 
	/// a schema update transaction must therefore be in a separate block of transactions if a transaction in the same writer depends on it.
	/// </summary>
	/// <param name="writer">Contains transactions.</param>
	/// <param name="offset">MUST be within the first buffer.</param>
	/// <param name="length">Amount of bytes representing the transactions to add.</param>
	/// <param name="schema">Schema to use.</param>
	public static void StatelessCheckIntegrity(Writer writer, int offset, int length, Schema schema)
	{
		var buffer = writer.FirstBuffer;

		ulong compressedNumber = 0;
		int fieldCount = 0;
		int fieldIndex = 0;
		ReadState state = ReadState.CompressedNumberStart;
		ReadState txState = ReadState.DefinitionIdDone;
		var part = 0;
		int fieldDataSize = 0;
		Definition definition = null;
		FieldDefinition field = null;
		int fieldDataSoFar = 0;

		while (buffer != null && length > 0)
		{
			var readBuffer = buffer.Bytes;
			var read = offset + length;
			if (read > buffer.Length)
			{
				read = buffer.Length;
			}

			var i = offset;

			while (i < read)
			{
				// The current byte is used based on what the current read state is.
				switch (state)
				{
					case ReadState.CompressedNumberStart:
						compressedNumber = readBuffer[i++];

						if (compressedNumber < 251)
						{
							// its value is just the first byte
							state = txState;
						}
						else if (compressedNumber == 251)
						{
							// 3 bytes needed
							if ((i + 3) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 251)
								{
									throw new Exception("Integrity failure - invalid 2 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber2Bytes;
								part = 0;
							}
						}
						else if (compressedNumber == 252)
						{
							// 4 bytes needed
							if ((i + 4) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8) | ((ulong)readBuffer[i++] << 16);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 252)
								{
									throw new Exception("Integrity failure - invalid 3 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber3Bytes;
								part = 0;
							}
						}
						else if (compressedNumber == 253)
						{
							// 5 bytes needed
							if ((i + 5) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8) | ((ulong)readBuffer[i++] << 16) | ((ulong)readBuffer[i++] << 24);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 253)
								{
									throw new Exception("Integrity failure - invalid 4 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber4Bytes;
								part = 0;
							}
						}
						else if (compressedNumber == 254)
						{
							// 9 bytes needed
							if ((i + 9) <= read)
							{
								compressedNumber = (ulong)readBuffer[i++] | ((ulong)readBuffer[i++] << 8) | ((ulong)readBuffer[i++] << 16) | ((ulong)readBuffer[i++] << 24) |
					((ulong)readBuffer[i++] << 32) | ((ulong)readBuffer[i++] << 40) | ((ulong)readBuffer[i++] << 48) | ((ulong)readBuffer[i++] << 56);

								// Redundancy check (also exists for inversion):
								if (readBuffer[i++] != 254)
								{
									throw new Exception("Integrity failure - invalid 8 byte compressed number.");
								}

								state = txState;
							}
							else
							{
								compressedNumber = 0;
								state = ReadState.CompressedNumber8Bytes;
								part = 0;
							}
						}
						break;
					case ReadState.CompressedNumber2Bytes:
						// 2 byte number (partial)

						if (part == 16)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 251)
							{
								throw new Exception("Integrity failure - invalid 2 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 24)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.CompressedNumber3Bytes:
						// 3 byte number (partial)

						if (part == 24)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 252)
							{
								throw new Exception("Integrity failure - invalid 3 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 32)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.CompressedNumber4Bytes:
						// 4 byte number (partial)

						if (part == 32)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 253)
							{
								throw new Exception("Integrity failure - invalid 4 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 40)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.CompressedNumber8Bytes:
						// 8 byte number (partial)
						if (part == 64)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i++] != 254)
							{
								throw new Exception("Integrity failure - invalid 8 byte compressed number.");
							}

							part += 8;
						}
						else if (part == 72)
						{
							state = txState;
						}
						else
						{
							compressedNumber |= ((ulong)readBuffer[i++] << part);
							part += 8;
						}

						break;
					case ReadState.DefinitionIdDone:
						// done reading definition ID
						var definitionId = compressedNumber;

						definition = schema.Get((int)definitionId);

						if (definition == null)
						{
							if (definitionId <= 3)
							{
								definition = schema.GetTemporaryCriticalDefinition((int)definitionId);
							}
							else
							{
								throw new Exception("Definition does not exist with ID " + definitionId);
							}
						}

						txState = ReadState.FieldCountDone;
						state = ReadState.CompressedNumberStart;
						break;
					case ReadState.FieldCountDone:
						// done reading field count
						fieldCount = (int)compressedNumber;
						fieldIndex = 0;

						if (fieldCount == 0)
						{
							// A 0 field count is not duplicated. The definition ID is though, so we read that next:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryDefinitionIdDone;
						}
						else
						{

							if (fieldCount > 4096)
							{
								// Too many fields in one transaction
								throw new Exception("Max of 4096 fields can be set in one transcation");
							}

							// Read field ID:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.FieldIdDone;
						}
						break;
					case ReadState.FieldIdDone:
						// Get field definition (can be null):
						field = schema.GetField((int)compressedNumber);

						if (field == null)
						{
							if (compressedNumber <= 3)
							{
								// This happens whilst the schema is loading.
								// Use a temporary field ref.
								field = schema.GetTemporaryCriticalField((int)compressedNumber);
							}
							else
							{
								throw new Exception("Unknown field ID: " + compressedNumber);
							}
						}

						// Get the field data size:
						fieldDataSize = field.FieldDataSize;

						if (fieldDataSize == -1)
						{
							// It's variable - start reading the number:
							state = ReadState.CompressedNumberStart;
							txState = ReadState.FieldValueLengthDone;
						}
						else
						{
							// Reading a fixed number of bytes:
							if (i + fieldDataSize <= read)
							{
								// Already available in the buffer.
								i += fieldDataSize;

								fieldIndex++;
								state = ReadState.CompressedNumberStart;
								txState = ReadState.SecondaryFieldIdDone;
							}
							else
							{
								// Go to partial field read state:
								fieldDataSoFar = read - i;
								i = read;
								state = ReadState.FieldBytes;
							}
						}
						break;
					case ReadState.FieldValueLengthDone:
						if (field.SizeIsValue)
						{
							// The size we have is the base of the value that we'll be using

							// We've therefore completed reading this field value and should proceed to the next one.
							fieldIndex++;
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryFieldIdDone;
						}
						else
						{
							// Read size bytes next, unless it is nullable.
							if (field.IsNullable)
							{
								if (compressedNumber == 0)
								{
									// It's null
									// We've therefore completed reading this field value and should proceed to the next one.
									fieldIndex++;
									state = ReadState.CompressedNumberStart;
									txState = ReadState.SecondaryFieldIdDone;
								}
								else
								{
									// It's not null - there are some bytes (might be 0) to read
									fieldDataSize = ((int)compressedNumber) - 1;

									if (i + fieldDataSize <= read)
									{
										// All bytes of the field are available already.
										i += fieldDataSize;

										fieldIndex++;
										state = ReadState.CompressedNumberStart;
										txState = fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
									}
									else
									{
										// Go to partial field read state:
										fieldDataSoFar = read - i;
										i = read;
										state = ReadState.FieldBytes;
									}
								}
							}
							else
							{
								fieldDataSize = (int)compressedNumber;

								if (i + fieldDataSize <= read)
								{
									// All bytes of the field are available already.
									i += fieldDataSize;

									fieldIndex++;
									state = ReadState.CompressedNumberStart;
									txState = fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
								}
								else
								{
									// Go to partial field read state:
									fieldDataSoFar = read - i;
									i = read;
									state = ReadState.FieldBytes;
								}

							}
						}
						break;
					case ReadState.FieldBytes:
						// Read part of a field value

						var bytesToCopy = fieldDataSize - fieldDataSoFar;
						var bytesAvailable = read - i;
						if (bytesToCopy > bytesAvailable)
						{
							bytesToCopy = bytesAvailable;
						}

						fieldDataSoFar += bytesToCopy;
						i += bytesToCopy;

						if (fieldDataSoFar == fieldDataSize)
						{
							// Done reading this field value. Go to duplicated field length next.
							fieldIndex++;
							state = ReadState.CompressedNumberStart;
							txState = ReadState.SecondaryFieldValueLengthDone;
						}
						break;
					case ReadState.SecondaryFieldValueLengthDone:
						// Occurs on string and byte fields - the value length is present twice.

						var compareSize = (field.IsNullable) ? (int)(compressedNumber - 1) : (int)compressedNumber;

						if (compareSize != fieldDataSize)
						{
							throw new Exception("Integrity failure - field data sizes do not match (Expected " + fieldDataSize + ", got " + compareSize + ")");
						}

						// Go to secondary field ID next:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.SecondaryFieldIdDone;
						break;
					case ReadState.SecondaryFieldIdDone:
						// All fields have 2 field IDs.

						if (compressedNumber != field.Id)
						{
							throw new Exception("Integrity failure - secondary field ID does not match the first (Expected " + field.Id + ", got " + compressedNumber + ")");
						}

						state = ReadState.CompressedNumberStart;

						// Next state - either reading the secondary field count, or the next field ID:
						txState = (fieldCount == fieldIndex) ? ReadState.SecondaryFieldCountDone : ReadState.FieldIdDone;
						break;
					case ReadState.SecondaryFieldCountDone:
						// Secondary field count (if there is one - field count 0 does not get doubled up)

						if (fieldIndex != (int)compressedNumber)
						{
							throw new Exception("Integrity failure - the secondary field count does not match the first (Expected " + fieldIndex + ", got " + compressedNumber + ")");
						}

						// Read secondary def ID:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.SecondaryDefinitionIdDone;
						break;
					case ReadState.SecondaryDefinitionIdDone:
						// Secondary definition ID
						var defId = definition == null ? 0 : definition.Id;

						if (defId != compressedNumber)
						{
							throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + defId + ", got " + compressedNumber + ")");
						}

						// Reset:
						state = ReadState.CompressedNumberStart;
						txState = ReadState.DefinitionIdDone;

						break;
				}

			}

			buffer = buffer.After;
			length -= read - offset;
			offset = 0;
		}
	}

}