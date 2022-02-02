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

        /// <summary>
        /// If true, add content-disposition header as inline to pdf files
        /// </summary>
        public bool DisplayPdfInline { get; set;}

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
                        _cdnUrl = "https://" + partUrl.Replace(".digitaloceanspaces.com", ".cdn.digitaloceanspaces.com");

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
        /// The URL for the upload host (excluding any paths) if this host platform is providing file services. e.g. https://thing.ams.cdn.digitaloceanspaces.com
        /// </summary>
        /// <returns></returns>
        public override string GetContentUrl()
        {
            return _cdnUrl;
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
                    BucketName = _spaceName + (upload.IsPrivate ? "/content-private" : "/content") + (string.IsNullOrEmpty(upload.Subdirectory) ? "" : "/" + upload.Subdirectory),
                    FilePath = tempFile,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = 6291456, // 6 MB
                    Key = upload.GetStoredFilename(variantName),
                    ContentType = upload.GetMimeType(variantName),
                    CannedACL = upload.IsPrivate ? S3CannedACL.AuthenticatedRead : S3CannedACL.PublicRead
                };

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