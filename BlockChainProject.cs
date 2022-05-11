using System;

namespace Lumity.BlockChains;



/// <summary>
/// Represents a particular blockchain project (a group of blockchains).
/// </summary>
public class BlockChainProject
{
	/// <summary>
	/// The 4 chains, indexed by ChainType-1.
	/// </summary>
	private BlockChain[] _chains = new BlockChain[4];

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
	{ }

	/// <summary>
	/// May be a slow operation. Loads the project from the given path.
	/// </summary>
	/// <param name="dbPath"></param>
	/// <param name="onTransaction">Called when a transaction is read from any of the projects chains.</param>
	/// <param name="runForFutureTransactions">True if onTransaction should be called if anything is written to the chain(s) in the future.</param>
	public void Load(string dbPath, Action<TransactionReader> onTransaction, bool runForFutureTransactions = true)
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