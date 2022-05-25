using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using System;

namespace Lumity.BlockChains.Distributors;

/// <summary>
/// AWS config.
/// </summary>
public partial class AwsConfig
{
	/// <summary>
	/// The S3 service URL used to declare the region, e.g. s3.eu-west-2.amazonaws.com
	/// </summary>
	public string S3ServiceUrl { get; set; }

	/// <summary>
	/// S3 access key.
	/// </summary>
	public string S3AccessKey { get; set; }

	/// <summary>
	/// S3 access secret.
	/// </summary>
	public string S3AccessSecret { get; set; }

	/// <summary>
	/// S3 bucket name.
	/// </summary>
	public string S3BucketName { get; set; }
}

/// <summary>
/// A representation of AWS.
/// </summary>
public partial class AwsHost : DistributionPlatform
{
	private AwsConfig _config;


	/// <summary>
	/// Creates a new AWS host, shared by all requests.
	/// </summary>
	/// <param name="config"></param>
	public AwsHost(AwsConfig config)
	{
		//_cloudHost = chs;
		_config = config;

		if (!string.IsNullOrEmpty(_config.S3AccessKey) && !string.IsNullOrEmpty(_config.S3AccessSecret) && !string.IsNullOrEmpty(_config.S3ServiceUrl) && !string.IsNullOrEmpty(_config.S3BucketName))
		{
			_cdnUrl = "https://" + _config.S3BucketName + "." + _config.S3ServiceUrl;

			// Got a space which can be uploaded to:
			SetConfigured("upload");
		}

	}

	private string _cdnUrl;
	private IAmazonS3 _uploadClient;

	/// <summary>
	/// The URL for the upload host (excluding any paths) if this host platform is providing file services.
	/// </summary>
	/// <returns></returns>
	public override string GetContentUrl()
	{
		return _cdnUrl;
	}

	/// <summary>
	/// Reads a files bytes from the remote host.
	/// </summary>
	/// <param name="relativeUrl">e.g. 123-original.png</param>
	/// <param name="isPrivate">True if /content-private/, false for regular /content/.</param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public override async Task<System.IO.Stream> ReadFile(string relativeUrl, bool isPrivate)
	{
		var key = (isPrivate ? "content-private/" : "content/") + relativeUrl;
		var str = await _uploadClient.GetObjectStreamAsync(_config.S3BucketName, key, null);
		return str;
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

			var s3ClientConfig = new AmazonS3Config
			{
				ServiceURL = "https://"+ _config.S3ServiceUrl
			};

			var creds = new Amazon.Runtime.BasicAWSCredentials(_config.S3AccessKey, _config.S3AccessSecret);

			_uploadClient = new AmazonS3Client(creds, s3ClientConfig);
		}

		try
		{
			TransferUtility utility = new TransferUtility(_uploadClient);
			var fileTransferUtilityRequest = new TransferUtilityUploadRequest
			{
				BucketName = _config.S3BucketName,
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