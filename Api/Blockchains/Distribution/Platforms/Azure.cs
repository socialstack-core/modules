using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using System;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;

namespace Lumity.BlockChains.Distributors;

/// <summary>
/// Azure config.
/// </summary>
public partial class AzureConfig
{
	/// <summary>
	/// The connection string for Azure blob storage. Containers called "content" and "content-private" will be created inside this if they don't already exist.
	/// </summary>
	public string StorageConnectionString { get; set; }

	/// <summary>
	/// If true, add content-disposition header as inline to pdf files
	/// </summary>
	public bool DisplayPdfInline { get; set; }

	/// <summary>
	/// True if the files should be proxied via the NGINX API.
	/// </summary>
	public bool LocalProxy { get; set; }

}

/// <summary>
/// A representation of Azure.
/// </summary>
public partial class AzureHost : DistributionPlatform
{
	// private CloudHostService _cloudHost;
	private AzureConfig _config;
	private BlobContainerClient _privateContainer;
	private BlobContainerClient _publicContainer;
	private BlobServiceClient _blobServiceClient;


	/// <summary>
	/// Creates a new Azure host, shared by all requests.
	/// </summary>
	/// <param name="config"></param>
	public AzureHost(AzureConfig config)
	{
		_config = config;

		if (!string.IsNullOrEmpty(config.StorageConnectionString))
		{
			_blobServiceClient = new BlobServiceClient(_config.StorageConnectionString);

			if (!_config.LocalProxy)
			{
				_originUrl = "https://" + _blobServiceClient.Uri.Host;
			}

			SetConfigured("upload");
		}
	}

	private string _originUrl;

	/// <summary>
	/// The URL for the upload host (excluding any paths) if this host platform is providing file services.
	/// </summary>
	/// <returns></returns>
	public override string GetContentUrl()
	{
		return _originUrl;
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
		if (_publicContainer == null)
		{
			await SetupContainers();
		}

		var blobClient = (isPrivate ? _privateContainer : _publicContainer).GetBlobClient(relativeUrl);

		var strResult = await blobClient.DownloadStreamingAsync();

		return strResult.Value.Content;
	}

	private async Task SetupContainers()
	{
		// Verify existence of the two:
		var hasPrivate = false;
		var hasPublic = false;

		await foreach (var container in _blobServiceClient.GetBlobContainersAsync())
		{

			if (container.Name == "content-private")
			{
				hasPrivate = true;
			}
			else if (container.Name == "content")
			{
				hasPublic = true;
			}

		}

		if (!hasPrivate)
		{
			// Private is api only:
			await _blobServiceClient.CreateBlobContainerAsync("content-private", PublicAccessType.None);
		}

		if (!hasPublic)
		{
			// Public is blob - only the api can list files, but anyone can view a file if they have the URL:
			await _blobServiceClient.CreateBlobContainerAsync("content", PublicAccessType.Blob);
		}

		_privateContainer = _blobServiceClient.GetBlobContainerClient("content-private");
		_publicContainer = _blobServiceClient.GetBlobContainerClient("content");
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
		/*
		try
		{
			if (_publicContainer == null)
			{
				await SetupContainers();
			}

			var fileDirectory = string.IsNullOrEmpty(upload.Subdirectory) ? "" : upload.Subdirectory;
			var fileName = upload.GetStoredFilename(variantName);
			var contentType = "application/x-lumity";

			var blobClient = (upload.IsPrivate ? _privateContainer : _publicContainer).GetBlobClient(fileDirectory + "/" + fileName);

			var headers = new BlobHttpHeaders()
			{
				ContentType = contentType
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
					if (_config.DisplayPdfInline && contentType == "application/pdf")
					{
						headers.ContentDisposition = "inline; filename=\"" + escapedName + "\"; filename*=utf-8''" + escapedName;
					}
					else
					{
						headers.ContentDisposition = "attachment; filename=\"" + escapedName + "\"; filename*=utf-8''" + escapedName;
					}
				}
			}

			// Upload data from the temp local file:
			await blobClient.UploadAsync(tempFile, new BlobUploadOptions()
			{
				HttpHeaders = headers
			});
			
			return true;
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
		*/

		return false;
	}
	
}