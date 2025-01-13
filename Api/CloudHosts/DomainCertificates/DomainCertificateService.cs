using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Eventing;
using System;
using Api.ContentSync;
using LetsEncrypt.Client.Entities;
using LetsEncrypt.Client;
using LetsEncrypt.Client.Cryptography;
using Api.Uploader;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using Org.BouncyCastle.X509;
using Api.PasswordResetRequests;
using Api.Configuration;

namespace Api.CloudHosts;

/// <summary>
/// Handles domainCertificates.
/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
/// </summary>
public partial class DomainCertificateService : AutoService<DomainCertificate>
    {
	private ContentSyncService _contentSync;
	private UploadService _uploads;
	private DomainCertificateChallengeService _challengeService;
	private Dictionary<string, DomainCertificateLocales> _assignedCerts;

	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public DomainCertificateService(ContentSyncService csync, UploadService uploads, DomainCertificateChallengeService challengeService) : base(Events.DomainCertificate)
    {
		_contentSync = csync;
		_uploads = uploads;
		_challengeService = challengeService;
	}

	/// <summary>
	/// Returns the latest set of certificate information. Returns null if it hasn't happened yet.
	/// </summary>
	/// <returns></returns>
	public Dictionary<string, DomainCertificateLocales> GetLatestCertificates()
	{
		return _assignedCerts;
	}

	/// <summary>
	/// Gets the valid set of certificates for all this sites applied domains.
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public async ValueTask<Dictionary<string, DomainCertificateLocales>> UpdateValidSet(Context context)
	{
		// Get all the raw public URLs (arranged by locale ID):
		var publicUrls = AppSettings.GetRawPublicUrls();

		if (publicUrls == null)
		{
			return null;
		}

		string contactEmail = null;
		var uniques = new Dictionary<string, DomainCertificateLocales>();

		// For each public URL, check its cert.
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

			// Generate an email address based on the first seen URI.
			if (contactEmail == null)
			{
				contactEmail = "web-admin@" + host;
			}

			if (uniques.TryGetValue(host, out DomainCertificateLocales locales))
			{
				locales.Add((uint)(i + 1));
			}
			else
			{
				locales = new DomainCertificateLocales() { Host = host };
				locales.Add((uint)(i + 1));
				uniques.Add(host, locales);
			}

		}

		foreach (var kvp in uniques)
		{
			var host = kvp.Value.Host;

			// Either uses a cached one (so long as it hasn't expired) or obtains one.
			kvp.Value.Certificate = await RequireCertificate(context, host, contactEmail);
		}

		_assignedCerts = uniques;
		return uniques;
	}

	/// <summary>
	/// Requests a certificate for http validation. 
	/// If another server has requested a certificate then this request will instead go in to a holding pattern waiting for it.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="domain"></param>
	/// <param name="contactEmail"></param>
	/// <returns></returns>
	public async ValueTask<ServiceCertificate> RequireCertificate(Context context, string domain, string contactEmail)
	{
		var readyCerts = await Where("Domain=? and Status=? and Ready=?", DataOptions.IgnorePermissions)
			.Bind(domain)
			.Bind((uint)1)
			.Bind(true)
			.ListAll(context);

		// Best active cert:
		DomainCertificate best = null;

		// Remove any expired ones.
		// These can fail if another server happened to do this at exactly the same time.
		foreach (var cert in readyCerts)
		{
			if (!cert.ExpiryUtc.HasValue)
			{
				continue;
			}

			// Renew the cert within 10 days of its expiry date.
			if (cert.ExpiryUtc.Value.AddDays(-10) < DateTime.UtcNow)
			{
				try
				{
					await Delete(context, cert, DataOptions.IgnorePermissions);
				}
				catch (Exception)
				{
					// This is ok - we don't really care if it failed. We're just preventing them from building up.
				}
			}
			else if (best != null)
			{
				if (cert.ExpiryUtc > best.ExpiryUtc)
				{
					best = cert;
				}
			}
			else
			{
				best = cert;
			}
		}

		if (best == null)
		{
			// Generate one now.
			Log.Info(LogTag, "Generating a certificate for host '" + domain + "'");
			return await GetCertificate(context, domain, contactEmail);
		}
		else
		{
			// Load the certificate.
			return await ReadCertificate(best);
		}
	}

	/// <summary>
	/// Requests a certificate for http validation. 
	/// If another server has requested a certificate then this request will instead go in to a holding pattern waiting for it.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="domain"></param>
	/// <param name="contactEmail"></param>
	/// <returns></returns>
	public async ValueTask<ServiceCertificate> GetCertificate(Context context, string domain, string contactEmail)
	{
		var now = DateTime.UtcNow;
		var serverId = _contentSync.ServerId;

		// Create a pending record:
		var pendingRecord = await Create(context, new DomainCertificate() {
			Domain = domain,
			LastPingUtc = now,
			FileKey = RandomToken.Generate(16),
			ServerId = serverId
		}, DataOptions.IgnorePermissions);

		// Ask the database if there is another non-expired pending record before ours.
		// The lowest ID wins and all servers wait for that.
		// If the lowest ID is ours then we proceed with it.

		var maxPingTime = now.AddSeconds(-10);

		// - It has a matching domain.
		// - It has a status other than 1.
		// - It is not this same server.
		// - And its lastPingUtc is no longer than 10s ago.
		var pending = await Where("Domain=? and Status!=? and LastPingUtc>? and ServerId!=?", DataOptions.IgnorePermissions)
			.Bind(domain)
			.Bind((uint)1)
			.Bind(maxPingTime)
			.Bind(serverId)
			.ListAll(context);

		var activeRecord = pendingRecord;

		if (pending != null)
		{
			foreach (var record in pending)
			{
				if (record.Id < activeRecord.Id)
				{
					activeRecord = record;
				}
			}
		}

		// If activeRecord is ours, start the request now.
		if (activeRecord == pendingRecord)
		{
			return await RequestCertificate(context, activeRecord, contactEmail);
		}

		// This server is waiting for record being
		// managed by another server in the cluster to become ready.
		for (var i = 0; i < 30; i++)
		{
			// Get the request:
			var latest = await Get(context, activeRecord.Id, DataOptions.IgnorePermissions);

			if (latest.Ready)
			{
				// Success! Read the cert from the filestore.
				return await ReadCertificate(latest);
			}

			if (latest.Status == 1)
			{
				// Failed!
				return null;
			}

			await Task.Delay(2000);
		}

		return null;
	}

	/// <summary>
	/// Reads a cert from the (usually remote) filesystem.
	/// </summary>
	/// <param name="cert"></param>
	/// <returns></returns>
	private async Task<ServiceCertificate> ReadCertificate(DomainCertificate cert)
	{
		if (!cert.ExpiryUtc.HasValue || !cert.Ready)
		{
			return null;
		}

		// Read the bytes of the file:
		var jsonBytes = await _uploads.ReadFile(new Upload()
		{
			Id = 1,
			IsPrivate = true,
			Subdirectory = "letsencrypt-certs",
		}, cert.Id + "-" + cert.FileKey + ".json");

		if (jsonBytes == null)
		{
			return null;
		}

		var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(
			System.Text.Encoding.UTF8.GetString(jsonBytes)
		) as JObject;

		var fullchainPem = jsonData["fullchainPem"].Value<string>();
		var privateKeyPem = jsonData["keyPem"].Value<string>();

		return new ServiceCertificate() {
			ExpiryUtc = cert.ExpiryUtc.Value,
			FullchainPem = fullchainPem,
			PrivateKeyPem = privateKeyPem
		};
	}

	private async Task<ServiceCertificate> RequestCertificate(Context context, DomainCertificate pendingRecord, string contactEmail)
	{
		// Set status to requesting:
		await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
		{
			toUpdate.Status = 2;
			toUpdate.LastPingUtc = DateTime.UtcNow;
		}, DataOptions.IgnorePermissions);

		// Create the request:
		var acmeClient = new AcmeClient(ApiEnvironment.LetsEncryptV2); // LetsEncryptV2Staging if testing

		// Load or create the account:
		var account = await GetAccount(context, acmeClient, contactEmail);

		// Account ping:
		await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
		{
			toUpdate.LastPingUtc = DateTime.UtcNow;
		}, DataOptions.IgnorePermissions);

		// Create the domain order:
		var order = await acmeClient.NewOrderAsync(account, new List<string> { pendingRecord.Domain });

		// Order ping:
		await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
		{
			toUpdate.Status = 3;
			toUpdate.OrderUrl = order.Location.ToString();
			toUpdate.LastPingUtc = DateTime.UtcNow;
		}, DataOptions.IgnorePermissions);

		// Collect HTTP authorisations and add them to the DB:
		List<Challenge> challenges = new List<Challenge>();

		foreach (var authorizationLocation in order.Authorizations)
		{
			var authorization = await acmeClient.GetAuthorizationAsync(account, authorizationLocation);
			var toAdd = authorization.Challenges.Where(i => i.Type == ChallengeType.Http01);

			foreach (var challenge in toAdd)
			{
				await _challengeService.Create(context, new DomainCertificateChallenge()
				{
					DomainCertificateId = pendingRecord.Id,
					Token = challenge.Token,
					VerificationValue = acmeClient.GetChalangeKey(account, challenge.Token),
				}, DataOptions.IgnorePermissions);

				challenges.Add(challenge);
			}

		}

		// All challenges collected and are in the db - wait for validation now.
		await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
		{
			toUpdate.Status = 4;
			toUpdate.LastPingUtc = DateTime.UtcNow;
		}, DataOptions.IgnorePermissions);

		foreach (var challenge in challenges)
		{
			await acmeClient.ValidateChallengeAsync(account, challenge.Url.ToString());
		}

		// All challenges have had their validation requested.
		await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
		{
			toUpdate.Status = 5;
			toUpdate.LastPingUtc = DateTime.UtcNow;
		}, DataOptions.IgnorePermissions);

		// Try checking the validation status:
		for (var i = 0; i < 20; i++)
		{
			var newOrderObject = await acmeClient.GetOrderAsync(account, order.Location);

			if (newOrderObject.Status == OrderStatus.Ready || newOrderObject.Status == OrderStatus.Valid)
			{
				// A certificate is available!
				await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
				{
					toUpdate.Status = 6;
					toUpdate.LastPingUtc = DateTime.UtcNow;
				}, DataOptions.IgnorePermissions);

				// Collect the cert:
				var certificate = await acmeClient.GenerateCertificateAsync(account, order, pendingRecord.Domain);

				if (certificate == null)
				{
					// Fail state:
					await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
					{
						toUpdate.Status = 1;
						toUpdate.Ready = false;
						toUpdate.LastPingUtc = DateTime.UtcNow;
					}, DataOptions.IgnorePermissions);

					return null;
				}
					
				// Write the cert to the filesystem:
				var cert = await StoreCertificate(context, pendingRecord.Id, certificate, pendingRecord.FileKey);

				await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
				{
					toUpdate.Status = 1;
					toUpdate.ExpiryUtc = cert.ExpiryUtc;
					toUpdate.Ready = true;
					toUpdate.LastPingUtc = DateTime.UtcNow;
				}, DataOptions.IgnorePermissions);

				return cert;
			}
			else if (newOrderObject.Status == OrderStatus.Pending)
			{
				// Wait for 2s (40s max).
				await Task.Delay(2000);

				// ping:
				await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
				{
					toUpdate.LastPingUtc = DateTime.UtcNow;
				}, DataOptions.IgnorePermissions);

			}
			else
			{
				// Failed.

				// Stopping there.
				await Update(context, pendingRecord, (Context ctx, DomainCertificate toUpdate, DomainCertificate original) =>
				{
					toUpdate.Status = 1;
					toUpdate.Ready = false;
					toUpdate.LastPingUtc = DateTime.UtcNow;
				}, DataOptions.IgnorePermissions);

				return null;
			}

		}

		return null;
	}

	private async Task<ServiceCertificate> StoreCertificate(Context context, uint certificateId, Certificate certificate, string fileKey)
	{
		var certificateHost = new Upload()
		{
			Id = 1,
			IsPrivate = true,
			Subdirectory = "letsencrypt-certs",
		};

		var fullchainPem = certificate.Serialize();
		var keyPem = certificate.GenerateKeyPem();

		JObject jsonObject = new JObject();
		jsonObject["fullchainPem"] = fullchainPem;
		jsonObject["keyPem"] = keyPem;

		var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);

		// Write to temp file:
		string tempFilePath = System.IO.Path.GetTempFileName();
		await File.WriteAllTextAsync(tempFilePath, json);

		// Write it to the (private) filestore:
		await Events.Upload.StoreFile.Dispatch(context, certificateHost, tempFilePath, certificateId + "-" + fileKey  + ".json");

		// Delete the temp file:
		File.Delete(tempFilePath);

		// Obtain expiry date:
		var parser = new X509CertificateParser();
		var x509Cert = parser.ReadCertificate(certificate.GetOriginalCertificate());
		var expiry = x509Cert.NotAfter.ToUniversalTime();
		
		return new ServiceCertificate
		{
			FullchainPem = fullchainPem,
			PrivateKeyPem = keyPem,
			ExpiryUtc = expiry
		};
	}

	/// <summary>
	/// Gets an account from private storage or creates one.
	/// </summary>
	/// <returns></returns>
	private async Task<Account> GetAccount(Context context, AcmeClient acmeClient, string contactEmail)
	{
		var accountHost = new Upload()
		{
			Id = 1,
			IsPrivate = true,
			Subdirectory = "letsencrypt-certs",
		};

		var accountJsonBytes = await _uploads.ReadFile(accountHost, "account.json");
		string accountStr;

		if (accountJsonBytes == null)
		{
			// The account does not exist. Create one now.
			Console.WriteLine("Hello, world! This server is creating a certificate account in order to initialise HTTPS for this project.");

			// Create the account now:
			var account = await acmeClient.CreateNewAccountAsync(contactEmail);

			// build a JSON string:
			var jsonObject = new JObject();
			jsonObject["contactEmail"] = contactEmail;
			jsonObject["location"] = account.Location.AbsoluteUri;
			jsonObject["privateKeyPem"] = account.Key.ToPrivateKeyPem();
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);

			// Write to temp file:
			string tempFilePath = System.IO.Path.GetTempFileName();
			await File.WriteAllTextAsync(tempFilePath, json);

			// Write it to the (private) filestore:
			await Events.Upload.StoreFile.Dispatch(context, accountHost, tempFilePath, "account.json");

			// Delete the temp file:
			File.Delete(tempFilePath);

			// NB: we'll serialise and immediately deserialise - this is intentional. It checks that the JSON loader is running correctly.
			accountStr = json;
		}
		else
		{
			accountStr = System.Text.Encoding.UTF8.GetString(accountJsonBytes);
		}

		// It's a JSON file containing a location URL, private key (as a textual pem) and the contact email.
		var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject(
			accountStr
		) as JObject;

		var keyPem = jsonData["privateKeyPem"].Value<string>();

		var key = new RsaKeyPair(keyPem);
		var location = jsonData["location"].Value<string>();

		return new Account(key, location);

	}
	
}
