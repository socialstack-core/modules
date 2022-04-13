using Api.SocketServerLibrary;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Lumity.BlockChains;


/// <summary>
/// A Lumity project has 4 chains with different purposes and accessibility.
/// </summary>
public enum ChainType
{
	/// <summary>
	/// The public chain is the main one of the project. Ideally as much information is in this as possible.
	/// </summary>
	Public = 1,
	/// <summary>
	/// Personal information, configuration such as email services, as well as project relevant secrets.
	/// The schema of a private chain is always an extension of the public chain schema.
	/// </summary>
	Private = 2,
	/// <summary>
	/// The host public chain is available publicly and contains any running information that the host wants to share about the project. This is generally server health/ metrics and uptime information.
	/// Host chains must not contain any project mandatory data - i.e. a host chain can be completely lost and the project will still operate.
	/// </summary>
	PublicHost = 3,
	/// <summary>
	/// Internal host information used to keep the servers in sync. This can include things like session resumption keys or server private data.
	/// Host chains must not contain any project mandatory data - i.e. a host chain can be completely lost and the project will still operate.
	/// </summary>
	PrivateHost = 4
}

/// <summary>
/// Represents a block chain.
/// </summary>
public partial class BlockChain
{
	/// <summary>
	/// Size of the header on a transaction message.
	/// </summary>
	public const int TransactionMessageHeaderSize = 12;
	
	/// <summary>
	/// The field/ content structure of this blockchain.
	/// </summary>
	public Schema Schema = new Schema();

	/// <summary>
	/// File to read/ write from.
	/// </summary>
	public string File;

	/// <summary>
	/// File containing shortform schema. (.lbs)
	/// </summary>
	public string SchemaFile;

	/// <summary>
	/// The type of chain this is (public, private etc).
	/// </summary>
	public ChainType ChainType;
	
	/// <summary>
	/// True if this chains type is either Private or PrivateHost.
	/// </summary>
	public bool IsPrivate;

	/// <summary>
	/// A chain that this one is relative to. This chain uses the schema of the remote chain in combination with its own.
	/// </summary>
	public BlockChain RelativeTo;

	/// <summary>
	/// Creates a blockchain info handler for the given file path.
	/// </summary>
	/// <param name="file"></param>
	/// <param name="type"></param>
	/// <param name="relativeTo">A chain that this one is relative to. This is used when a chain gets its schema exclusively from some other chain.
	/// Private chains generally derive their schema from their public counterpart, i.e. if you are using a private chain type, this field is likely required.</param>
	public BlockChain(string file, ChainType type, BlockChain relativeTo = null)
	{
		File = file;
		ChainType = type;
		IsPrivate = ((int)ChainType & 2) == 2;
		RelativeTo = relativeTo;
	}

	/// <summary>
	/// Loads or sets up the schema.
	/// </summary>
	public void LoadOrCreate()
	{
		if (RelativeTo != null)
		{
			// This chain derives its schema from the given (usually public type) chain.
			Schema = RelativeTo.Schema;

			if (!FileExists())
			{
				Directory.CreateDirectory(Path.GetDirectoryName(File));
				System.IO.File.Create(File);
			}
		}
		else if (FileExists())
		{
			// Load the schema:
			#warning todo - use shortform schema file if one exists
			LoadForwards((TransactionReader reader) => { }, true);
		}
		else
		{
			// Write the schema:
			WriteSchema();
		}
	}

	/// <summary>
	/// Checks if the file exists
	/// </summary>
	/// <returns></returns>
	public bool FileExists()
	{
		return System.IO.File.Exists(File);
	}

	/// <summary>
	/// Creates the chain file if it doesn't exist yet. Usually do this after you have created an initial schema.
	/// </summary>
	public void CreateIfNotExists()
	{
		if (!FileExists())
		{
			WriteSchema();
		}
	}

	/// <summary>
	/// Writes the schema to the chain file
	/// </summary>
	public void WriteSchema()
	{
		if (Schema.Definitions.Count == 0)
		{
			Schema.CreateDefaults();
		}

		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current time stamp:
		var timestamp = (ulong)(System.DateTime.UtcNow.Ticks);

		// Write schema to writer:
		Schema.Write(writer, timestamp);

		// Write to file:
		Directory.CreateDirectory(Path.GetDirectoryName(File));
		System.IO.File.WriteAllBytes(File, writer.AllocatedResult());

		writer.Release();
	}

	/// <summary>
	/// Closes an open write stream
	/// </summary>
	public void Close()
	{
		if (_writeStream != null)
		{
			_writeStream.Close();
			_writeStream = null;
			_writeBuffer = null;
		}
	}

	/// <summary>
	/// Loads from the file now in the forwards direction.
	/// </summary>
	public void LoadForwards(Action<TransactionReader> onTransaction, bool updateSchema = false)
	{
		var fs = new FileStream(File, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite, 0, true);

		var txReader = new TransactionReader(fs, Schema, onTransaction){ UpdateSchema = updateSchema };

		txReader.Read();

		fs.Close();
	}

	/// <summary>
	/// Loads from the file now in the backwards direction.
	/// </summary>
	public void LoadBackwards(Action<TransactionReader> onTransaction)
	{
		var fs = new FileStream(File, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite, 0, true);

		var txReader = new TransactionReader(fs, Schema, onTransaction);

		txReader.ReadBackwards();

		fs.Close();
	}

	/// <summary>
	/// Defines the given type in this blockchain. Also updates the schema.
	/// </summary>
	/// <param name="name"></param>
	public Definition AddCreateType(string name)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Creating a new type:
		writer.WriteInvertibleCompressed(Schema.EntityTypeId);

		// 2 fields:
		writer.WriteInvertibleCompressed(2);

		// Timestamp:
		var now = (ulong)DateTime.UtcNow.Ticks;
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(now);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		writer.WriteInvertibleCompressed(Schema.NameDefId);
		writer.WriteInvertibleUTF8(name);
		writer.WriteInvertibleCompressed(Schema.NameDefId);

		// 2 fields (again, for readers going backwards):
		writer.WriteInvertibleCompressed(2);

		// definition ID (again, for readers going backwards):
		writer.WriteInvertibleCompressed(Schema.EntityTypeId);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		AddBuffers(first, last);

		// AddBuffers doesn't update the schema so define there as well:
		var addedToSchema = Schema.Define(name);

		return addedToSchema;
	}

	/// <summary>
	/// Gets a definition or defines it if it didn't exist.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="wasDefined"></param>
	/// <returns></returns>
	public Definition FindOrDefine(string name, out bool wasDefined)
	{
		if (RelativeTo != null)
		{
			return RelativeTo.FindOrDefine(name, out wasDefined);
		}

		var def = Schema.FindDefinition(name);

		if (def != null)
		{
			wasDefined = false;
			return def;
		}

		// Define it:
		def = Schema.Define(name);

		// Write the transaction:
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current time stamp:
		var timestamp = (ulong)(System.DateTime.UtcNow.Ticks);

		def.WriteCreate(writer, timestamp);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		AddBuffers(first, last);

		wasDefined = true;
		return def;
	}
	
	/// <summary>
	/// Gets a field definition or defines it if it didn't exist.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="type"></param>
	/// <param name="wasDefined"></param>
	/// <returns></returns>
	public FieldDefinition FindOrDefineField(string name, string type, out bool wasDefined)
	{
		if (RelativeTo != null)
		{
			return RelativeTo.FindOrDefineField(name, type, out wasDefined);
		}
		
		var field = Schema.FindField(name, type);

		if (field != null)
		{
			wasDefined = false;
			return field;
		}

		// Define it:
		field = Schema.DefineField(name, type);

		// Write the transaction:
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current time stamp:
		var timestamp = (ulong)(System.DateTime.UtcNow.Ticks);

		field.WriteCreate(writer, timestamp);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		AddBuffers(first, last);

		wasDefined = true;
		return field;
	}

	/// <summary>
	/// Creates an instance of the given thing with no fields other than a timestamp. 
	/// Exists for testing or creating an initial chain - these transactions normally originate from validators.
	/// </summary>
	/// <param name="definition"></param>
	/// <param name="count">Number to add</param>
	public void AddInstances(Definition definition, int count = 1)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		var now = (ulong)DateTime.UtcNow.Ticks;

		// Create a buffer which will be written out repeatedly:

		// Creating an instance of the definition:
		writer.WriteInvertibleCompressed(definition.Id);

		// 1 field:
		writer.WriteInvertibleCompressed(1);

		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(now);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// 1 field (for readers going backwards):
		writer.WriteInvertibleCompressed(1);

		// Definition again (for readers going backwards):
		writer.WriteInvertibleCompressed(definition.Id);

		var transaction = writer.AllocatedResult();
		writer.Reset(null);

		for (var i = 0; i < count; i++)
		{
			writer.Write(transaction, 0, transaction.Length);
		}

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		AddBuffers(first, last);
	}

	/// <summary>
	/// Checks and adds a binary set of transactions to the current block. Throws if the integrity check fails.
	/// </summary>
	/// <param name="writer">Contains transactions.</param>
	/// <param name="offset">MUST be within the first buffer.</param>
	/// <param name="length">Amount of bytes representing the transactions to add.</param>
	public void CheckIntegrity(Writer writer, int offset, int length)
	{
		var buffer = writer.FirstBuffer;

		while (buffer != null && length > 0)
		{
			var readBuffer = buffer.Bytes;
			var bytesToRead = buffer.Length - offset;
			if (bytesToRead > length)
			{
				bytesToRead = length;
			}

			// Process readBuffer at offset and bytesToRead

			buffer = buffer.After;
			offset = 0;
			length -= bytesToRead;
		}
	}

	private object ChainWriteQueueLock = new object();
	private BufferedBytes WriteQueueFirst;
	private BufferedBytes WriteQueueLast;
	private bool WritingFromQueue = false;

	private FileStream _writeStream;
	private byte[] _writeBuffer;

	/// <summary>
	/// Adds the given buffers to the outbound write queue. These buffers must contain a valid network message, 
	/// offset to eliminate the network message header (that's an offset of 9 bytes). All others must have an offset of 0.
	/// </summary>
	/// <param name="first"></param>
	/// <param name="last"></param>
	public void AddBuffers(BufferedBytes first, BufferedBytes last)
	{
		// Append the buffers to the write queue.

		// Valid data block - add to the chain now via a bulk transfer in a locked context.
		last.After = null;
		BufferedBytes writeFirst = null;

		lock (ChainWriteQueueLock)
		{
			if (WriteQueueFirst == null)
			{
				WriteQueueFirst = first;
				WriteQueueLast = last;
			}
			else
			{
				WriteQueueLast.After = first;
				WriteQueueLast = last;
			}

			if (!WritingFromQueue)
			{
				WritingFromQueue = true;
				writeFirst = WriteQueueFirst;
				WriteQueueFirst = null;
				WriteQueueLast = null;
			}
		}

		while (writeFirst != null)
		{
			WriteOutQueue(writeFirst);
			writeFirst = null;

			lock (ChainWriteQueueLock) {
				if (WriteQueueFirst != null)
				{
					// New queue to write out
					writeFirst = WriteQueueFirst;
					WriteQueueFirst = null;
					WriteQueueLast = null;
				}
				else
				{
					// Clear the queue writing flag:
					WritingFromQueue = false;
				}
			}
		}
	}

	/// <summary>
	/// The file buffer size. 16kb is a recommended write buffer size on dotnet 6.
	/// </summary>
	private const int FileBufferSize = 16384;

	private void WriteOutQueue(BufferedBytes writeFirst)
	{
		// We have exclusivity to write to the outbound filestream.
		var writeBuffer = _writeBuffer;
		var writeStream = _writeStream;

		if (writeStream == null)
		{
			// Ensure directories exist:
			Directory.CreateDirectory(Path.GetDirectoryName(File));

			// Open the stream (no buffer, and in sync mode - a critical goal here is to make sure the data is actually stored on disk)
			writeStream = new FileStream(File, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 0, false);
			_writeStream = writeStream;
		}

		if (writeBuffer == null)
		{
			writeBuffer = new byte[FileBufferSize];
			_writeBuffer = writeBuffer;
		}

		var bufferToWrite = writeFirst;
		BufferedBytes lastBuffer = null;
		var writeFill = 0;
		var totalBufferSize = 0;
		var bufferCount = 0;

		while (bufferToWrite != null)
		{
			lastBuffer = bufferToWrite;
			bufferCount++;

			// Does it fit in the current write buffer?
			var bytesToCopy = bufferToWrite.Length;

			if ((writeFill + bytesToCopy) > FileBufferSize)
			{
				// Write out the write buffer:
				writeStream.Write(writeBuffer, 0, writeFill);
				writeFill = 0;
			}

			// Copy to the write buffer:
			Array.Copy(bufferToWrite.Bytes, bufferToWrite.Offset, writeBuffer, writeFill, bytesToCopy);
			writeFill += bytesToCopy;

			if (bufferToWrite.Offset == TransactionMessageHeaderSize)
			{
				// This buffer starts with a network message header.
				// Clear the offset such that we can use this buffer in an outbound message
				// with no other copying necessary:
				bufferToWrite.Offset = 0;
				bufferToWrite.Length += TransactionMessageHeaderSize;
				totalBufferSize += bufferToWrite.Length;
			}
			else
			{
				totalBufferSize += bytesToCopy;
			}

			bufferToWrite = bufferToWrite.After;
		}

		// Last write:
		if (writeFill != 0)
		{
			writeStream.Write(writeBuffer, 0, writeFill);
		}

		// Flush and force an actual OS write:
		writeStream.Flush(true);

		OnWroteTransactions(bufferCount, totalBufferSize, writeFirst, lastBuffer);
	}

	/// <summary>
	/// Called when transactions were written to the blockchain file.
	/// </summary>
	/// <param name="blockCount"></param>
	/// <param name="totalLength"></param>
	/// <param name="first"></param>
	/// <param name="last"></param>
	protected virtual void OnWroteTransactions(int blockCount, int totalLength, BufferedBytes first, BufferedBytes last)
	{
		// Release the buffers:
		var buff = first;
		while (buff != null)
		{
			var next = buff.After;
			buff.Release();
			buff = next;
		}
	}
}
