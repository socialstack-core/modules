using Lumity.BlockChains.Distributors;

namespace Lumity.BlockChains;

/// <summary>
/// Various platforms for distribution of blocks.
/// </summary>
public class DistributionConfig
{
	/// <summary>
	/// The projects public hash which is registered (or registerable) in the Lumity Project Index. This is always present in the block file paths. It is hex and lowercase.
	/// </summary>
	public string PublicHash {get; set;}
	
	/// <summary>
	/// Optional config for distribution via uploading to DO spaces.
	/// </summary>
	public DigitalOceanConfig DigitalOcean {get; set;}
	
	/// <summary>
	/// Optional config for distribution via uploading to AWS S3.
	/// </summary>
	public AwsConfig Aws {get; set;}
	
	/// <summary>
	/// Optional config for distribution via uploading to MS Azure.
	/// </summary>
	public AzureConfig Azure {get; set;}
}