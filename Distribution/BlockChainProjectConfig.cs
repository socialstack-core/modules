using Lumity.BlockChains.Distributors;

namespace Lumity.BlockChains;

/// <summary>
/// Various platforms for distribution of blocks.
/// </summary>
public class BlockChainProjectConfig
{

	/// <summary>
	/// Optional config for distribution of the blocks.
	/// </summary>
	public DistributionConfig Distribution {get; set;}
	
}