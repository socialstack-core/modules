using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// The result of a completed txn.
/// </summary>
public struct TransactionResult
{
	
	/// <summary>
	/// Most relevant object. This can be a Definition that was just created, a FieldDefinition or a particular entity instance.
	/// </summary>
	public object RelevantObject;
	
	/// <summary>
	/// The discovered transaction ID.
	/// </summary>
	public ulong TransactionId;

	/// <summary>
	/// True if the txn was valid.
	/// </summary>
	public bool Valid;
	
}