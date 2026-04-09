using Api.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Api.Startup;


/// <summary>
/// Holds the current certificate used by Kestrel in Edge mode.
/// </summary>
public static class CertificateHolder
{
	private static X509Certificate2 _singular;

	/// <summary>
	/// Certs by hostname.
	/// </summary>
	private static Dictionary<string, X509Certificate2> _hosts;

	/// <summary>
	/// Gets the cert for the given hostname. Returns a temp self-signed one if none available yet.
	/// </summary>
	/// <param name="hostname"></param>
	/// <returns></returns>
	public static X509Certificate2 GetCertificate(string hostname)
	{
		// Internalised to avoid threading problems
		var cert = _singular;

		if (cert != null)
		{
			return cert;
		}

		var lookup = _hosts;

		if (lookup != null && lookup.TryGetValue(hostname, out cert))
		{
			return cert;
		}

		return GetTemporaryCert();
	}

	/// <summary>
	/// Loads the main set of certs from the given directory (usually ./kestrel)
	/// </summary>
	/// <param name="kestrelConfigPath"></param>
	public static void LoadFromFiles(string kestrelConfigPath)
	{
		var publicUrls = AppSettings.GetRawPublicUrls();

		if (publicUrls == null)
		{
			Log.Warn("certificates", "No public URLs configured - no certs can be loaded");
			return;
		}

		var hostSet = new Dictionary<string, X509Certificate2>();

		// For each public URL, load its cert.
		X509Certificate2 cert = null;

		for (var i = 0; i < publicUrls.Length; i++)
		{
			var publicUrl = publicUrls[i];

			if (string.IsNullOrEmpty(publicUrl))
			{
				continue;
			}

			if (!publicUrl.StartsWith("https://"))
			{
				continue;
			}

			// Get the host:
			var parsedUrl = new Uri(publicUrl);
			var host = parsedUrl.Host;

			var fullPath = $"{kestrelConfigPath}/" + host + "-fullchain.pem";

			if (!File.Exists(fullPath))
			{
				// No certs ready yet - use a temp cert instead.
				cert = GetTemporaryCert();
			}
			else
			{
				cert = X509Certificate2.CreateFromPemFile(
					fullPath,
					$"{kestrelConfigPath}/" + host + "-privkey.pem"
				);
			}

			hostSet[host] = cert;
		}

		if (hostSet.Count == 0)
		{
			_singular = GetTemporaryCert();
		}
		else
		{
			_singular = hostSet.Count == 1 ? cert : null;
		}

		_hosts = hostSet;
	}

	private static X509Certificate2 _temp;

	/// <summary>
	/// During the very first startup when no cert has been obtained yet, 
	/// a temp cert is required for Let's Encrypt validations to pass through. 
	/// That's because we assume everything is redirected to HTTPS (including those requests) such 
	/// that there are no special cases, simplifying the routing logic.
	/// </summary>
	/// <returns></returns>
	private static X509Certificate2 GetTemporaryCert()
	{
		var cert = _temp;
		if (cert == null)
		{
			using var rsa = RSA.Create(2048);
			var request = new CertificateRequest("cn=SocialstackInit", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

			// Export & recreate: this allows Kestrel to access the private key (on dotnet 9 you'll get socket errors otherwise!)
			var fullCert = cert.Export(X509ContentType.Pkcs12);
			cert = X509CertificateLoader.LoadPkcs12(fullCert, null);

			_temp = cert;
		}
		return cert;
	}
}