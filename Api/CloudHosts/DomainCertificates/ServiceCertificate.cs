using System;

namespace Api.CloudHosts;


/// <summary>
/// A certificate which can be used by the webserver.
/// </summary>
public class ServiceCertificate
{
	
	/// <summary>
	/// Raw textual fullchain (PEM format, compatible with most things such as NGINX).
	/// </summary>
	public string FullchainPem;
	
	/// <summary>
	/// Raw textual private key (PEM format, compatible with most things such as NGINX).
	/// </summary>
	public string PrivateKeyPem;

	/// <summary>
	/// This certs expiry.
	/// </summary>
	public DateTime ExpiryUtc;

}