using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using System;
using Amazon.Runtime.Internal;
using Api.Startup;

namespace Lumity.BlockChains.Distributors;

/// <summary>
/// DigitalOcean config.
/// </summary>
public partial class DigitalOceanConfig
{
	/// <summary>
	/// The space "origin" endpoint URL (not the edge/ CDN one)
	/// </summary>
	public string SpaceOriginUrl { get; set; }

	/// <summary>
	/// A space key.
	/// </summary>
	public string SpaceKey { get; set; }

	/// <summary>
	/// A space secret.
	/// </summary>
	public string SpaceSecret { get; set; }

	/// <summary>
	/// True if should use origin URLs
	/// </summary>
	public bool DisableCDN {get; set;}
}

/// <summary>
/// A representation of DigitalOcean.
/// </summary>
public partial class DigitalOceanHost : DistributionPlatform
{
	// private CloudHostService _cloudHost;
	private DigitalOceanConfig _config;


	/// <summary>
	/// Creates a new digitalocean host, shared by all requests.
	/// </summary>
	/// <param name="config"></param>
	public DigitalOceanHost(DigitalOceanConfig config)
	{
		_config = config;

		if (!string.IsNullOrEmpty(_config.SpaceKey) && !string.IsNullOrEmpty(_config.SpaceSecret) && !string.IsNullOrEmpty(_config.SpaceOriginUrl))
		{
			// Establish the space name + service URL.
			var partUrl = _config.SpaceOriginUrl.Trim().ToLower();

			if (partUrl.StartsWith("https://"))
			{
				// Remove the https:
				partUrl = partUrl.Substring(8);

				var nameAndZone = partUrl.Split('.', 2);

				if (nameAndZone.Length == 2)
				{
					_spaceName = nameAndZone[0];

					// Restore https:
					_spaceRegionUrl = "https://" + nameAndZone[1];
					
					if(_config.DisableCDN){
						_cdnUrl = "https://" + partUrl;
					}else{
						_cdnUrl = "https://" + partUrl.Replace(".digitaloceanspaces.com", ".cdn.digitaloceanspaces.com");
					}
					
					if (_cdnUrl.EndsWith('/'))
					{
						// Remove the last slash:
						_cdnUrl = _cdnUrl.Substring(0, _cdnUrl.Length - 1);
					}

					// Got a space which can be uploaded to:
					SetConfigured("upload");
				}
			}
		}
	}

	private string _spaceName;
	private string _cdnUrl;

	private string _spaceRegionUrl;

	private IAmazonS3 _uploadClient;

	/// <summary>
	/// Reads a files bytes from the remote host.
	/// </summary>
	/// <param name="relativeUrl">e.g. 123-original.png</param>
	/// <param name="isPrivate">True if /content-private/, false for regular /content/.</param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public override async Task<System.IO.Stream> ReadFile(string relativeUrl, bool isPrivate)
	{
		if (_uploadClient == null)
		{
			SetupClient();
		}

		try
		{
			var str = await _uploadClient.GetObjectStreamAsync(_spaceName, relativeUrl, null);
			return str;
		}
		catch (AmazonS3Exception ex)
		{
			if (ex.ErrorCode == "NoSuchKey")
			{
				throw new PublicException("Doesn't exist", "not_found");
			}

			throw;
		}
		
	}

	/// <summary>
	/// The URL for the upload host (excluding any paths) if this host platform is providing file services. e.g. https://thing.ams.cdn.digitaloceanspaces.com
	/// </summary>
	/// <returns></returns>
	public override string GetContentUrl()
	{
		return _cdnUrl;
	}

	private void SetupClient()
	{
		if (_uploadClient == null)
		{
			var s3ClientConfig = new AmazonS3Config
			{
				ServiceURL = _spaceRegionUrl
			};

			var creds = new Amazon.Runtime.BasicAWSCredentials(_config.SpaceKey, _config.SpaceSecret);
			_uploadClient = new AmazonS3Client(creds, s3ClientConfig);
		}
	}

	/// <summary>
	/// Runs when uploading a file.
	/// </summary>
	/// <param name="targetPath">The complete path of the file, including the first forward slash.</param>
	/// <param name="isPrivate"></param>
	/// <param name="toUpload"></param>
	/// <param name="cacheMaxAge"></param>
	/// <param name="contentType"></param>
	/// <returns></returns>
	public async override Task<bool> Upload(string targetPath, bool isPrivate, System.IO.Stream toUpload, int cacheMaxAge = -1, string contentType = "application/x-lumity")
	{
		if(_uploadClient == null)
		{
			SetupClient();
		}

		try
		{
			TransferUtility utility = new TransferUtility(_uploadClient);
			var fileTransferUtilityRequest = new TransferUtilityUploadRequest
			{
				BucketName = _spaceName,
				InputStream = toUpload,
				StorageClass = S3StorageClass.Standard,
				PartSize = 6291456, // 6 MB
				Key = targetPath,
				ContentType = contentType,
				CannedACL = isPrivate ? S3CannedACL.AuthenticatedRead : S3CannedACL.PublicRead
			};

			fileTransferUtilityRequest.Headers.CacheControl = cacheMaxAge == -1 ? "public, max-age=31536000, immutable" : "public, max-age=" + cacheMaxAge;

			// Get the file name:
			var lastSlash = targetPath.LastIndexOf('/');
			var name = targetPath.Substring(lastSlash + 1);

			var escapedName = Uri.EscapeDataString(name);

			fileTransferUtilityRequest.Headers.ContentDisposition = "attachment; filename=\"" + escapedName + "\"; filename*=utf-8''" + escapedName;
			
			await utility.UploadAsync(fileTransferUtilityRequest);
			return true;
		}
		catch (AmazonS3Exception e)
		{
			Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
		}
		catch (Exception e)
		{
			Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
			if (e.Message.Contains("disposed"))
				return true;
		}
		return false;
	}

}