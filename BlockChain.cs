using Api.SocketServerLibrary;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using Api.SocketServerLibrary.Crypto;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;

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
	/// The project this chain belongs to.
	/// </summary>
	public BlockChainProject Project;

	/// <summary>
	/// Last written timestamp in this session on this chain.
	/// </summary>
	private ulong latestTimestamp;

	/// <summary>
	/// Timestamp lock
	/// </summary>
	private object _tsLock = new object();
	
	/// <summary>
	/// Pending transaction queue lock.
	/// </summary>
	private object _pendingQueue = new object();

	/// <summary>
	/// The current waiting transaction. Transactions are generally removed here (never added to the front).
	/// </summary>
	public PendingTransaction FirstPendingTransaction;
	/// <summary>
	/// The last waiting transaction. Transactions are added here.
	/// </summary>
	public PendingTransaction LastPendingTransaction;

	/// <summary>
	/// The TransactionReader type to use. If not specified, a default TransactionReader is used.
	/// </summary>
	public Type _readerType;
	
	/// <summary>
	/// When listening to a CDN for updates, this checks for blocks every 5s by default.
	/// </summary>
	public int MaintenanceTicksPerCdnBlockCheck = 5;

	/// <summary>
	/// Ticks up until it reaches MaintenanceTicksPerCdnBlockCheck.
	/// </summary>
	private int CdnBlockCheckTicks = 0;

	/// <summary>
	/// Converts the given timestamp to a DateTime (UTC).
	/// </summary>
	/// <param name="stamp"></param>
	/// <returns></returns>
	public DateTime TimestampToDateTime(ulong stamp)
	{
		// Div by 100:
		stamp = stamp / 100;

		// Offset by start ticks:
		var ticks = Project.TimestampTickOffset + (long)stamp;

		return new DateTime((long)ticks, DateTimeKind.Utc);
	}

	/// <summary>
	/// Gets the current max transaction byte (essentially the same chain length).
	/// This value is always transaction aligned unlike the filestream length.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public ulong GetCurrentMaxByte()
	{
		if (_txReader == null)
		{
			// Must have loaded it.
			throw new Exception("Chain not loaded yet. Load the chain before calling this.");
		}

		return _txReader.TransactionId;
	}

	/// <summary>
	/// Called once a second to perform any housekeeping tasks.
	/// </summary>
	/// <param name="timestamp">Timestamp in the chains precision (ns).</param>
	public void Update(ulong timestamp)
	{
		// Clear any pendings that have been waiting too long:
		var pending = FirstPendingTransaction;
		ulong noEarlierThan;

		if (pending != null)
		{
			var waitTimeInNs = Project.MaxTransactionWaitTimeMs * 1000000;
			noEarlierThan = timestamp - waitTimeInNs;

			// The ones at the front of the q have been waiting longest
			// So if its time is ok, we can just break out of the loop and not check others.
			PendingTransaction previous = null;

			while (pending != null)
			{
				if (pending.Timestamp > noEarlierThan)
				{
					// It's not been waiting that long, thus also nothing after it has. Stop there.
					break;
				}

				// Remove this pending txn.
				PendingTransaction next;

				lock (_pendingQueue)
				{
					// Get next again, it may have had another txn added to it:
					next = pending.Next;

					if (next == null)
					{
						// It was the last one.
						LastPendingTransaction = previous;
					}

					if (previous == null)
					{
						// It was the first one.
						FirstPendingTransaction = next;
					}
					else
					{
						previous.Next = next;
					}
				}
				
				pending.TransactionId = 0;
				pending.Valid = false;
				pending.RelevantObject = null;
				pending.Done();

				previous = pending;
				pending = next;
			}
		}

		// If we're watching a CDN, check it for updates at some reduced rate.
		if (_isWatching == WatchMode.Cdn)
		{
			CdnBlockCheckTicks++;

			if (CdnBlockCheckTicks >= MaintenanceTicksPerCdnBlockCheck)
			{
				// Reset it:
				CdnBlockCheckTicks = 0;

				// Run the check now:
				_ = Task.Run(CheckForCdnUpdates);
			}

		}

		// If the last block boundary was >30s ago and we have >0 txns written since it, output a boundary.
		if (Project.IsAssembler && HasTransactionsInBlock && LastBlockBoundaryTimestamp != 0)
		{
			noEarlierThan = timestamp - MaxTicksPerBlock;

			if (LastBlockBoundaryTimestamp < noEarlierThan)
			{
				// To make sure that the offset is correct, we need to lock the queue whilst the boundary is added.
				// The best way to achieve that is for addBuffers to add the boundary internally, minimising the lock time required.
				// The queue could be null though so to ensure that some queue processing occurs, we pass through an empty buffer.
				var emptyBuffer = new BufferedBytes(Array.Empty<byte>(), 0, null);
				AddBuffers(Timestamp, emptyBuffer, emptyBuffer);
			}
		}
	}

	/// <summary>
	/// Called when the parent project is updated.
	/// </summary>
	public void ProjectUpdated()
	{
		// If the StartYear was just changed, the TimestampTickOffset "baked" into latestTimestamp is likely to cause a time offset problem.
		// So, must reset the latestTimestamp:

		// Local stamp first:
		var stamp = DateTime.UtcNow.Ticks;

		// Adjust to being relative to the project year:
		stamp -= Project.TimestampTickOffset;

		// Finally convert ticks into the default project precision (which will be assumed to be nanoseconds always here):
		var result = ((ulong)stamp) * 100;

		lock (_tsLock)
		{
			latestTimestamp = result;
		}
	}

	/// <summary>
	/// Gets the filename for the given chain type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static string GetChainTypeFileName(ChainType type)
	{
		return GetChainTypeName(type) + ".lbc";
	}

	/// <summary>
	/// Gets the chain type name for the given type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static string GetChainTypeName(ChainType type)
	{
		switch (type)
		{
			case ChainType.Public:
				return "public";
			case ChainType.Private:
				return "private";
			case ChainType.PublicHost:
				return "public-host";
			case ChainType.PrivateHost:
				return "private-host";
		}

		return null;
	}

	/// <summary>
	/// The textual chain type name.
	/// </summary>
	public string ChainTypeName => GetChainTypeName(ChainType);

	/// <summary>
	/// Creates a unique timestamp using the project information and UtcNow.
	/// </summary>
	public ulong Timestamp {
		get {

			// Local stamp first:
			var stamp = DateTime.UtcNow.Ticks;
			
			// Adjust to being relative to the project year:
			stamp -= Project.TimestampTickOffset;

			// Finally convert ticks into the default project precision (which will be assumed to be nanoseconds always here):
			var result = ((ulong)stamp) * 100;

			lock (_tsLock)
			{
				if (result <= latestTimestamp)
				{
					// Stamp is not unique yet.
					// Increase the latest timestamp and set it into result.
					result = ++latestTimestamp;
				}
			}

			return result;
		}
	}

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
	/// True if there is at least 1 transaction in the current block.
	/// </summary>
	public bool HasTransactionsInBlock;

	/// <summary>
	/// The current length of the blockchain. This is essentially the transaction ID of the next txn on the chain.
	/// </summary>
	private long WriteFileOffset;

	/// <summary>
	/// The timestamp when the last block header occurred, or the first timestamp in the chain.
	/// If a transaction being added results in this being >30s ago, a header is added effectively completing a block.
	/// </summary>
	public ulong LastBlockBoundaryTimestamp;

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
	/// The CDN path for the blocks of this chain. e.g. "aaaaaaaa/public-host/block"
	/// </summary>
	private string _blockCdnPath;
	
	/// <summary>
	/// The CDN path for the blocks of this chain. e.g. "aaaaaaaa/public-host/block"
	/// </summary>
	private string _fileCdnPath;

	/// <summary>
	/// The CDN path for the blocks of this chain. e.g. "aaaaaaaa/public-host/block"
	/// </summary>
	public string BlockCdnPath
	{
		get {
			if (_blockCdnPath == null)
			{
				_blockCdnPath = Project.PublicHash.ToLower() + "/" + ChainTypeName + "/block";
			}

			return _blockCdnPath;
		}
	}
	
	/// <summary>
	/// The CDN path for the side files of this chain. e.g. "aaaaaaaa/public-host/file"
	/// </summary>
	public string FileCdnPath
	{
		get {
			if (_fileCdnPath == null)
			{
				_fileCdnPath = Project.PublicHash.ToLower() + "/" + ChainTypeName + "/file";
			}

			return _fileCdnPath;
		}
	}

	/// <summary>
	/// Creates a blockchain info handler for the given chain type in the given project.
	/// </summary>
	/// <param name="project"></param>
	/// <param name="type"></param>
	/// <param name="readerType">A type which inherits TransactionReader. Is used when reading and validating the txns on this chain.</param>
	public BlockChain(BlockChainProject project, ChainType type, Type readerType)
	{
		Project = project;
		File = project != null && project.LocalStorageDirectory != null ? project.LocalStorageDirectory + GetChainTypeFileName(type) : null;
		ChainType = type;
		IsPrivate = ChainType == ChainType.Private || ChainType == ChainType.PrivateHost;

		if (project != null)
		{
			if (type == ChainType.Private)
			{
				RelativeTo = project.GetChain(ChainType.Public);
			}
			else if (type == ChainType.PrivateHost)
			{
				RelativeTo = project.GetChain(ChainType.PublicHost);
			}
		}

		_readerType = readerType == null ? typeof(TransactionReader) : readerType;

		if (project != null && project.OnChainEvent != null)
		{
			project.OnChainEvent(this, BlockChainEvent.Instanced);
		}
	}

	private TransactionReader CreateReader()
	{
		var txObj = Activator.CreateInstance(_readerType);
		var txReader = (TransactionReader)txObj;

		if (Project != null && Project.OnReaderEvent != null)
		{
			Project.OnReaderEvent(txReader, TransactionReaderEvent.Instanced);
		}

		return txReader;
	}

	/// <summary>
	/// Checks if the CDN has any more blocks on it and if so, downloads them.
	/// This is used by the CDN only watch mode and when specifically not the assembler.
	/// </summary>
	/// <returns></returns>
	public async Task CheckForCdnUpdates()
	{
		if (Project.Distributor == null || Project.IsAssembler)
		{
			return;
		}

		// Get the index:
		var index = await Project.Distributor.GetIndex(this);

		if (index.LatestEndByteOffset == 0)
		{
			return;
		}

		await PullCdnData(index);
	}

	/// <summary>
	/// Loads or sets up the schema.
	/// </summary>
	public async ValueTask LoadOrCreate(Action<TransactionReader> onTransaction = null)
	{
		// If we have distribution config, first make sure the file is up to date.
		var remoteDataExists = false;
		DistributorJsonIndex index = null;

		if (Project.Distributor != null)
		{
			// Get the index for this chain:
			index = await Project.Distributor.GetIndex(this);

			// It is never null.
			if (index.LatestEndByteOffset != 0)
			{
				// May need to download and append blocks.
				remoteDataExists = true;
			}
		}

		// Next, whenever the callback is executed, we can update the digest with additional bytes.

		if (RelativeTo != null)
		{
			// This chain derives its schema from the given (usually public type) chain.
			Schema = RelativeTo.Schema;

			if (!FileExists())
			{
				Directory.CreateDirectory(Path.GetDirectoryName(File));
				WriteFileOffset = 0;
			}
			else
			{
				// Set up the length:
				WriteFileOffset = new System.IO.FileInfo(File).Length;
			}

			// Load the transactions:
			LoadForwards(onTransaction, false);

			// Setup initial write digest:
			_writeDigest = _txReader.CopyDigest();
		}
		else if (FileExists())
		{
			// Set up the length:
			WriteFileOffset = new System.IO.FileInfo(File).Length;

			// Load the transactions:
			LoadForwards(onTransaction, true);

			// Setup initial write digest:
			_writeDigest = _txReader.CopyDigest();
		}
		else
		{
			// Initial length:
			WriteFileOffset = 0;

			// Write the schema:
			if (remoteDataExists)
			{
				// Load from an empty file. This makes sure the _txReader is setup with a filestream.

				// Load the transactions (really just sets up the reader):
				LoadForwards(onTransaction, true);

				// Setup initial write digest:
				_writeDigest = _txReader.CopyDigest();
			}
			else
			{
				// We are the first.
				WriteInitialChain(onTransaction);
			}
		}

		// At this point we now know what blocks we have locally.
		// If there is any remote data, obtain it now.
		if (remoteDataExists)
		{
			if (_txReader == null)
			{
				// That wasn't supposed to happen!
				throw new Exception("Unexpected missing reader");
			}

			await PullCdnData(index);
		}
	}

	private async Task PullCdnData(DistributorJsonIndex index)
	{
		// Latest block we have received (or partially received) is..
		var latestBlock = _txReader.CurrentBlockId;
		var latestBlockOffset = _txReader.BlockBoundaryTransactionId;

		// Max byte so far:
		var currentMaxByte = _txReader.TransactionId;

		// Set the write file offset to the correct value:
		WriteFileOffset = (long)index.LatestEndByteOffset;

		if (index.LatestEndByteOffset > currentMaxByte)
		{
			// Remote has more data than we do.
			// Download block range now.
			var fs = Open(FileAccess.ReadWrite);
			var readBuffer = new byte[2048];
			var bufferedBytes = new BufferedBytes(readBuffer, 2048, null);
			var size = fs.Length;
			fs.Seek(size, SeekOrigin.Begin);

			// The number of partial bytes we currently have on the end of the file:
			int partialBlockBytesRemaining = (int)(size - (long)latestBlockOffset);

			await Project.Distributor.GetBlockRange(this, latestBlock, index.LatestBlockId, async (Stream block, ulong firstBlockId, ulong lastBlockId) => {

				// Append to the file and stream load the segments.
				var bytesRead = await block.ReadAsync(readBuffer, 0, 2048);

				while (bytesRead > 0)
				{
					var offset = partialBlockBytesRemaining;

					if (offset > bytesRead)
					{
						// Skip this whole segment.
						partialBlockBytesRemaining -= bytesRead;
					}
					else
					{
						bufferedBytes.Length = bytesRead - offset;
						bufferedBytes.Offset = offset;

						// Load:
						_txReader.ProcessBuffer(bufferedBytes);

						// Append to the file:
						fs.Write(readBuffer, offset, bufferedBytes.Length);

						if (offset > 0)
						{
							// Clear partial:
							partialBlockBytesRemaining = 0;
						}
					}

					bytesRead = await block.ReadAsync(readBuffer, 0, 2048);
				}

				await fs.FlushAsync();

			});
		}

	}

	/// <summary>
	/// Transactions added to this chain will trigger the given reader event. Call this after you have loaded at least once (or you know the file was empty).
	/// </summary>
	public void Watch(bool cdnMode = true)
	{
		_isWatching = cdnMode ? WatchMode.Cdn : WatchMode.Realtime;

		// The txReader is reused from a LoadForwards call
		// as that provides an important but small piece of state, the blockchainOffset.
		if (_txReader == null)
		{
			throw new Exception("Must load the chain forwards before you can watch it.");
		}

		// Set the initial # of txns sitting in the block:
		HasTransactionsInBlock = _txReader.TransactionsInBlockSoFar != 0;
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
	/// Writes to this chain. May send the packets remotely if there is a remote BAS currently operating it.
	/// </summary>
	/// <param name="timestamp"></param>
	/// <param name="first"></param>
	/// <param name="last"></param>
	/// <returns></returns>
	public async ValueTask<TransactionResult> Write(ulong timestamp, BufferedBytes first, BufferedBytes last)
	{
		// If this node is the block assembly service (BAS), add the buffers to the chain.
		// Otherwise, send them as UDP packets to the BAS.

		// Create a pending transaction:
		var pending = CreatePendingTransaction(timestamp);

		if (Project.IsAssembler)
		{
			// Add buffers straight to the chain file.
			// This may happen instantly, or it might be delayed.
			// Either way when it completes, the pending transaction object will be marked Done.
			AddBuffers(timestamp, first, last);
		}
		else
		{
			// Submit the buffers to the remote BAS now.
			#warning todo - send buffers to remote BAS.
		}

		// Wait for the pending transaction.
		// If the buffers were added immediately, which is very common on single instance sites, this completes sync (as a ValueTask).
		await pending;

		var relevant = pending.RelevantObject;
		var txnId = pending.TransactionId;
		var valid = pending.Valid;

		pending.Release();

		return new TransactionResult() {
			TransactionId = txnId,
			Valid = valid,
			RelevantObject = relevant
		};
	}

	/// <summary>
	/// Writes the initial schema to the chain file
	/// </summary>
	public void WriteInitialChain(Action<TransactionReader> onTransaction = null)
	{
		var _schemaToWrite = onTransaction == null ? Schema : new Schema();

		_schemaToWrite.CreateDefaults();

		// Create Sha3 hash of the public key and chain type:
		_writeDigest = new Sha3Digest();

		Span<byte> blockHash = stackalloc byte[32];
		GetInitialHash(blockHash);

		// Console.WriteLine("Write started with hash: " + Hex.Convert(blockHash.ToArray()));

		// Initialise the digest with the hash:

		for (var i = 0; i < 32; i++)
		{
			_writeDigest.Update(blockHash[i]);
		}

		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current time stamp:
		var timestamp = Timestamp;

		// Write schema to writer:
		_schemaToWrite.Write(writer, timestamp);

		// If it's the public project chain, create a project public key now:
		if (ChainType == ChainType.Public && Project != null)
		{
			// Write the public key to the chain:

			// Chain meta:
			writer.WriteInvertibleCompressed(4);

			writer.WriteInvertibleCompressed(2);

			// Timestamp:
			writer.WriteInvertibleCompressed(Schema.TimestampDefId);
			writer.WriteInvertibleCompressed(timestamp);
			writer.WriteInvertibleCompressed(Schema.TimestampDefId);

			// PublicKey:
			writer.WriteInvertibleCompressed(Schema.PublicKeyDefId);
			writer.WriteInvertible(Project.PublicKey);
			writer.WriteInvertibleCompressed(Schema.PublicKeyDefId);

			// Field count again (for readers travelling backwards):
			writer.WriteInvertibleCompressed(2);

			// Chain meta again (for readers travelling backwards):
			writer.WriteInvertibleCompressed(4);
		}

		// Make sure directory exists:
		Directory.CreateDirectory(Path.GetDirectoryName(File));

		var result = writer.AllocatedResult();

		// Release the writer:
		writer.Release();

		// Write the bytes:
		System.IO.File.WriteAllBytes(File, result);

		WriteFileOffset = result.Length;

		_writeDigest.BlockUpdate(result, 0, result.Length);

		// Ensure we call the callback (if there is one) for this block:
		if (onTransaction != null)
		{
			var txReader = CreateReader();
			txReader.Init(Schema, this, onTransaction);
			txReader.UpdateSchema = true;

			Span<byte> initialHash = stackalloc byte[32];
			GetInitialHash(initialHash);

			// Set the initial hash:
			txReader.SetupPreviousBlock(initialHash);

			txReader.ProcessBuffer(new BufferedBytes() {
				Bytes = result,
				Length = result.Length,
				Offset = 0
			});

			if (_txReader == null)
			{
				_txReader = txReader;
			}
		}
	}

	/// <summary>
	/// Generates the initial project hash and puts it into the given 32 byte span.
	/// </summary>
	/// <param name="outputHash"></param>
	public void GetInitialHash(Span<byte> outputHash)
	{
		var firstHash = new Sha3Digest();

		// Initialise it with a hash of the project public key. Use readDigest to calc the hash itself:
		firstHash.BlockUpdate(Project.PublicKey, 0, Project.PublicKey.Length);

		// Textual chain type as well:
		var asciiBytes = System.Text.Encoding.ASCII.GetBytes(ChainTypeName);
		firstHash.BlockUpdate(asciiBytes, 0, asciiBytes.Length);

		firstHash.DoFinal(outputHash, 0);
	}

	/// <summary>
	/// Set the current assembler.
	/// </summary>
	public async ValueTask<TransactionResult> SetAssembler(uint id)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current time stamp:
		var timestamp = Timestamp;

		// Chain meta:
		writer.WriteInvertibleCompressed(4);

		writer.WriteInvertibleCompressed(3);

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Schema.NodeId);
		writer.WriteInvertibleCompressed(Project.SelfNodeId);
		writer.WriteInvertibleCompressed(Schema.NodeId);
		
		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// AssemblerId:
		writer.WriteInvertibleCompressed(Schema.AssemblerDefId);
		writer.WriteInvertibleCompressed(id);
		writer.WriteInvertibleCompressed(Schema.AssemblerDefId);

		// Field count again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(3);

		// Chain meta again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(4);
		
		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		var result = await Write(timestamp, first, last);
		
		return result;
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

	private TransactionReader _txReader;
	private WatchMode _isWatching = WatchMode.None;

	/// <summary>
	/// Opens a filestream for this chain.
	/// </summary>
	/// <returns></returns>
	public FileStream Open(FileAccess access = FileAccess.Read)
	{
		return new FileStream(File, FileMode.OpenOrCreate, access, FileShare.ReadWrite, 0, true);
	}

	/// <summary>
	/// Finds blocks in the chain, invoking the given async callback when they are discovered.
	/// </summary>
	public async ValueTask<BlockDiscoveryMeta> FindBlocks(Func<Writer, ulong, ValueTask> onFoundBlock, ulong blockchainOffset = 0, ulong currentBlockId = 1)
	{
		var fs = Open();

		// Assuming str is the complete chain:
		fs.Seek((long)blockchainOffset, SeekOrigin.Begin);

		var txReader = CreateReader();
		txReader.Init(Schema, this, null);
		txReader.UpdateSchema = false;

		var meta = await txReader.FindBlocks(fs, onFoundBlock, blockchainOffset, currentBlockId);
		fs.Close();
		return meta;
	}

	/// <summary>
	/// Loads from the file now in the forwards direction.
	/// </summary>
	public void LoadForwards(Action<TransactionReader> onTransaction = null, bool updateSchema = false, bool checkHashes = true, bool reuseReaderForWatch = true, ulong byteOffset = 0)
	{
		var fs = Open();

		var txReader = CreateReader();
		txReader.Init(Schema, this, onTransaction);
		txReader.UpdateSchema = updateSchema;

		if (checkHashes)
		{
			// Starts from 0:
			Span<byte> initialHash = stackalloc byte[32];
			GetInitialHash(initialHash);

			// Set the initial hash:
			txReader.SetupPreviousBlock(initialHash);
		}
		else
		{
			// Setup the read state:
			txReader.StartReadForwards(byteOffset);
		}

		if (reuseReaderForWatch && _txReader == null)
		{
			_txReader = txReader;
		}

		txReader.StartReadForwards(fs);

		fs.Close();
	}
	
	/// <summary>
	/// Loads from the file now in the backwards direction.
	/// </summary>
	public void LoadBackwards(Action<TransactionReader> onTransaction)
	{
		var fs = Open();

		var txReader = CreateReader();
		txReader.Init(Schema, this, onTransaction);

		txReader.StartReadBackwards(fs);

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
		var timestamp = Timestamp;
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
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

		AddBuffers(timestamp, first, last);

		// AddBuffers doesn't update the schema so define there as well:
		var addedToSchema = Schema.Define(name);

		return addedToSchema;
	}

	/// <summary>
	/// Find a definition by name. Null if it doesn't exist.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public Definition FindDefinition(string name)
	{
		if (RelativeTo != null)
		{
			return RelativeTo.FindDefinition(name);
		}

		return Schema.FindDefinition(name);
	}

	/// <summary>
	/// Gets a definition or defines it if it didn't exist.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public async ValueTask<Definition> Define(string name)
	{
		// Write the transaction:
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current unique time stamp:
		var timestamp = Timestamp;

		// Creating a definition:
		writer.WriteInvertibleCompressed(3);

		// 3 fields:
		writer.WriteInvertibleCompressed(3);

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Schema.NodeId);
		writer.WriteInvertibleCompressed(Project.SelfNodeId);
		writer.WriteInvertibleCompressed(Schema.NodeId);

		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// Name:
		writer.WriteInvertibleCompressed(Schema.NameDefId);
		writer.WriteInvertibleUTF8(name);
		writer.WriteInvertibleCompressed(Schema.NameDefId);

		// 3 fields (again, for readers going backwards):
		writer.WriteInvertibleCompressed(3);
		writer.WriteInvertibleCompressed(3);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		var result = await Write(timestamp, first, last);

		if (!result.Valid)
		{
			return null;
		}

		// The relevant object is the new definition in the schema:
		return result.RelevantObject as Definition;
	}

	/// <summary>
	/// Finds a pending txn by the given timestamp.
	/// </summary>
	/// <param name="timestamp"></param>
	/// <param name="transactionId"></param>
	/// <param name="relevantObject"></param>
	/// <param name="nodeId"></param>
	/// <param name="valid"></param>
	/// <returns></returns>
	public void UpdatePending(ulong timestamp, ulong transactionId, ulong nodeId, object relevantObject, bool valid)
	{
		// Note that if this is the node which is setting up the chain, they can both be zero. This is fine.
		// During the initial loading of the chain, the self node ID can be zero when it technically could be known - this is also fine.
		if (nodeId != Project.SelfNodeId)
		{
			// Console.WriteLine("Ignoring an update from some other node " + nodeId + ", " + Project.SelfNodeId);
			return;
		}

		var current = FirstPendingTransaction;
		PendingTransaction previous = null;

		while (current != null)
		{
			var next = current.Next;

			if (current.Timestamp == timestamp)
			{
				// Found it! remove from this q:
				lock (_pendingQueue)
				{
					// Get next again, it may have had another txn added to it:
					next = current.Next;

					if (next == null)
					{
						// It was the last one.
						LastPendingTransaction = previous;
					}

					if (previous == null)
					{
						// It was the first one.
						FirstPendingTransaction = next;
					}
					else
					{
						previous.Next = next;
					}
				}

				current.Next = null;
				current.TransactionId = transactionId;
				current.Valid = valid;
				current.RelevantObject = relevantObject;
				current.Done();

				return;
			}

			previous = current;
			current = next;
		}
	}

	/// <summary>
	/// Creates a pending transaction for the given timestamp. Essentially this will wait until the given timestamp (from this node) is seen.
	/// Note that it is async in that it won't leave a thread paused whilst it 'waits'.
	/// </summary>
	/// <param name="timestamp"></param>
	private PendingTransaction CreatePendingTransaction(ulong timestamp)
	{
		// Create the pending object:
		var pending = PendingTransaction.GetPooled();
		pending.Timestamp = timestamp;
		pending.Reset();

		lock (_pendingQueue) {
			if (LastPendingTransaction == null)
			{
				FirstPendingTransaction = pending;
				LastPendingTransaction = pending;
			}
			else
			{
				LastPendingTransaction.Next = pending;
				LastPendingTransaction = pending;
			}
		}

		return pending;
	}

	/// <summary>
	/// Gets a field definition or null it if it didn't exist.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="type"></param>
	public FieldDefinition FindField(string name, string type)
	{
		if (RelativeTo != null)
		{
			return RelativeTo.FindField(name, type);
		}

		return Schema.FindField(name, type);
	}

	/// <summary>
	/// Defines a field.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	public async ValueTask<FieldDefinition> DefineField(string name, string type)
	{
		if (RelativeTo != null)
		{
			return await RelativeTo.DefineField(name, type);
		}
		
		// Write the transaction:
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Current time stamp:
		var timestamp = Timestamp;

		// Entity create tx:
		writer.WriteInvertibleCompressed(2);

		// Field count:
		writer.WriteInvertibleCompressed(4);

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Schema.NodeId);
		writer.WriteInvertibleCompressed(Project.SelfNodeId);
		writer.WriteInvertibleCompressed(Schema.NodeId);
		
		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// Name:
		writer.WriteInvertibleCompressed(Schema.NameDefId);
		writer.WriteInvertibleUTF8(name);
		writer.WriteInvertibleCompressed(Schema.NameDefId);

		// Data type:
		writer.WriteInvertibleCompressed(Schema.DataTypeDefId);
		writer.WriteInvertibleUTF8(type);
		writer.WriteInvertibleCompressed(Schema.DataTypeDefId);

		// Field count again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(4);

		// Entity create again (for readers travelling backwards):
		writer.WriteInvertibleCompressed(2);

		var first = writer.FirstBuffer;
		var last = writer.LastBuffer;

		writer.FirstBuffer = null;
		writer.LastBuffer = null;
		last.Length = writer.CurrentFill;

		// Release the writer:
		writer.Release();

		var result = await Write(timestamp, first, last);

		if (!result.Valid)
		{
			return null;
		}

		return result.RelevantObject as FieldDefinition;
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

		var timestamp = Timestamp;

		// Create a buffer which will be written out repeatedly:

		// Creating an instance of the definition:
		writer.WriteInvertibleCompressed(definition.Id);

		// 1 field:
		writer.WriteInvertibleCompressed(1);

		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
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

		AddBuffers(timestamp, first, last);
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
	/// Max number of ticks per block. Timestamps are in nanoseconds, and the default is 30s.
	/// </summary>
	public const ulong MaxTicksPerBlock = (ulong)1000000000 * 5; // 30;

	private object _blockHeaderLock = new object();

	private Sha3Digest _writeDigest = null;

	private Writer BuildBlockBoundary(ulong timestamp, long byteOffset, bool isFirstBlock, bool updateDigest = false)
	{
		// Generate and append a block header.
		var writer = Writer.GetPooled();
		writer.Start(null);

		// Block boundary txn:
		writer.WriteInvertibleCompressed(Schema.BlockBoundaryDefId);

		// 4 fields:
		writer.WriteInvertibleCompressed(4);

		// The node ID (can be 0 at the start of the chain). As it is a special field it must occur before timestamp:
		writer.WriteInvertibleCompressed(Schema.NodeId);
		writer.WriteInvertibleCompressed(Project.SelfNodeId);
		writer.WriteInvertibleCompressed(Schema.NodeId);
		
		// Timestamp:
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);
		writer.WriteInvertibleCompressed(timestamp);
		writer.WriteInvertibleCompressed(Schema.TimestampDefId);

		// ByteOffset:
		writer.WriteInvertibleCompressed(Schema.ByteOffsetDefId);
		writer.WriteInvertibleCompressed((ulong)byteOffset);
		writer.WriteInvertibleCompressed(Schema.ByteOffsetDefId);

		// The field ID for the signature is included in the hash:
		writer.WriteInvertibleCompressed(Schema.SignatureDefId);

		// ---------- End of hash for signature region -----------

		// Thus, apply it to the digest:
		var writerBuffer = writer.FirstBuffer;
		var offset = writerBuffer.Offset;
		var handledUpTo = writer.CurrentFill;
		_writeDigest.BlockUpdate(writerBuffer.Bytes, offset, handledUpTo - offset);

		// Get the pre-signed block hash:
		Span<byte> blockHash = stackalloc byte[32];
		_writeDigest.DoFinal(blockHash, 0);

		// Console.WriteLine("[SIGN] Block #x pre-sig hash: " + Hex.Convert(blockHash.ToArray()));

		// Note: The rolling digest was Reset internally DoFinal.

		// Sign the hash using the suitable key.
		// If this is the first block then it is using the project key, otherwise it is using "this" node key.

		ECDsaSigner blockSigner;

		if (isFirstBlock)
		{
			blockSigner = Project.GetProjectSigner();
		}
		else
		{
			blockSigner = Project.GetNodeSigner();
		}

		var signatureBytes = GenerateSignature(blockHash, blockSigner);

		writer.WriteInvertible(signatureBytes);
		writer.WriteInvertibleCompressed(Schema.SignatureDefId);

		// 4 fields (again, for readers going backwards):
		writer.WriteInvertibleCompressed(4);
		writer.WriteInvertibleCompressed(Schema.BlockBoundaryDefId);

		var bytesToAdd = writer.CurrentFill - handledUpTo;

		// Include the signature in the hash. Initialise with the current block hash,
		// then write the rest of the transaction (i.e. incl the signature).
		for (var i = 0; i < blockHash.Length; i++)
		{
			_writeDigest.Update(blockHash[i]);
		}

		// Add the rest of the txn (the signature):
		_writeDigest.BlockUpdate(writerBuffer.Bytes, handledUpTo, bytesToAdd);

		// The result is now the block hash. Must stop/start the digest at this point such that this secondary hash
		// is all that is needed to init a block validation.
		_writeDigest.DoFinal(blockHash, 0);

		// Console.WriteLine("[SIGN] Block #x hash: " + Hex.Convert(blockHash.ToArray()));

		for (var i = 0; i < blockHash.Length; i++)
		{
			_writeDigest.Update(blockHash[i]);
		}

		return writer;
	}

	/// <summary>
	/// Generates a signature for the given hash.
	/// </summary>
	/// <param name="hash"></param>
	/// <param name="signer"></param>
	public byte[] GenerateSignature(Span<byte> hash, ECDsaSigner signer)
	{
		// Todo: BouncyCastle API uses substantial amounts of allocation - tidy up.
		var hashByteArray = new byte[hash.Length];
		hash.CopyTo(hashByteArray);
		BigInteger[] rs = signer.GenerateSignature(hashByteArray);

		var r = rs[0].ToByteArrayUnsigned();
		var s = rs[1].ToByteArrayUnsigned();
		byte[] result = new byte[1 + r.Length + s.Length];

		result[0] = (byte)r.Length;
		Array.Copy(r, 0, result, 1, r.Length);
		Array.Copy(s, 0, result, r.Length + 1, s.Length);
		return result;
	}

	/// <summary>
	/// Gets the total length of bytes in the given buffer set.
	/// </summary>
	/// <param name="buffer"></param>
	/// <returns></returns>
	public long GetLengthForBuffers(BufferedBytes buffer)
	{
		long result = 0;

		while (buffer != null)
		{
			result += (buffer.Length - buffer.Offset);
			buffer = buffer.After;
		}

		return result;
	}

	/// <summary>
	/// Adds the given buffers to the outbound write queue. These buffers must contain a valid network message, 
	/// offset to eliminate the network message header (that's an offset of 9 bytes). All others must have an offset of 0.
	/// </summary>
	/// <param name="timestamp"></param>
	/// <param name="first"></param>
	/// <param name="last"></param>
	/// <returns>True if the buffers were written syncronously.</returns>
	private bool AddBuffers(ulong timestamp, BufferedBytes first, BufferedBytes last)
	{
		// Append the buffers to the write queue.

		// Note that the buffer(s) can be empty and not from a pool.

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

		var writingSync = (writeFirst != null);

		while (writeFirst != null)
		{
			WriteOutQueue(timestamp, writeFirst);
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

		return writingSync;
	}

	/// <summary>
	/// The file buffer size. 16kb is a recommended write buffer size on dotnet 6.
	/// </summary>
	private const int FileBufferSize = 16384;

	private void WriteOutQueue(ulong timestamp, BufferedBytes writeFirst)
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
			WriteFileOffset = writeStream.Length;
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

		// Update the digest and establish how many bytes are being written:
		var buffer = bufferToWrite;
		BufferedBytes lastToWrite = buffer;
		long bytesToWrite = 0;
		while (buffer != null)
		{
			if (buffer.Length != 0)
			{
				_writeDigest.BlockUpdate(buffer.Bytes, buffer.Offset, buffer.Length);
				bytesToWrite += buffer.Length;
			}
			lastToWrite = buffer;
			buffer = buffer.After;
		}

		// Check if it has been more than 30s.
		// These timestamps are in nanoseconds, so that's a fixed gap of..
		if (LastBlockBoundaryTimestamp == 0 || timestamp > (LastBlockBoundaryTimestamp + MaxTicksPerBlock))
		{
			var isFirstBlock = LastBlockBoundaryTimestamp == 0;

			var boundaryTs = Timestamp;

			// Update boundary timestamp and clear has tx flag:
			LastBlockBoundaryTimestamp = boundaryTs;
			HasTransactionsInBlock = false;

			// We'll be writing a block boundary too.
			// First though we need to establish what the byte offset is.
			var offset = WriteFileOffset + bytesToWrite;

			var blockBoundaryWriter = BuildBlockBoundary(boundaryTs, offset, isFirstBlock, true);

			// Get the writers buffers:
			var firstBoundary = blockBoundaryWriter.FirstBuffer;
			var lastBoundary = blockBoundaryWriter.LastBuffer;

			blockBoundaryWriter.FirstBuffer = null;
			blockBoundaryWriter.LastBuffer = null;
			lastBoundary.Length = blockBoundaryWriter.CurrentFill;

			// Add the buffers to the end of the buffer set:
			lastToWrite.After = firstBoundary;
			lastBoundary.After = null;

			// Release the writer:
			blockBoundaryWriter.Release();
		}
		else
		{
			// There are some txns in the block:
			HasTransactionsInBlock = true;
		}

		while (bufferToWrite != null)
		{
			lastBuffer = bufferToWrite;

			// Does it fit in the current write buffer?
			var bytesToCopy = bufferToWrite.Length;

			if (bytesToCopy == 0)
			{
				// Skip:
				bufferToWrite = bufferToWrite.After;
				continue;
			}

			if ((writeFill + bytesToCopy) > FileBufferSize)
			{
				// Write out the write buffer:
				writeStream.Write(writeBuffer, 0, writeFill);
				WriteFileOffset += writeFill;
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
			}

			bufferToWrite = bufferToWrite.After;
		}

		// Last write:
		if (writeFill != 0)
		{
			writeStream.Write(writeBuffer, 0, writeFill);
			WriteFileOffset += writeFill;
		}

		// Flush and force an actual OS write:
		writeStream.Flush(true);

		OnWroteTransactions(writeFirst, lastBuffer);
	}

	/// <summary>
	/// Called when transactions were written to the blockchain file. This only happens if we are the assembly service.
	/// </summary>
	/// <param name="first"></param>
	/// <param name="last"></param>
	protected void OnWroteTransactions(BufferedBytes first, BufferedBytes last)
	{
		// Release the buffers whilst processing them.
		var buff = first;
		while (buff != null)
		{
			var next = buff.After;

			if (_isWatching != WatchMode.None)
			{
				// Note that a dedicated assembly service has its watcher turned off.

				// Pass each buffer to the watcher.
				_txReader.ProcessBuffer(buff);
			}

			#warning todo Send buffer to listening clients.

			if (buff.Pool != null)
			{
				buff.Release();
			}
			buff = next;
		}
	}
}
