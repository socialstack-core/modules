using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Events indicating a transaction reader did something notable.
/// </summary>
public enum TransactionReaderEvent
{
	/// <summary>
	/// A reader was instanced.
	/// </summary>
	Instanced = 1
}