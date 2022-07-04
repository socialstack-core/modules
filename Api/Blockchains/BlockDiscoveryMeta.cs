using Api.SocketServerLibrary;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Additional information from the block discovery mechanism.
/// </summary>
public struct BlockDiscoveryMeta
{
	/// <summary>
	/// The byte that we've processed up to.
	/// </summary>
	public ulong MaxBytes;

	/// <summary>
	/// Transaction reader which was used for the discovery process.
	/// </summary>
	public TransactionReader Reader;
	
	/// <summary>
	/// A writer potentially containing partial block result, for any transactions at the end of the stream with no header yet.
	/// </summary>
	public Writer Writer;
	
}