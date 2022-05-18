using Lumity.BlockChains.Distributors;

namespace Lumity.BlockChains;

/// <summary>
/// Various platforms for distribution of blocks.
/// </summary>
public class BlockChainProjectConfig
{
	
	/// <summary>
	/// Base64 private key.
	/// </summary>
	public string PrivateKey {get;set;}
	
	/// <summary>
	/// Base64 public key.
	/// </summary>
	public string PublicKey {get;set;}
	
	/// <summary>
	/// Config for distribution of the blocks.
	/// </summary>
	public DistributionConfig Distribution {get; set;}
	
}