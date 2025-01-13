using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using Api.Uploader;
using Api.Contexts;
using System;
using System.Collections.Generic;
using Amazon.S3.Model;

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

						// Indicate that this provider can sign URLs:
						SetConfigured("ref-signing");
					}
				}
            }
        }

        private string _spaceName;
        private string _cdnUrl;

        private string _spaceRegionUrl;

        private IAmazonS3 _uploadClient;

		/// <summary>
		/// Gets a signed URL for the given private upload.
		/// </summary>
		/// <param name="upload"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public override string GetSignedRef(Upload upload, string sizeName = "original")
        {
			if (_uploadClient == null)
			{
				SetupClient();
			}

			DateTime expiration = DateTime.UtcNow.AddHours(1);

			var key = (upload.IsPrivate ? "content-private/" : "content/") + upload.GetRelativePath(sizeName);

			var request = new GetPreSignedUrlRequest
			{
				BucketName = _spaceName,
				Key = key,
				Expires = expiration
			};

            return _uploadClient.GetPreSignedURL(request);
		}

		/// <summary>
		/// Reads a files bytes from the remote host.
		/// </summary>
		/// <param name="locator">e.g. 123-original.png</param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public override async Task<System.IO.Stream> ReadFile(FilePartLocator locator)
        {
            if (_uploadClient == null)
            {
                SetupClient();
            }
            
            var key = (locator.IsPrivate ? "content-private/" : "content/") + locator.Path;

            try
            {
                if (locator.Size != 0)
                {
                    var gor = new GetObjectRequest
                    {
                        BucketName = _spaceName,
                        Key = key
                    };

                    gor.ByteRange = new ByteRange(locator.Offset, locator.Offset + locator.Size - 1);

                    var resp = await _uploadClient.GetObjectAsync(gor);
                    return resp.ResponseStream;
                }

                var str = await _uploadClient.GetObjectStreamAsync(_spaceName, key, null);
                return str;
            }
            catch (Amazon.S3.AmazonS3Exception e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
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
		/// Runs when deleting a file.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="fDel"></param>
		/// <returns></returns>
		public override async Task<bool> Delete(Context context, FileDelete fDel)
        {
            if (_uploadClient == null)
            {
                SetupClient();
            }

			try
            {
                await _uploadClient.DeleteAsync(
                    _spaceName,
                    (fDel.IsPrivate ? "content-private/" : "content/") + fDel.Path,
                    null
                );
                return true;
            }
            catch (AmazonS3Exception e)
            {
				// File not found etc.
				Log.Warn("storage", e, "Error when trying to delete a file");
				return false;
            }
            catch (Exception e)
            {
                Log.Warn("storage", e, "Unexpected error when trying to delete a file");
            }

            return false;
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