using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Lumity.BlockChains;



/// <summary>
/// Represents a particular blockchain project (a group of blockchains).
/// </summary>
public class BlockChainProject
{
	/// <summary>
	/// Timestamp precision (currently never changed).
	/// </summary>
	public ulong TimestampPrecision = 1000000000;

	/// <summary>
	/// The date string which timestamps are relative to. This can be safely updated once every 500 years assuming the default timestamp precision.
	/// </summary>
	public uint StartYear = 2000;

	/// <summary>
	/// The name of this chain.
	/// </summary>
	public string BlockchainName;

	/// <summary>
	/// The projects public key.
	/// </summary>
	public byte[] PublicKey;

	/// <summary>
	/// The chain version, often never changes.
	/// </summary>
	public uint Version = 1;

	/// <summary>
	/// The public URL where this chain is running.
	/// </summary>
	public string ServiceUrl;

	/// <summary>
	/// The executable for this project, if there is one.
	/// </summary>
	public byte[] ExecutableArchive;

	/// <summary>
	/// Node Id of the current BAS. If it's 0, the default value is the first defined node.
	/// </summary>
	public uint AssemblerId;

	/// <summary>
	/// This is based on the StartYear.
	/// </summary>
	[JsonIgnore]
	public long TimestampTickOffset;

	/// <summary>
	/// Set to true if this node is the current BAS.
	/// </summary>
	[JsonIgnore]
	public bool IsAssembler = true;

	/// <summary>
	/// The node ID of "this" node.
	/// </summary>
	[JsonIgnore]
	public uint SelfNodeId;

	/// <summary>
	/// The 4 chains, indexed by ChainType-1.
	/// </summary>
	[JsonIgnore]
	private BlockChain[] _chains = new BlockChain[4];

	/// <summary>
	/// Sets the selfNodeId on this project so the BAS state can be easily identified.
	/// </summary>
	/// <param name="selfNodeId"></param>
	public void SetSelfNodeId(uint selfNodeId)
	{
		SelfNodeId = selfNodeId;
		IsAssembler = AssemblerId == 0 || AssemblerId == SelfNodeId;

		Console.WriteLine("- Project updated (Self ready) - " + AssemblerId + ", " + SelfNodeId);

	}

	/// <summary>
	/// Set the current assembler.
	/// </summary>
	public async ValueTask SetAssembler(uint id)
	{
		var publicChain = GetChain(ChainType.Public);

		if (publicChain == null)
		{
			return;
		}

		// Submit txn:
		await publicChain.SetAssembler(id);
	}

	/// <summary>
	/// Called whenever the project is updated (or instanced).
	/// </summary>
	public void Updated()
	{
		// Set the tick offset:
		TimestampTickOffset = new DateTime((int)StartYear, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

		if (_chains != null)
		{
			for (var i = 0; i < _chains.Length; i++)
			{
				var chain = _chains[i];

				if (chain != null)
				{
					chain.ProjectUpdated();
				}
			}
		}

		IsAssembler = AssemblerId == 0 || AssemblerId == SelfNodeId;

		Console.WriteLine("- Project updated - " + AssemblerId + ", " + SelfNodeId);

	}

	/// <summary>
	/// Gets the blockchain schema.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public Lumity.BlockChains.Schema GetSchema(ChainType type)
	{
		return _chains[(int)type - 1].Schema;
	}

	/// <summary>
	/// Gets the blockchain of a given type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public BlockChain GetChain(ChainType type)
	{
		return _chains[(int)type - 1];
	}

	/// <summary>
	/// Creates a blockchain project. Call Load when you're ready to set it up.
	/// </summary>
	public BlockChainProject()
	{
		// Ensure defaults are set:
		Updated();
	}

	/// <summary>
	/// May be a slow operation. Loads the project from the given path.
	/// </summary>
	/// <param name="dbPath"></param>
	/// <param name="onTransaction">Called when a transaction is read from any of the projects chains. Return true if txn was valid.</param>
	/// <param name="runForFutureTransactions">True if onTransaction should be called if anything is written to the chain(s) in the future.</param>
	public void Load(string dbPath, Func<TransactionReader, bool> onTransaction, bool runForFutureTransactions = true)
	{
		// Load the 4 chains:
		_chains[0] = new BlockChain(this, dbPath + "public.lbc", ChainType.Public);
		_chains[0].LoadOrCreate(onTransaction);

		if (runForFutureTransactions)
		{
			_chains[0].Watch(onTransaction);
		}

		_chains[1] = new BlockChain(this, dbPath + "private.lbc", ChainType.Private, _chains[0]);
		_chains[1].LoadOrCreate(onTransaction);

		if (runForFutureTransactions)
		{
			_chains[1].Watch(onTransaction);
		}

		Console.WriteLine("Loading pubhost");
		_chains[2] = new BlockChain(this, dbPath + "public-host.lbc", ChainType.PublicHost);
		_chains[2].LoadOrCreate(onTransaction);

		if (runForFutureTransactions)
		{
			_chains[2].Watch(onTransaction);
		}

		_chains[3] = new BlockChain(this, dbPath + "private-host.lbc", ChainType.PrivateHost, _chains[2]);
		_chains[3].LoadOrCreate(onTransaction);

		if (runForFutureTransactions)
		{
			_chains[3].Watch(onTransaction);
		}
	}
}