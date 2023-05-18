
using Api.SocketServerLibrary;
using Api.SocketServerLibrary.Crypto;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Transaction reader
/// </summary>
public partial class TransactionReader
{
	
	/// <summary>
	/// Called when a transaction is in the buffer
	/// </summary>
	private void TransactionInBackwardBuffer()
	{
		// Reverse direction does not need to update the schema (because it would be removing things from it, which is likely unnecessary in all scenarios).

		NodeId = 0;
		TransactionByteOffset = TransactionId;

		_onTransaction(this);

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
	/// Loads the transactions from the open read stream in the backwards direction.
	/// This is useful for state loading, as you can e.g. skip setting values which are overriden by future transactions.
	/// </summary>
	/// <param name="str"></param>
	/// <param name="blockchainOffset"></param>
	/// <param name="bufferSize"></param>
	public void StartReadBackwards(Stream str, ulong blockchainOffset = 0, int bufferSize = 16384)
	{
		bufferSize = 1024;
		
		var virtualStreamPosition = str.Length;

		if (virtualStreamPosition == 0)
		{
			return;
		}

		var readBuffer = new byte[bufferSize];

		// A transaction is:
		// [definitionId][fieldCount][[fieldType][fieldValue][fieldType]][fieldCount][definitionId]
		// Components of the transaction are duplicated for both integrity checking and also such that the transaction can be read backwards as well as forwards.
		// Virtually all of these components are the same thing - a compressed number (field values are often the same too), so reading compressed numbers quickly is important.
		ResetState();

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
				
				switch (_state)
				{
					case ReadState.CompressedNumberStart:
						_compressedNumber = readBuffer[i--];

						if (_compressedNumber < 251)
						{
							// its value is just the first byte
							_state = _txState;
						}
						else if (_compressedNumber == 251)
						{
							// 3 bytes needed
							if (i >= 2)
							{
								_compressedNumber = ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 251)
								{
									throw new Exception("Integrity failure - invalid 2 byte compressed number.");
								}

								_state = _txState;
							}
							else
							{
								_compressedNumber = 0;
								_state = ReadState.CompressedNumber2Bytes;
								_part = 8;
							}
						}
						else if (_compressedNumber == 252)
						{
							// 4 bytes needed
							if (i >= 3)
							{
								_compressedNumber = ((ulong)readBuffer[i--] << 16) | ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 252)
								{
									throw new Exception("Integrity failure - invalid 3 byte compressed number.");
								}

								_state = _txState;
							}
							else
							{
								_compressedNumber = 0;
								_state = ReadState.CompressedNumber3Bytes;
								_part = 16;
							}
						}
						else if (_compressedNumber == 253)
						{
							// 5 bytes needed
							if (i >= 4)
							{
								_compressedNumber = ((ulong)readBuffer[i--] << 24) | ((ulong)readBuffer[i--] << 16) | ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 253)
								{
									throw new Exception("Integrity failure - invalid 4 byte compressed number.");
								}

								_state = _txState;
							}
							else
							{
								_compressedNumber = 0;
								_state = ReadState.CompressedNumber4Bytes;
								_part = 24;
							}
						}
						else if (_compressedNumber == 254)
						{
							// 9 bytes needed
							if (i >= 8)
							{
								_compressedNumber = ((ulong)readBuffer[i--] << 56) | ((ulong)readBuffer[i--] << 48) | ((ulong)readBuffer[i--] << 40) | ((ulong)readBuffer[i--] << 32) | 
									((ulong)readBuffer[i--] << 24) | ((ulong)readBuffer[i--] << 16) | ((ulong)readBuffer[i--] << 8) | (ulong)readBuffer[i--];

								// Redundancy check (also exists for inversion):
								if (readBuffer[i--] != 254)
								{
									throw new Exception("Integrity failure - invalid 8 byte compressed number.");
								}

								_state = _txState;
							}
							else
							{
								_compressedNumber = 0;
								_state = ReadState.CompressedNumber8Bytes;
								_part = 56;
							}
						}
						break;
					case ReadState.CompressedNumber2Bytes:
						// 2 byte number (partial)

						if (_part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 251)
							{
								throw new Exception("Integrity failure - invalid 2 byte compressed number.");
							}

							_part = -9;
						}
						else if (_part == -9)
						{
							_state = _txState;
						}
						else
						{
							_compressedNumber |= ((ulong)readBuffer[i--] << _part);
							_part -= 8;
						}

						break;
					case ReadState.CompressedNumber3Bytes:
						// 3 byte number (partial)

						if (_part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 252)
							{
								throw new Exception("Integrity failure - invalid 3 byte compressed number.");
							}

							_part = -9;
						}
						else if (_part == -9)
						{
							_state = _txState;
						}
						else
						{
							_compressedNumber |= ((ulong)readBuffer[i--] << _part);
							_part -= 8;
						}

						break;
					case ReadState.CompressedNumber4Bytes:
						// 4 byte number (partial)

						if (_part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 253)
							{
								throw new Exception("Integrity failure - invalid 4 byte compressed number.");
							}

							_part = -9;
						}
						else if (_part == -9)
						{
							_state = _txState;
						}
						else
						{
							_compressedNumber |= ((ulong)readBuffer[i--] << _part);
							_part -= 8;
						}

						break;
					case ReadState.CompressedNumber8Bytes:
						// 8 byte number (partial)
						if (_part == -8)
						{
							// Redundancy check (also exists for inversion):
							if (readBuffer[i--] != 254)
							{
								throw new Exception("Integrity failure - invalid 8 byte compressed number.");
							}

							_part = -9;
						}
						else if (_part == -9)
						{
							_state = _txState;
						}
						else
						{
							_compressedNumber |= ((ulong)readBuffer[i--] << _part);
							_part -= 8;
						}

						break;
					case ReadState.DefinitionIdDone:
						// done reading definition ID
						_definitionId = _compressedNumber;

						Definition = _schema.Get((int)_definitionId);

						if (Definition == null)
						{
							if (_definitionId <= 3)
							{
								Definition = _schema.GetTemporaryCriticalDefinition((int)_definitionId);
							}
							else
							{
								throw new Exception("Definition does not exist with ID " + _definitionId);
							}
						}

						_txState = ReadState.FieldCountDone;
						_state = ReadState.CompressedNumberStart;
						break;
					case ReadState.FieldCountDone:
						// done reading field count
						_fieldCount = (int)_compressedNumber;
						FieldCount = 0;

						if (_fieldCount == 0)
						{
							// A 0 field count is not duplicated. The definition ID is though, so we read that next:
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.SecondaryDefinitionIdDone;
						}
						else
						{
							if (_fieldCount > 4096)
							{
								// Too many fields in one transaction
								throw new Exception("Max of 4096 fields can be set in one transaction");
							}

							if ((int)_fieldCount > Fields.Length)
							{
								Array.Resize(ref Fields, 4096);
							}

							// Read field ID:
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.FieldIdDone;
						}
						break;
					case ReadState.FieldIdDone:
						// Get field definition (can be null):
						_field = _schema.GetField((int)_compressedNumber);

						if (_field == null)
						{
							if (_compressedNumber <= 3)
							{
								// This happens whilst the schema is loading.
								// Use a temporary field ref.
								_field = _schema.GetTemporaryCriticalField((int)_compressedNumber);
							}
							else
							{
								throw new Exception("Unknown field ID: " + _compressedNumber);
							}
						}

						Fields[FieldCount].Field = _field;

						// Get the field data size:
						_fieldDataSize = _field.FieldDataSize;

						if (_fieldDataSize == -1)
						{
							// It's variable - start reading the number:
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.FieldValueLengthDone;
						}
						else
						{
							// Reading a fixed number of bytes:
							Fields[FieldCount].DataLength = _fieldDataSize;

							if (i+1 >= _fieldDataSize)
							{
								// Already available in the buffer. Transfer it straight over
								i -= _fieldDataSize;
								WriteToFieldBuffers(readBuffer, i+1, _fieldDataSize);

								FieldCount++;
								_state = ReadState.CompressedNumberStart;
								_txState = ReadState.SecondaryFieldIdDone;
							}
							else
							{
								// Go to partial field read state:
								_fieldDataSoFar = i+1;
								i = -1;
								WriteToFieldBuffers(readBuffer, 0, _fieldDataSoFar);
								_state = ReadState.FieldBytes;
							}
						}
						break;
					case ReadState.FieldValueLengthDone:
						if (_field.SizeIsValue)
						{
							// The size we have is the base of the value that we'll be using
							Fields[FieldCount].NumericValue = _compressedNumber;
							Fields[FieldCount].IsNull = false;

							// We've therefore completed reading this field value and should proceed to the next one.
							FieldCount++;
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.SecondaryFieldIdDone;
						}
						else
						{
							// Read size bytes next, unless it is nullable.
							if (_field.IsNullable)
							{
								if (_compressedNumber == 0)
								{
									// It's null
									Fields[FieldCount].IsNull = true;

									// We've therefore completed reading this field value and should proceed to the next one.
									FieldCount++;
									_state = ReadState.CompressedNumberStart;
									_txState = ReadState.SecondaryFieldIdDone;
								}
								else
								{
									// It's not null - there are some bytes (might be 0) to read
									Fields[FieldCount].IsNull = false;
									_fieldDataSize = ((int)_compressedNumber) - 1;
									Fields[FieldCount].DataLength = _fieldDataSize;

									if (i+1 >= _fieldDataSize)
									{
										// All bytes of the field are available already.
										i -= _fieldDataSize;
										WriteToFieldBuffers(readBuffer, i+1, _fieldDataSize);

										FieldCount++;
										_state = ReadState.CompressedNumberStart;
										_txState = _fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
									}
									else
									{
										// Go to partial field read state:
										_fieldDataSoFar = i + 1;
										i = -1;
										WriteToFieldBuffers(readBuffer, 0, _fieldDataSoFar);
										_state = ReadState.FieldBytes;
									}
								}
							}
							else
							{
								Fields[FieldCount].IsNull = false;
								_fieldDataSize = (int)_compressedNumber;
								Fields[FieldCount].DataLength = _fieldDataSize;

								if (i + 1 >= _fieldDataSize)
								{
									// All bytes of the field are available already.
									i -= _fieldDataSize;
									WriteToFieldBuffers(readBuffer, i + 1, _fieldDataSize);

									FieldCount++;
									_state = ReadState.CompressedNumberStart;
									_txState = _fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
								}
								else
								{
									// Go to partial field read state:
									_fieldDataSoFar = i + 1;
									i = -1;
									WriteToFieldBuffers(readBuffer, 0, _fieldDataSoFar);
									_state = ReadState.FieldBytes;
								}

							}
						}
						break;
					case ReadState.FieldBytes:
						// Read part of a field value

						#warning wrong order!
						// We're going backwards - this appends a block of bytes at the end of the current field value

						var bytesToCopy = _fieldDataSize - _fieldDataSoFar;
						var bytesAvailable = i + 1;
						if (bytesToCopy > bytesAvailable)
						{
							bytesToCopy = bytesAvailable;
						}

						WriteToFieldBuffers(readBuffer, i - bytesToCopy + 1, bytesToCopy, false);
						_fieldDataSoFar += bytesToCopy;
						i -= bytesToCopy;

						if (_fieldDataSoFar == _fieldDataSize)
						{
							// Done reading this field value. Go to duplicated field length next.
							FieldCount++;
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.SecondaryFieldValueLengthDone;
						}
					break;
					case ReadState.SecondaryFieldValueLengthDone:
						// Occurs on string and byte fields - the value length is present twice.

						var compareSize = (_field.IsNullable) ? (int)(_compressedNumber - 1) : (int)_compressedNumber;

						if (compareSize != _fieldDataSize)
						{
							throw new Exception("Integrity failure - field data sizes do not match (Expected " + _fieldDataSize + ", got " + compareSize + ")");
						}

						// Go to secondary field ID next:
						_state = ReadState.CompressedNumberStart;
						_txState = ReadState.SecondaryFieldIdDone;
						break;
					case ReadState.SecondaryFieldIdDone:
						// All fields have 2 field IDs.

						if (_compressedNumber != _field.Id)
						{
							throw new Exception("Integrity failure - secondary field ID does not match the first (Expected " + _field.Id + ", got " + _compressedNumber + ")");
						}

						_state = ReadState.CompressedNumberStart;

						// Next state - either reading the secondary field count, or the next field ID:
						_txState = (_fieldCount == FieldCount) ? ReadState.SecondaryFieldCountDone : ReadState.FieldIdDone;
						break;
					case ReadState.SecondaryFieldCountDone:
						// Secondary field count (if there is one - field count 0 does not get doubled up)

						if (FieldCount != (int)_compressedNumber)
						{
							throw new Exception("Integrity failure - the secondary field count does not match the first (Expected " + FieldCount + ", got " + _compressedNumber + ")");
						}

						// Read secondary def ID:
						_state = ReadState.CompressedNumberStart;
						_txState = ReadState.SecondaryDefinitionIdDone;
						break;
					case ReadState.SecondaryDefinitionIdDone:
						// Secondary definition ID
						_definitionId = Definition == null ? 0 : Definition.Id;

						if (_definitionId != _compressedNumber)
						{
							throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + _definitionId + ", got " + _compressedNumber + ")");
						}

						// Current ID:
						TransactionId = _blockchainOffset + (ulong)(virtualStreamPosition + i + 1);

						TransactionInBackwardBuffer();

						if (Halt)
						{
							return;
						}

						// Reset:
						_state = ReadState.CompressedNumberStart;
						_txState = ReadState.DefinitionIdDone;

						break;
				}
			}
		}

		if (_state != ReadState.SecondaryDefinitionIdDone)
		{
			throw new Exception("Integrity failure - partial bytes of a transaction at the beginning of the provided file");
		}

		// Because i was -1, the very last transaction has not been handled yet.
		_definitionId = Definition == null ? 0 : Definition.Id;

		if (_definitionId != _compressedNumber)
		{
			throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + _definitionId + ", got " + _compressedNumber + ")");
		}

		// Current ID:
		TransactionId = _blockchainOffset + (ulong)virtualStreamPosition;

		TransactionInBackwardBuffer();
	}
	
}
