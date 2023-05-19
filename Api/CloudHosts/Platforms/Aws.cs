using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using Api.Uploader;
using Api.Contexts;
using System;

namespace Api.CloudHosts
{

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

        /// <summary>
        /// If true, add content-disposition header as inline to pdf files
        /// </summary>
        public bool DisplayPdfInline { get; set; }

        /// <summary>
        /// Custom URL for the CDN. Of the form https://www.example.com (starts with https, does not end with a fwd slash).
        /// </summary>
        public string CustomCdnUrl { get; set; }

        /// <summary>
        /// Is the s3 bucket locked down for public access and needs to use S3CannedACL.BucketOwnerFullControl
        /// </summary>
        public bool LockedDownAccess { get; set; }

    }

    /// <summary>
    /// A representation of AWS.
    /// </summary>
    public partial class AwsHost : CloudHostPlatform
    {
        // private CloudHostService _cloudHost;
        private AwsConfig _config;


        /// <summary>
        /// Creates a new AWS host, shared by all requests.
        /// </summary>
        /// <param name="chs"></param>
        /// <param name="config"></param>
        public AwsHost(CloudHostService chs, AwsConfig config)
        {
            //_cloudHost = chs;
            _config = config;

            if (!string.IsNullOrEmpty(_config.S3AccessKey) && !string.IsNullOrEmpty(_config.S3AccessSecret) && !string.IsNullOrEmpty(_config.S3ServiceUrl) && !string.IsNullOrEmpty(_config.S3BucketName))
            {
                if (string.IsNullOrEmpty(_config.CustomCdnUrl))
                {
                    _cdnUrl = "https://" + _config.S3BucketName + "." + _config.S3ServiceUrl;
                }
                else
                {
                    _cdnUrl = _config.CustomCdnUrl;
                }

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
		/// Lists files from the storage area into the given file meta stream.
		/// The source can cancel the request via cancelling the meta stream.
		/// </summary>
		/// <param name="metaStream"></param>
		/// <returns></returns>
		public override async Task ListFiles(FileMetaStream metaStream)
		{
			if (_uploadClient == null)
			{
				SetupClient();
			}

			var key = (metaStream.SearchPrivate ? "content-private/" : "content/") + metaStream.SearchDirectory;

			string currentContinuation = null;
			var hasMore = true;

			while (hasMore)
			{
				if (metaStream.Cancelled)
				{
					return;
				}

				var response = await _uploadClient.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request()
				{
					BucketName = _config.S3BucketName,
					Prefix = key,
					ContinuationToken = currentContinuation
				});

				if (metaStream.Cancelled)
				{
					return;
				}

				foreach (var file in response.S3Objects)
				{
                    var meta = metaStream.StartFile();
					meta.FileSize = (ulong)file.Size;
					meta.LastModifiedUtc = file.LastModified.ToUniversalTime();
					meta.Path = file.Key.Substring(key.Length);
                    await metaStream.FileListed(meta);
				}

				if (response.IsTruncated)
				{
					currentContinuation = response.NextContinuationToken;
					hasMore = true;
				}
				else
				{
					hasMore = false;
				}
			}
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
            if (_uploadClient == null)
            {
                SetupClient();
            }
            
            var key = (isPrivate ? "content-private/" : "content/") + relativeUrl;
            var str = await _uploadClient.GetObjectStreamAsync(_config.S3BucketName, key, null);
            return str;
        }

        private void SetupClient()
        {
            if (_uploadClient == null)
            {
                var s3ClientConfig = new AmazonS3Config
                {
                    ServiceURL = "https://" + _config.S3ServiceUrl
                };

                var creds = new Amazon.Runtime.BasicAWSCredentials(_config.S3AccessKey, _config.S3AccessSecret);

                _uploadClient = new AmazonS3Client(creds, s3ClientConfig);
            }
        }

        /// <summary>
        /// Runs when uploading a file.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="upload"></param>
        /// <param name="tempFile"></param>
        /// <param name="variantName"></param>
        /// <returns></returns>
        public override async Task<bool> Upload(Context context, Upload upload, string tempFile, string variantName)
        {
            if (_uploadClient == null)
            {
                SetupClient();
            }
            
            try
            {
                TransferUtility utility = new TransferUtility(_uploadClient);
                
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = _config.S3BucketName,
                    FilePath = tempFile,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = 6291456, // 6 MB
                    Key = (upload.IsPrivate ? "content-private/" : "content/") + upload.GetRelativePath(variantName),
                    ContentType = upload.GetMimeType(variantName),
                };

                // if the s3 bucket has been locked down for no public access 
                // where the files are serviced via a cdn for example 
                if (_config.LockedDownAccess)
                {
                    fileTransferUtilityRequest.CannedACL = S3CannedACL.BucketOwnerFullControl;
                }
                else
                {
                    fileTransferUtilityRequest.CannedACL = upload.IsPrivate ? S3CannedACL.AuthenticatedRead : S3CannedACL.PublicRead;
                }

                if (variantName == "original")
                {
                    if (!string.IsNullOrEmpty(upload.OriginalName))
                    {
                        var name = upload.OriginalName;

                        if (name.Length > 100)
                        {
                            // This is very likely just someone trying to break things.
                            name = name.Substring(0, 100);
                        }

                        var escapedName = Uri.EscapeDataString(name);

                        // The filename* helps with non-English filenames.
                        if (_config.DisplayPdfInline && fileTransferUtilityRequest.ContentType == "application/pdf")
                        {
                            fileTransferUtilityRequest.Headers.ContentDisposition = "inline; filename=\"" + escapedName + "\"; filename*=utf-8''" + escapedName;
                        }
                        else
                        {
                            fileTransferUtilityRequest.Headers.ContentDisposition = "attachment; filename=\"" + escapedName + "\"; filename*=utf-8''" + escapedName;
                        }
                    }
                }

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
 
}