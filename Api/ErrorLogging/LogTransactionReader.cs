using Api.SocketServerLibrary;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Api.ErrorLogging;

/// <summary>
/// Lumity transaction reader used to read binary log files.
/// </summary>
public partial class LogTransactionReader
{
	private Schema _schema;
	private Action<LogTransactionReader> _onTransaction;

	/// <summary>
	/// True if the transaction reader is loading from a file vs. it loading from a just occurred transaction.
	/// </summary>
	public bool IsLoading = true;

	/// <summary>
	/// Creates a txn reader.
	/// </summary>
	public LogTransactionReader()
	{
		
	}

	/// <summary>
	/// Sets up this transaction reader
	/// </summary>
	/// <param name="schema"></param>
	/// <param name="onTransaction"></param>
	/// <param name="blockchainOffset"></param>
	public void Init(Schema schema, Action<LogTransactionReader> onTransaction, ulong blockchainOffset = 0)
	{
		_schema = schema;
		_onTransaction = onTransaction;
		_blockchainOffset = blockchainOffset;
		TransactionId = _blockchainOffset;
	}

	/// <summary>
	/// Update the callback method.
	/// </summary>
	/// <param name="onTransaction"></param>
	public void SetCallback(Action<LogTransactionReader> onTransaction)
	{
		_onTransaction = onTransaction;
	}

	/// <summary>
	/// The schema of this reader.
	/// </summary>
	public Schema Schema => _schema;

	/// <summary>
	/// Set this to true to stop the reader.
	/// </summary>
	public bool Halt = false;

	/// <summary>
	/// The cache being used by the current transaction.
	/// </summary>
	public CacheSet CacheSet;

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
	/// An object just updated by a transaction. Could be a definition, project, entity etc.
	/// </summary>
	public object RelevantObject;

	/// <summary>
	/// The block ID of the current block. Starts at 1.
	/// </summary>
	public ulong CurrentBlockId = 1;

	/// <summary>
	/// The original, unaltered transaction ID. The transaction byte offset.
	/// </summary>
	public ulong TransactionByteOffset;
	
	/// <summary>
	/// NodeId of the transaction, or 0 if none.
	/// </summary>
	public ulong NodeId;

	/// <summary>
	/// True if this reader should update the schema.
	/// </summary>
	public bool UpdateSchema;

	/// <summary>
	/// Used to track invalid transactions within the current block.
	/// Most transactions will be valid as they're submitted by validators with knowledge of the state, so it will generally be that this set is very small.
	/// </summary>
	private ulong[] InvalidTransactions;

	/// <summary>
	/// Pointer to the number of used slots in InvalidTransactions set.
	/// </summary>
	private int InvalidTransactionCounter;

	/// <summary>
	/// Current location of the block header. When a boundary is encountered this is updated.
	/// </summary>
	public ulong BlockBoundaryTransactionId = 0;

	/// <summary>
	/// The latest transaction timestamp.
	/// </summary>
	public ulong Timestamp;

	/// <summary>
	/// The offset to the first user field in the transaction.
	/// </summary>
	public int StartFieldsOffset;

	/// <summary>
	/// The number of txns read from the current block so far.
	/// </summary>
	public int TransactionsInBlockSoFar;

	/// <summary>
	/// Applies the transaction currently in the reader buffer which has been initialised.
	/// </summary>
	/// <returns></returns>
	public virtual object ApplyTransaction()
	{
		return null;
	}

	/// <summary>
	/// May be called during InitialiseTransaction to setup the core start fields.
	/// </summary>
	public ValidationState InitialiseStartFields()
	{
		FieldData[] fields = Fields;
		StartFieldsOffset = 0;

		for (var i = 0; i < FieldCount; i++)
		{
			var fieldMeta = fields[i].Field;

			if (fieldMeta.Id == Api.ErrorLogging.Schema.TimestampFieldDefId)
			{
				// Timestamp. This will be used to set EditedUtc.
				Timestamp = Fields[i].NumericValue;

				// Timestamp marks the end of the special fields.
				StartFieldsOffset = i + 1;
				break;
			}
		}

		return ValidationState.Valid;
	}

	/// <summary>
	/// Initialises the current transaction and sets up any useful state fields in the reader.
	/// </summary>
	public virtual ValidationState InitialiseTransaction()
	{
		// Read a transaction (forward direction).
		FieldData[] fields = Fields;
		StartFieldsOffset = 0;

		for (var i = 0; i < FieldCount; i++)
		{
			var fieldMeta = fields[i].Field;

			if (fieldMeta.Id == Api.ErrorLogging.Schema.TimestampFieldDefId)
			{
				// Timestamp. This will be used to set EditedUtc.
				Timestamp = Fields[i].NumericValue;

				// Timestamp marks the end of the special fields.
				StartFieldsOffset = i + 1;
				break;
			}
		}

		return ValidationState.Valid;
	}
	

	/// <summary>
	/// True if the given transaction (which must be in the current block) is valid.
	/// </summary>
	/// <param name="txId"></param>
	/// <returns></returns>
	public bool IsTransactionInBlockValid(ulong txId)
	{
		if (txId < BlockBoundaryTransactionId)
		{
			// The requested txn is not in this block.
			return false;
		}

		if (InvalidTransactions == null || InvalidTransactionCounter == 0)
		{
			// It's valid - there aren't any invalids in the block.
			return true;
		}

		// Could improve this with a binary search - it'll always be in ascending order.
		for (var i = 0; i < InvalidTransactionCounter; i++)
		{
			if (InvalidTransactions[i] == txId)
			{
				// It's invalid
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Adds an invalid txn ID to the set.
	/// </summary>
	/// <param name="id"></param>
	private void AddInvalidTransactionId(ulong id)
	{
		if (InvalidTransactions == null)
		{
			InvalidTransactions = new ulong[10];
		}
		else if (InvalidTransactionCounter == InvalidTransactions.Length)
		{
			// Add 10 slots:
			Array.Resize(ref InvalidTransactions, InvalidTransactionCounter + 10);
		}

		// Add the ID to the set:
		InvalidTransactions[InvalidTransactionCounter++] = id;
	}

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

		NodeId = 0;
		var txnId = TransactionId;
		TransactionByteOffset = txnId;
		TransactionsInBlockSoFar++;

		// Initialise the transaction state, performing the validation and setting up any useful fields on the reader:
		var txnState = InitialiseTransaction();
		var isValid = txnState == ValidationState.Valid;

		if (!isValid)
		{
			// Use the potentially modified TransactionId rather than the original one (txnId) here.
			AddInvalidTransactionId(txnId);
		}
		else
		{
			// Apply the transaction now:
			var relevantObject = ApplyTransaction();
			RelevantObject = relevantObject;

			if (_onTransaction != null)
			{
				// Run the callback:
				_onTransaction(this);
			}
		}
		
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
	/// The stateful nature of this reader is because it can ingest packets from a network over time as well.
	/// </summary>
	private ulong _compressedNumber;
	private ulong _definitionId;
	private int _fieldCount;
	private ReadState _state;
	private ReadState _txState;
	private int _part = 0;
	private int _fieldDataSize = 0;
	private FieldDefinition _field;
	private int _fieldDataSoFar;
	private int _byteIndex;
	private ulong _blockchainOffset;
	private byte[] _readBuffer;
	private byte[] _blockHashBuffer = new byte[32];

	/// <summary>
	/// Resets the state of this reader.
	/// </summary>
	public void ResetState()
	{
		_compressedNumber = 0;
		_definitionId = 0;
		_fieldCount = 0;
		_byteIndex = 0;
		_state = ReadState.CompressedNumberStart;
		_txState = ReadState.DefinitionIdDone;
		_part = 0;
		_fieldDataSize = 0;
		_field = null;
		_fieldDataSoFar = 0;
	}

	private void ProcessBuffer(int read)
	{

		while (_byteIndex < read)
		{
			// The current byte is used based on what the current read state is.
			switch (_state)
			{
				case ReadState.CompressedNumberStart:
					_compressedNumber = _readBuffer[_byteIndex++];

					if (_compressedNumber < 251)
					{
						// its value is just the first byte
						_state = _txState;
					}
					else if (_compressedNumber == 251)
					{
						// 3 bytes needed
						if ((_byteIndex + 3) <= read)
						{
							_compressedNumber = (ulong)_readBuffer[_byteIndex++] | ((ulong)_readBuffer[_byteIndex++] << 8);

							// Redundancy check (also exists for inversion):
							if (_readBuffer[_byteIndex++] != 251)
							{
								throw new Exception("Integrity failure - invalid 2 byte compressed number.");
							}

							_state = _txState;
						}
						else
						{
							_compressedNumber = 0;
							_state = ReadState.CompressedNumber2Bytes;
							_part = 0;
						}
					}
					else if (_compressedNumber == 252)
					{
						// 4 bytes needed
						if ((_byteIndex + 4) <= read)
						{
							_compressedNumber = (ulong)_readBuffer[_byteIndex++] | ((ulong)_readBuffer[_byteIndex++] << 8) | ((ulong)_readBuffer[_byteIndex++] << 16);

							// Redundancy check (also exists for inversion):
							if (_readBuffer[_byteIndex++] != 252)
							{
								throw new Exception("Integrity failure - invalid 3 byte compressed number.");
							}

							_state = _txState;
						}
						else
						{
							_compressedNumber = 0;
							_state = ReadState.CompressedNumber3Bytes;
							_part = 0;
						}
					}
					else if (_compressedNumber == 253)
					{
						// 5 bytes needed
						if ((_byteIndex + 5) <= read)
						{
							_compressedNumber = (ulong)_readBuffer[_byteIndex++] | ((ulong)_readBuffer[_byteIndex++] << 8) | ((ulong)_readBuffer[_byteIndex++] << 16) | ((ulong)_readBuffer[_byteIndex++] << 24);

							// Redundancy check (also exists for inversion):
							if (_readBuffer[_byteIndex++] != 253)
							{
								throw new Exception("Integrity failure - invalid 4 byte compressed number.");
							}

							_state = _txState;
						}
						else
						{
							_compressedNumber = 0;
							_state = ReadState.CompressedNumber4Bytes;
							_part = 0;
						}
					}
					else if (_compressedNumber == 254)
					{
						// 9 bytes needed
						if ((_byteIndex + 9) <= read)
						{
							_compressedNumber = (ulong)_readBuffer[_byteIndex++] | ((ulong)_readBuffer[_byteIndex++] << 8) | ((ulong)_readBuffer[_byteIndex++] << 16) | ((ulong)_readBuffer[_byteIndex++] << 24) |
				((ulong)_readBuffer[_byteIndex++] << 32) | ((ulong)_readBuffer[_byteIndex++] << 40) | ((ulong)_readBuffer[_byteIndex++] << 48) | ((ulong)_readBuffer[_byteIndex++] << 56);

							// Redundancy check (also exists for inversion):
							if (_readBuffer[_byteIndex++] != 254)
							{
								throw new Exception("Integrity failure - invalid 8 byte compressed number.");
							}

							_state = _txState;
						}
						else
						{
							_compressedNumber = 0;
							_state = ReadState.CompressedNumber8Bytes;
							_part = 0;
						}
					}
					break;
				case ReadState.CompressedNumber2Bytes:
					// 2 byte number (partial)

					if (_part == 16)
					{
						// Redundancy check (also exists for inversion):
						if (_readBuffer[_byteIndex++] != 251)
						{
							throw new Exception("Integrity failure - invalid 2 byte compressed number.");
						}

						_part += 8;
					}
					else if (_part == 24)
					{
						_state = _txState;
					}
					else
					{
						_compressedNumber |= ((ulong)_readBuffer[_byteIndex++] << _part);
						_part += 8;
					}

					break;
				case ReadState.CompressedNumber3Bytes:
					// 3 byte number (partial)

					if (_part == 24)
					{
						// Redundancy check (also exists for inversion):
						if (_readBuffer[_byteIndex++] != 252)
						{
							throw new Exception("Integrity failure - invalid 3 byte compressed number.");
						}

						_part += 8;
					}
					else if (_part == 32)
					{
						_state = _txState;
					}
					else
					{
						_compressedNumber |= ((ulong)_readBuffer[_byteIndex++] << _part);
						_part += 8;
					}

					break;
				case ReadState.CompressedNumber4Bytes:
					// 4 byte number (partial)

					if (_part == 32)
					{
						// Redundancy check (also exists for inversion):
						if (_readBuffer[_byteIndex++] != 253)
						{
							throw new Exception("Integrity failure - invalid 4 byte compressed number.");
						}

						_part += 8;
					}
					else if (_part == 40)
					{
						_state = _txState;
					}
					else
					{
						_compressedNumber |= ((ulong)_readBuffer[_byteIndex++] << _part);
						_part += 8;
					}

					break;
				case ReadState.CompressedNumber8Bytes:
					// 8 byte number (partial)
					if (_part == 64)
					{
						// Redundancy check (also exists for inversion):
						if (_readBuffer[_byteIndex++] != 254)
						{
							throw new Exception("Integrity failure - invalid 8 byte compressed number.");
						}

						_part += 8;
					}
					else if (_part == 72)
					{
						_state = _txState;
					}
					else
					{
						_compressedNumber |= ((ulong)_readBuffer[_byteIndex++] << _part);
						_part += 8;
					}

					break;
				case ReadState.DefinitionIdDone:
					// done reading definition ID
					_definitionId = _compressedNumber;

					Definition = _schema.Get((int)_definitionId);

					if (Definition == null)
					{
						throw new Exception("Definition does not exist with ID " + _definitionId);
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
							throw new Exception("Max of 4096 fields can be set in one transcation");
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
						throw new Exception("Unknown field ID: " + _compressedNumber);
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

						if (_byteIndex + _fieldDataSize <= read)
						{
							// Already available in the buffer. Transfer it straight over
							WriteToFieldBuffers(_readBuffer, _byteIndex, _fieldDataSize);
							_byteIndex += _fieldDataSize;

							FieldCount++;
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.SecondaryFieldIdDone;
						}
						else
						{
							// Go to partial field read state:
							_fieldDataSoFar = read - _byteIndex;
							WriteToFieldBuffers(_readBuffer, _byteIndex, _fieldDataSoFar);
							_byteIndex = read;
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

								if (_byteIndex + _fieldDataSize <= read)
								{
									// All bytes of the field are available already.
									WriteToFieldBuffers(_readBuffer, _byteIndex, _fieldDataSize);
									_byteIndex += _fieldDataSize;

									FieldCount++;
									_state = ReadState.CompressedNumberStart;
									_txState = _fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
								}
								else
								{
									// Go to partial field read state:
									_fieldDataSoFar = read - _byteIndex;
									WriteToFieldBuffers(_readBuffer, _byteIndex, _fieldDataSoFar);
									_byteIndex = read;
									_state = ReadState.FieldBytes;
								}
							}
						}
						else
						{
							Fields[FieldCount].IsNull = false;
							_fieldDataSize = (int)_compressedNumber;
							Fields[FieldCount].DataLength = _fieldDataSize;

							if (_byteIndex + _fieldDataSize <= read)
							{
								// All bytes of the field are available already.
								WriteToFieldBuffers(_readBuffer, _byteIndex, _fieldDataSize);
								_byteIndex += _fieldDataSize;

								FieldCount++;
								_state = ReadState.CompressedNumberStart;
								_txState = _fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
							}
							else
							{
								// Go to partial field read state:
								_fieldDataSoFar = read - _byteIndex;
								WriteToFieldBuffers(_readBuffer, _byteIndex, _fieldDataSoFar);
								_byteIndex = read;
								_state = ReadState.FieldBytes;
							}

						}
					}
					break;
				case ReadState.FieldBytes:
					// Read part of a field value

					var bytesToCopy = _fieldDataSize - _fieldDataSoFar;
					var bytesAvailable = read - _byteIndex;
					if (bytesToCopy > bytesAvailable)
					{
						bytesToCopy = bytesAvailable;
					}

					WriteToFieldBuffers(_readBuffer, _byteIndex, bytesToCopy, false);
					_fieldDataSoFar += bytesToCopy;
					_byteIndex += bytesToCopy;

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

					TransactionInForwardBuffer();

					if (Halt)
					{
						return;
					}

					// Reset:
					_state = ReadState.CompressedNumberStart;
					_txState = ReadState.DefinitionIdDone;

					// Next tx ID:
					TransactionId = _blockchainOffset + (ulong)_byteIndex;

					break;
			}

		}

		_blockchainOffset += (ulong)read;
	}

	/// <summary>
	/// Current location of the read head.
	/// </summary>
	/// <returns></returns>
	public ulong ReadHeadLocation()
	{
		return _blockchainOffset + (ulong)_byteIndex;
	}

	/// <summary>
	/// Sets up the state for the first block.
	/// </summary>
	public void SetupFirstBlock(ulong blockchainOffset = 0, ulong currentBlockId = 1, int bufferSize = 16384)
	{
		_blockchainOffset = blockchainOffset;
		TransactionId = _blockchainOffset;
		CurrentBlockId = currentBlockId;

		ResetState();

		if (_readBuffer == null || _readBuffer.Length != bufferSize)
		{
			_readBuffer = new byte[bufferSize];
		}
	}

	/// <summary>
	/// Sets up the info for the previous block.
	/// </summary>
	/// <param name="blockHash"></param>
	/// <param name="blockchainOffset"></param>
	/// <param name="currentBlockId"></param>
	/// <param name="bufferSize">The size of the read buffer. Usually you don't need to change this.</param>
	public void SetupPreviousBlock(Span<byte> blockHash, ulong blockchainOffset = 0, ulong currentBlockId = 1, int bufferSize = 16384)
	{
		_blockchainOffset = blockchainOffset;
		TransactionId = _blockchainOffset;
		CurrentBlockId = currentBlockId;

		ResetState();

		if (_readBuffer == null || _readBuffer.Length != bufferSize)
		{
			_readBuffer = new byte[bufferSize];
		}
	}

	/// <summary>Sets up this reader for forward reading.</summary>
	/// <param name="bufferSize">The size of the read buffer. Usually you don't need to change this.</param>
	/// <param name="blockchainOffset">
	/// The total size, in bytes, of other parts of the blockchain before the one that is being read from here.
	/// If the blockchain is in one file, this value is 0. If you have partial files, you can obtain it via the "Byte Offset" field in the nearest block boundary transaction.
	/// </param>
	public void StartReadForwards(ulong blockchainOffset = 0, int bufferSize = 16384)
	{
		if (_readBuffer == null || _readBuffer.Length != bufferSize)
		{
			_readBuffer = new byte[bufferSize];
		}

		_blockchainOffset = blockchainOffset;
		TransactionId = _blockchainOffset;

		ResetState();
	}

	/// <summary>
	/// Loads the transactions from the open read stream in the forwards direction. This must be called on a transaction boundary.
	/// </summary>
	/// <param name="str"></param>
	/// <exception cref="Exception"></exception>
	public void StartReadForwards(Stream str)
	{
		if (_readBuffer == null)
		{
			throw new Exception("Can't read until you have setup the previous block state. See SetupPreviousBlock.");
		}

		if (str.CanSeek)
		{
			if (str.Position != 0)
			{
				str.Seek(0, SeekOrigin.Begin);
			}

			if (str.Length == 0)
			{
				// Nothing to do.
				return;
			}
		}

		var bufferSize = _readBuffer.Length;

		// A transaction is:
		// [definitionId][fieldCount][[fieldType][fieldValue][fieldType]][fieldCount][definitionId]
		// Components of the transaction are duplicated for both integrity checking and also such that the transaction can be read backwards as well as forwards.
		// Virtually all of these components are the same thing - a compressed number (field values are often the same too), so reading compressed numbers quickly is important.

		while (true)
		{
			var read = str.Read(_readBuffer, 0, bufferSize);

			if (read == 0)
			{
				break;
			}

			_byteIndex = 0;
			ProcessBuffer(read);
		}

		if (_state != ReadState.SecondaryDefinitionIdDone)
		{
			throw new Exception("Integrity failure - partial bytes of a transaction at the end of the provided file");
		}

		// Because i was -1, the very last transaction has not been handled yet.
		_definitionId = Definition == null ? 0 : Definition.Id;

		if (_definitionId != _compressedNumber)
		{
			throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + _definitionId + ", got " + _compressedNumber + ")");
		}

		TransactionInForwardBuffer();

		// Next tx ID:
		TransactionId = _blockchainOffset;
		
		ResetState();
	}

	/// <summary>
	/// Adds the given buffer to this processor.
	/// </summary>
	/// <param name="buffer"></param>
	public void ProcessBuffer(BufferedBytes buffer)
	{
		_readBuffer = buffer.Bytes;
		_byteIndex = buffer.Offset;
		ProcessBuffer(buffer.Length);

		// If we just finished reading a transaction, run it:
		if (_state == ReadState.SecondaryDefinitionIdDone)
		{
			_definitionId = Definition == null ? 0 : Definition.Id;

			if (_definitionId != _compressedNumber)
			{
				throw new Exception("Integrity failure - Secondary definition ID does not match the first (Expected " + _definitionId + ", got " + _compressedNumber + ")");
			}

			TransactionInForwardBuffer();

			// Next tx ID:
			TransactionId = _blockchainOffset;
			
			ResetState();
		}
	}

	/// <summary>
	/// Called when a transaction is in the buffer
	/// </summary>
	private void TransactionInBackwardBuffer()
	{
		NodeId = 0;
		TransactionByteOffset = TransactionId;
		RelevantObject = null;
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
							throw new Exception("Definition does not exist with ID " + _definitionId);
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
							throw new Exception("Unknown field ID: " + _compressedNumber);
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