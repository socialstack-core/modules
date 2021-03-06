using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lumity.BlockChains;


/// <summary>
/// Events indicating a chain object did something notable.
/// </summary>
public enum BlockChainEvent
{
	/// <summary>
	/// Chain was instanced.
	/// </summary>
	Instanced = 1,

	/// <summary>
	/// Chain has finished doing its initial load.
	/// </summary>
	Loaded = 2
}