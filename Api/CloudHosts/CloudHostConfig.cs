using Api.Configuration;


namespace Api.CloudHosts
{
	
	/// <summary>
	/// The configuration for cloud services.
	/// </summary>
	public partial class CloudHostConfig: Config
	{

		/// <summary>
		/// Config for digitalOcean.
		/// </summary>
		public DigitalOceanConfig DigitalOcean { get; set; }

		/// <summary>
		/// Config for AWS.
		/// </summary>
		public AwsConfig AWS { get; set; }

		/// <summary>
		/// Config for MS Azure.
		/// </summary>
		public AzureConfig Azure { get; set; }

	}

}