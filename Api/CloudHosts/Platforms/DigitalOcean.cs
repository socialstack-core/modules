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
		
		/// <summary>
		/// True if should use origin URLs
		/// </summary>
		public bool DisableCDN {get; set;}

        /// <summary>
        /// Custom URL for the CDN. Of the form https://www.example.com (starts with https, does not end with a fwd slash).
        /// </summary>
        public string CustomCdnUrl { get; set; }

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

                        if (!string.IsNullOrEmpty(_config.CustomCdnUrl))
                        {
                            _cdnUrl = _config.CustomCdnUrl;
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
            
            var key = (isPrivate ? "content-private/" : "content/") + relativeUrl;
            var str = await _uploadClient.GetObjectStreamAsync(_spaceName, key, null);
            return str;
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
                    BucketName = _spaceName,
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
        /// <param name="context"></param>
        /// <param name="upload"></param>
        /// <param name="tempFile"></param>
        /// <param name="variantName"></param>
        /// <returns></returns>
        public override async Task<bool> Upload(Context context, Upload upload, string tempFile, string variantName)
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
                    FilePath = tempFile,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = 6291456, // 6 MB
                    Key = (upload.IsPrivate ? "content-private" : "content") + (string.IsNullOrEmpty(upload.Subdirectory) ? "/" : "/" + upload.Subdirectory + "/") + upload.GetStoredFilename(variantName),
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