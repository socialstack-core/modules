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

    }
    
    /// <summary>
    /// A representation of DigitalOcean.
    /// </summary>
    public partial class DigitalOceanHost : CloudHostPlatform
    {
        // private CloudHostService _cloudHost;
        private DigitalOceanConfig _config;


        /// <summary>
        /// Creates a new digitalocean host, shared by all requests.
        /// </summary>
        /// <param name="chs"></param>
        /// <param name="config"></param>
        public DigitalOceanHost(CloudHostService chs, DigitalOceanConfig config)
        {
            //_cloudHost = chs;
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

                        // Got a space which can be uploaded to:
                        SetConfigured("upload");
                    }
                }
            }
        }

        private string _spaceName;

        private string _spaceRegionUrl;

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
                    ServiceURL = _spaceRegionUrl
                };

                var creds = new Amazon.Runtime.BasicAWSCredentials(_config.SpaceKey, _config.SpaceSecret);

                _uploadClient = new AmazonS3Client(creds, s3ClientConfig);
            }

            try
            {
                TransferUtility utility = new TransferUtility(_uploadClient);
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = _spaceName + (upload.IsPrivate ? "/content-private" : "/content") + (string.IsNullOrEmpty(upload.Subdirectory) ? "" : upload.Subdirectory),
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