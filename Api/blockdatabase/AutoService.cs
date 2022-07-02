using Lumity.BlockChains;
using System;


/// <summary>
/// A general use service which manipulates an entity type. In the global namespace due to its common use.
/// Deletes, creates, lists and updates them whilst also firing off a series of events.
/// Note that you don't have to inherit this to create a service - it's just for convenience for common functionality.
/// Services are actually detected purely by name.
/// </summary>
public partial class AutoService<T, ID> {
	
	/// <summary>
	/// The chain that the content of this service is written to.
	/// Set automatically at startup based on your DatabaseField group.
	/// </summary>
	public BlockChain Chain;
	
	/// <summary>
	/// The type definition for this service on the chain.
	/// </summary>
	public Definition Definition;
	
}