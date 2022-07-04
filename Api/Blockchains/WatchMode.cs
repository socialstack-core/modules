namespace Lumity.BlockChains;


/// <summary>
/// Mode to use when watching for transactions from a remote source.
/// </summary>
public enum WatchMode
{
	/// <summary>
	/// Not watching.
	/// </summary>
	None = 0,
	
	/// <summary>
	/// Watches for block updates via distribution CDN. Slower but only ever handles full blocks.
	/// Don't use this if you want to directly submit transactions.
	/// </summary>
	Cdn = 1,
	
	/// <summary>
	/// Connects to the assembly service stream to receive realtime txn updates. Required if you want to submit txns.
	/// That's because identifying the transaction in the transaction stream (in realtime) is how a transaction is known to have been received.
	/// </summary>
	Realtime = 2
}