
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
	/// Before calling this you must seek str to the correct representitive location of blockchainOffset.
	/// If you store the whole chain file, it is simply a case of seeking to that blockchain offset.
	/// </summary>
	/// <param name="str"></param>
	/// <param name="blockchainOffset"></param>
	/// <param name="currentBlockId"></param>
	/// <param name="onFoundBlock"></param>
	/// <param name="bufferSize"></param>
	/// <returns>If a partial block is at the end of the stream then the writer with this partial block data in it is returned.</returns>
	/// <exception cref="Exception"></exception>
	public async ValueTask<BlockDiscoveryMeta> FindBlocks(Stream str, Func<Writer, ulong, ValueTask> onFoundBlock, ulong blockchainOffset = 0, ulong currentBlockId = 1, int bufferSize = 16384)
	{
		_blockchainOffset = blockchainOffset;
		TransactionId = _blockchainOffset;
		CurrentBlockId = currentBlockId;

		ResetState();

		if (_readBuffer == null || _readBuffer.Length != bufferSize)
		{
			_readBuffer = new byte[bufferSize];
		}

		var writer = Writer.GetPooled();
		writer.Start(null);

		// In case the file is being written to, we'll ask the chain for its current max txnId:
		var maxTransactionByte = (long)Chain.GetCurrentMaxByte();

		if (maxTransactionByte == str.Position)
		{
			// Nothing to do.

			// Latest txn ID:
			TransactionId = (ulong)maxTransactionByte;
			
			return new BlockDiscoveryMeta() { Writer = writer, Reader = this, MaxBytes = TransactionId };
		}

		// A transaction is:
		// [definitionId][fieldCount][[fieldType][fieldValue][fieldType]][fieldCount][definitionId]
		// Components of the transaction are duplicated for both integrity checking and also such that the transaction can be read backwards as well as forwards.
		// Virtually all of these components are the same thing - a compressed number (field values are often the same too), so reading compressed numbers quickly is important.
		
		// As this block discovery process can run quite slowly (as it is mainly used to upload blocks, meaning it waits for the upload to complete before proceeding)
		// It's very possible that more txns have been added by the time it reaches the end.
		// This outer while loop does a check to see if the max txn byte has changed and breaks if it hasn't.
		while (true)
		{
			while (str.Position < maxTransactionByte)
			{
				var read = str.Read(_readBuffer, 0, bufferSize);
				_byteIndex = 0;
				_digestedUpTo = 0;

				// This while loop is because FindNextBoundary stops processing the buffer when it finds a boundary.
				// Thus we may have more bytes in the current buffer when it returns true.
				while (_byteIndex < read)
				{
					var foundBoundary = FindNextBoundary(read, (byte[] buff, int start, int length) =>
					{
						// Got a chain fragment. Copy it into the memory stream:
						writer.Write(buff, start, length);
					});

					if (!foundBoundary)
					{
						// We're done reading this buffer.
						if (Halt)
						{
							// Latest txn ID:
							TransactionId = (ulong)maxTransactionByte;

							return new BlockDiscoveryMeta() { Writer = writer, Reader = this, MaxBytes = TransactionId };
						}

						break;
					}

					// writer contains a complete block!
					await onFoundBlock(writer, CurrentBlockId - 1);

					writer.Reset(null);
				}

			}

			// Check if we now have more bytes.
			var newMax = (long)Chain.GetCurrentMaxByte();

			if (newMax <= maxTransactionByte)
			{
				// No additional txns. stop there.
				break;
			}

			// The above inner while loop can go again:
			maxTransactionByte = newMax;
		}

		// Latest txn ID:
		TransactionId = (ulong)maxTransactionByte;

		// Return the writer, potentially containing a partial block:
		return new BlockDiscoveryMeta() { Writer = writer, Reader = this, MaxBytes = TransactionId };
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <param name="read"></param>
	/// <param name="onBlockFragment"></param>
	/// <returns>True if a boundary was discovered. Note that there may be more to read still.</returns>
	/// <exception cref="Exception"></exception>
	private bool FindNextBoundary(int read, Action<byte[], int, int> onBlockFragment)
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
							throw new Exception("Max of 4096 fields can be set in one transcation");
						}
						
						// Read field ID:
						_state = ReadState.CompressedNumberStart;
						_txState = ReadState.FieldIdDone;
					}
					break;
				case ReadState.FieldIdDone:
					// Get field definition (can be null):
					_field = _schema.GetField((int)_compressedNumber);
					
					
					
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
						if (_byteIndex + _fieldDataSize <= read)
						{
							// Already available in the buffer.
							_byteIndex += _fieldDataSize;

							FieldCount++;
							_state = ReadState.CompressedNumberStart;
							_txState = ReadState.SecondaryFieldIdDone;
						}
						else
						{
							// Go to partial field read state:
							_fieldDataSoFar = read - _byteIndex;
							_byteIndex = read;
							_state = ReadState.FieldBytes;
						}
					}
					
					break;
				case ReadState.FieldValueLengthDone:
					if (_field.SizeIsValue)
					{
						// The size we have is the base of the value that we'll be using

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
								// We've therefore completed reading this field value and should proceed to the next one.
								FieldCount++;
								_state = ReadState.CompressedNumberStart;
								_txState = ReadState.SecondaryFieldIdDone;
							}
							else
							{
								// It's not null - there are some bytes (might be 0) to read
								_fieldDataSize = ((int)_compressedNumber) - 1;

								if (_byteIndex + _fieldDataSize <= read)
								{
									// All bytes of the field are available already.
									_byteIndex += _fieldDataSize;

									FieldCount++;
									_state = ReadState.CompressedNumberStart;
									_txState = _fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
								}
								else
								{
									// Go to partial field read state:
									_fieldDataSoFar = read - _byteIndex;
									_byteIndex = read;
									_state = ReadState.FieldBytes;
								}
							}
						}
						else
						{
							_fieldDataSize = (int)_compressedNumber;

							if (_byteIndex + _fieldDataSize <= read)
							{
								// All bytes of the field are available already.
								_byteIndex += _fieldDataSize;

								FieldCount++;
								_state = ReadState.CompressedNumberStart;
								_txState = _fieldDataSize == 0 ? ReadState.SecondaryFieldIdDone : ReadState.SecondaryFieldValueLengthDone;
							}
							else
							{
								// Go to partial field read state:
								_fieldDataSoFar = read - _byteIndex;
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

					// If its a block boundary, we've got what we're after:
					if (_definitionId == Schema.BlockBoundaryDefId)
					{
						// Add to the digest:
						var digestLen = _byteIndex - _digestedUpTo;
						onBlockFragment(_readBuffer, _digestedUpTo, digestLen);
						_digestedUpTo = _byteIndex;

						// Don't process any more - we've found a boundary so stop there.
						_state = ReadState.CompressedNumberStart;
						_txState = ReadState.DefinitionIdDone;
						CurrentBlockId++;
						return true;
					}

					if (Halt)
					{
						return false;
					}

					// Reset:
					_state = ReadState.CompressedNumberStart;
					_txState = ReadState.DefinitionIdDone;

					break;
			}

		}

		if (_state == ReadState.SecondaryDefinitionIdDone)
		{
			// The last transaction hasn't been processed yet.

			// If its a block boundary, we've got what we're after:
			if (_definitionId == Schema.BlockBoundaryDefId)
			{
				// Add to the digest:
				var digestLen = _byteIndex - _digestedUpTo;
				onBlockFragment(_readBuffer, _digestedUpTo, digestLen);
				_digestedUpTo = _byteIndex;

				// Don't process any more - we've found a boundary so stop there.
				_state = ReadState.CompressedNumberStart;
				_txState = ReadState.DefinitionIdDone;
				CurrentBlockId++;
				return true;
			}
			
		}

		if (_digestedUpTo < _byteIndex)
		{
			// Add to the digest:
			var digestLen = _byteIndex - _digestedUpTo;
			onBlockFragment(_readBuffer, _digestedUpTo, digestLen);
			_digestedUpTo = _byteIndex;
		}

		_blockchainOffset += (ulong)read;
		return false;
	}

}