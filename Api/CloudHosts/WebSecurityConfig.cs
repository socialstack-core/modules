using Api.Configuration;


namespace Api.CloudHosts
{
	
	/// <summary>
	/// The configuration for cloud services.
	/// </summary>
	public partial class WebSecurityConfig: Config
	{
		/// <summary>
		/// True if certs should auto renew.
		/// </summary>
		public bool AutoCertificate {get; set;} = true;
		
		/// <summary>
		/// True if the LE staging site should be used when renewing certs.
		/// </summary>
		public bool UseLetsEncryptStaging {get; set;} = false;
	}

}