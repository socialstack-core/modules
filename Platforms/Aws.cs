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
        /// The S3 service URL.
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
				// Got a space which can be uploaded to:
				SetConfigured("upload");
            }
        }
		
        private IAmazonS3 _uploadClient;

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
            if(_uploadClient == null)
			{

                var s3ClientConfig = new AmazonS3Config
                {
                    ServiceURL = _config.S3ServiceUrl
                };

                var creds = new Amazon.Runtime.BasicAWSCredentials(_config.S3AccessKey, _config.S3AccessSecret);

                _uploadClient = new AmazonS3Client(creds, s3ClientConfig);
            }

            try
            {
                TransferUtility utility = new TransferUtility(_uploadClient);
                
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = _config.S3BucketName + (upload.IsPrivate ? "/content-private" : "/content") + (string.IsNullOrEmpty(upload.Subdirectory) ? "" : "/" + upload.Subdirectory),
                    FilePath = tempFile,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = 6291456, // 6 MB
                    Key = upload.GetRelativePath(variantName),
                    ContentType = upload.GetMimeType(),
                    CannedACL = upload.IsPrivate ? S3CannedACL.AuthenticatedRead : S3CannedACL.PublicRead
                };

                if (!string.IsNullOrEmpty(upload.OriginalName))
                {
                    if (upload.OriginalName.Length > 100)
                    {
                        // This is very likely just someone trying to break things.
                        upload.OriginalName = upload.OriginalName.Substring(0, 100);
                    }

                    var escapedName = Uri.EscapeDataString(upload.OriginalName);

                    // The filename* helps with non-English filenames.
                    fileTransferUtilityRequest.Headers.ContentDisposition = "attachment; filename=\"" + escapedName + "\"; filename*=utf-8''" + escapedName;
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