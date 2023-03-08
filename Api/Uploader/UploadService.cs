using Api.Contexts;
using Api.Eventing;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Api.Startup;
using Api.Signatures;
using Api.SocketServerLibrary;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using ImageMagick;

namespace Api.Uploader
{
	/// <summary>
	/// Handles uploading of files related to particular pieces of content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UploadService : AutoService<Upload>
    {
		private UploaderConfig _configuration;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UploadService() : base(Events.Upload)
        {
			_configuration = GetConfig<UploaderConfig>();
			UpdateConfig();
			_configuration.OnChange += () => {
				UpdateConfig();
				return new ValueTask();
			};

			Events.Upload.BeforeCreate.AddEventListener((Context context, Upload upload) => {

				if (upload == null)
				{
					return new ValueTask<Upload>(upload);
				}

				var isVideo = false;
				var isAudio = false;

				switch (upload.FileType)
				{
					// Videos
					case "avi":
					case "wmv":
					case "ts":
					case "m3u8":
					case "ogv":
					case "flv":
					case "h264":
					case "h265":
					case "avif":
						isVideo = true;
						break;

					// Audio
					case "wav":
					case "oga":
					case "aac":
					case "mp3":
					case "opus":
					case "weba":
						isAudio = true;
						break;

					// Maybe A/V
					case "webm":
					case "ogg":
					case "mp4":
					case "mkv":
					case "mpeg":
					case "3g2":
					case "3gp":
					case "mov":
					case "media":
						// Assume video for now - we can't probe it until we have the file itself.
						isVideo = true;
						break;
					default:
						// do nothing.
						break;
				}

				upload.IsVideo = isVideo;
				upload.IsAudio = isAudio;

				return new ValueTask<Upload>(upload);
			}, 9);

			Events.Upload.Process.AddEventListener(async (Context context, Upload upload) => {

				MagickImage current = null;

				var transcodeTo = IsSupportedImage(upload.FileType);

				if (transcodeTo == MagickFormat.Svg)
				{
					return upload;
				}

				if (transcodeTo != null)
				{
					int? width = null;
					int? height = null;
					string variants = null;
					string blurHash = null;
					
					if (_configuration.ProcessImages)
					{
						try
						{
							current = new MagickImage(upload.TemporaryPath);

							if (NormalizeOrientation(current))
							{
								// The image was rotated - we need to overwrite the original:
								current.Write(upload.TemporaryPath);
							}

							width = current.Width;
							height = current.Height;

							// If transcoded format is not the same as the actual original, or we want webp always:
							var willTranscode = (transcodeTo.Value != current.Format);
							MagickFormat? doubleOutput = null;
							string doubleFormatName = null;

							if (_configuration.TranscodeToWebP && transcodeTo.Value != MagickFormat.WebP && transcodeTo.Value != MagickFormat.Svg)
							{
								// This means it's a web friendly format (jpg, png etc) but not svg.
								// A webp is required in this scenario.
								doubleOutput = transcodeTo;
								transcodeTo = MagickFormat.WebP;
								willTranscode = true;
								doubleFormatName = "." + doubleOutput.Value.ToString().ToLower();
								variants = "webp";
							}

							current.Format = transcodeTo.Value;

							var baseFormatName = transcodeTo.Value.ToString().ToLower();
							var formatName = "." + baseFormatName;

							if(willTranscode && baseFormatName != upload.FileType)
							{
								variants = baseFormatName;

								// Save original as well, but in the new format:
								var fullSizeTranscoded = System.IO.Path.GetTempFileName();

								current.Write(fullSizeTranscoded);

								// Ask to store it:
								await Events.Upload.StoreFile.Dispatch(context, upload, fullSizeTranscoded, "original" + formatName);
							}
							
							// Resize:
							var sizes = _resizeGroups;

							if (sizes != null)
							{
								for (var gId = 0; gId < sizes.Length; gId++)
								{
									var sizeGroup = _resizeGroups[gId];

									if (gId != 0)
									{
										// Must reload the original image as the previous size group destructively lost data
										current.Dispose();
										current = new MagickImage(upload.TemporaryPath);
									}

									for (var sizeId = 0; sizeId < sizeGroup.Sizes.Count; sizeId++)
									{
										var imageSize = sizeGroup.Sizes[sizeId];

										// Resize it now:
										Resize(current, imageSize);

										// output the file:
										current.Format = transcodeTo.Value;
										var resizedTempFile = System.IO.Path.GetTempFileName();
										current.Write(resizedTempFile);

										// Ask to store it:
										await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, imageSize.ToString() + formatName);

										if (_smallestSize == imageSize)
										{
											if (_configuration.GenerateBlurhash)
											{
												// Generate the blurhash using this smallest version of the image (probably 32px)
												blurHash = BlurHashEncoder.Encode(current, width > height);
											}
										}

										if (doubleOutput.HasValue)
										{
											// output the file:
											current.Format = doubleOutput.Value;
											resizedTempFile = System.IO.Path.GetTempFileName();
											current.Write(resizedTempFile);

											// Ask to store it:
											await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, imageSize.ToString() + doubleFormatName);
										}
									}
								}
							}

							// Current is the smallest size at the moment.
							// This is where a blurhash can best be generated from it.
							
							// Done with it:
							current.Dispose();
						}
						catch (Exception e)
						{
							// Either the image format is unknown or you don't have the required libraries to decode this format [GDI+ status: UnknownImageFormat]
							// Just ignore this one.
							File.Delete(upload.TemporaryPath);
							upload.TemporaryPath = null;

							Console.WriteLine("Unsupported image format was not resized. Underlying exception: " + e.ToString());
						}
					}

					// trigger update to set width/height isImage fields:
					await Update(context, upload, (Context ctx, Upload up, Upload orig) => {

						up.IsImage = true;
						up.Width = width;
						up.Height = height;
						up.Blurhash = blurHash;

						if (!string.IsNullOrEmpty(variants))
						{
							if (string.IsNullOrEmpty(up.Variants))
							{
								up.Variants = variants;
							}
							else
							{
								up.Variants += '|' + variants;
							}
						}

					}, DataOptions.IgnorePermissions);
				}

				return upload;
			}, 10);

			Events.Upload.Process.AddEventListener(async (Context context, Upload upload) =>
			{

				if (upload != null && upload.TemporaryPath != null)
				{
					// Store the original
					await Events.Upload.StoreFile.Dispatch(context, upload, upload.TemporaryPath, "original");
				
				}

				return upload;
			}, 50);

			Events.Upload.StoreFile.AddEventListener((Context context, Upload upload, string tempFile, string variantName) => {

				// Default filesystem move:
				if (upload != null)
				{
					var writePath = System.IO.Path.GetFullPath(upload.GetFilePath(variantName));

					// Create the dirs:
					var dir = System.IO.Path.GetDirectoryName(writePath);

					if (!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}

					System.IO.File.Move(tempFile, writePath);

					if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						// Set perms on the newly uploaded file:
						try
						{
							Chmod.SetRead(writePath);
						}
						catch (Exception e)
						{
							Console.WriteLine("Unable to set file permissions - skipping. File was " + writePath + " with error " + e.ToString());
						}
					}
					
					upload = null;
				}

				return new ValueTask<Upload>(upload);
			}, 15);

			Events.Upload.ReadFile.AddEventListener(async (Context context, byte[] result, string storagePath, bool isPrivate) => {

				if (result != null)
				{
					// Something else has handled this.
					return result;
				}

				// Default filesystem handler.
				// Get the complete path:
				var basePath = isPrivate ? "Content/content-private/" : "Content/content/";

				var filePath = System.IO.Path.GetFullPath(basePath + storagePath);

				result = await File.ReadAllBytesAsync(filePath);

				return result;
			}, 15);
			
			Events.Upload.OpenFile.AddEventListener((Context context, Stream result, string storagePath, bool isPrivate) => {

				if (result != null)
				{
					// Something else has handled this.
					return new ValueTask<Stream>(result);
				}

				// Default filesystem handler.
				// Get the complete path:
				var basePath = isPrivate ? "Content/content-private/" : "Content/content/";

				var filePath = System.IO.Path.GetFullPath(basePath + storagePath);

				result = File.OpenRead(filePath);

				return new ValueTask<Stream>(result);
			}, 15);

			InstallAdminPages("Media", "fa:fa-film", new string[] { "id", "name" });
		}

		/// <summary>
		/// Gets the file bytes of the given ref, if it is a file ref. Supports remote filesystems as well.
		/// </summary>
		/// <param name="upload"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public ValueTask<byte[]> ReadFile(Upload upload, string sizeName = "original")
		{
			// Get path relative to the storage engine:
			var uploadPath = upload.GetRelativePath(sizeName);

			return GetFileBytesForStoragePath(uploadPath, upload.IsPrivate);
		}

		/// <summary>
		/// To make image resizing as efficient as possible without repeatedly reloading the source into memory
		/// a set of "resize groups" are made. These are based on the sizes in the configuration
		/// and are sorted into multiples. For example, powers of 2 are grouped together.
		/// Using default config, 2 groups exist: 2048, 1024, 512, .. 32 is one, and 200, 100 is the other.
		/// </summary>
		private ImageResizeGroup[] _resizeGroups;
		private int _smallestSize;

		private void UpdateConfig()
		{
			var sizes = _configuration.ImageSizes;

			if (sizes == null || sizes.Length == 0)
			{
				_resizeGroups = null;
				return;
			}

			// Sort them:
			var sizeList = new List<int>(sizes);
			sizeList.Sort();

			var sizeGroups = new List<ImageResizeGroup>();

			var currentMin = sizeList[sizeList.Count - 1];

			sizeGroups.Add(new ImageResizeGroup(currentMin));

			for (var i = sizeList.Count - 2;i>=0;i--)
			{
				var currentSize = sizeList[i];

				if (currentSize < currentMin)
				{
					currentMin = currentSize;
				}

				var added = false;

				for (var x = 0; x < sizeGroups.Count; x++)
				{
					if(sizeGroups[x].TryAdd(currentSize))
					{
						added = true;
						break;
					}
				}

				if (!added)
				{
					// New set required:
					sizeGroups.Add(new ImageResizeGroup(currentSize));
				}
			}

			_smallestSize = currentMin;
			_resizeGroups = sizeGroups.ToArray();
		}
		
		/// <summary>
		/// Gets the file bytes of the given ref, if it is a file ref. Supports remote filesystems as well.
		/// </summary>
		/// <param name="fileRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public ValueTask<byte[]> ReadFile(string fileRef, string sizeName = "original")
		{
			var refMeta = FileRef.Parse(fileRef);

			var uploadPath = refMeta.GetRelativePath(sizeName);

			return GetFileBytesForStoragePath(uploadPath, refMeta.Scheme == "private");
		}

		/// <summary>
		/// Gets the file bytes of the given ref, if it is a file ref. Supports remote filesystems as well.
		/// </summary>
		/// <param name="fileRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public ValueTask<byte[]> ReadFile(FileRef fileRef, string sizeName = "original")
		{
			var uploadPath = fileRef.GetRelativePath(sizeName);

			return GetFileBytesForStoragePath(uploadPath, fileRef.Scheme == "private");
		}

		/// <summary>
		/// Gets the file bytes of the given ref, if it is a file ref. Supports remote filesystems as well.
		/// </summary>
		/// <param name="fileRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public ValueTask<Stream> OpenFile(string fileRef, string sizeName = "original")
		{
			var refMeta = FileRef.Parse(fileRef);

			var uploadPath = refMeta.GetRelativePath(sizeName);

			return GetFileStreamForStoragePath(uploadPath, refMeta.Scheme == "private");
		}

		/// <summary>
		/// Opens a stream for the given file ref. Supports large files in remote filesystems. Release the stream when you are done.
		/// </summary>
		/// <param name="fileRef"></param>
		/// <param name="sizeName"></param>
		/// <returns></returns>
		public ValueTask<Stream> OpenFile(FileRef fileRef, string sizeName = "original")
		{
			var uploadPath = fileRef.GetRelativePath(sizeName);

			return GetFileStreamForStoragePath(uploadPath, fileRef.Scheme == "private");
		}

		private Context readBytesContext = new Context();

		/// <summary>
		/// Gets the file bytes for a given storage path. Usually use ReadFile with a ref or an Upload instead.
		/// </summary>
		/// <param name="storagePath"></param>
		/// <param name="isPrivate">True if this is in the private storage area.</param>
		/// <returns></returns>
		public async ValueTask<byte[]> GetFileBytesForStoragePath(string storagePath, bool isPrivate)
		{ 
			// Trigger a read file event. The default handler will read from the file system, 
			// and the CloudHosts module adds a handler if it is handling uploads.
			return await Events.Upload.ReadFile.Dispatch(readBytesContext, null, storagePath, isPrivate);
		}
		
		/// <summary>
		/// Gets the file stream for a given storage path. Usually use OpenFile with a ref or an Upload instead.
		/// </summary>
		/// <param name="storagePath"></param>
		/// <param name="isPrivate">True if this is in the private storage area.</param>
		/// <returns></returns>
		public async ValueTask<Stream> GetFileStreamForStoragePath(string storagePath, bool isPrivate)
		{ 
			// Trigger an open file event. The default handler will read from the file system, 
			// and the CloudHosts module adds a handler if it is handling uploads.
			return await Events.Upload.OpenFile.Dispatch(readBytesContext, null, storagePath, isPrivate);
		}

		private SignatureService _sigService;

		/// <summary>
		/// Extracts a tar to storage.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="uploadId"></param>
		/// <param name="targetDirectory"></param>
		/// <param name="tarStream"></param>
		/// <returns></returns>
		public async ValueTask ExtractTarToStorage(Context context, uint uploadId, string targetDirectory, Stream tarStream)
		{
			// Get the upload:
			var upload = await Get(context, uploadId, DataOptions.IgnorePermissions);

			// First, extract the tar to a series of temporary files.
			// Each time it happens, the temporary file path and its name are given to us.
			await ExtractTar(tarStream, (string path, string name) => {

				// Transfer to storage now.

				// Name may start with output directory:
				if (name.StartsWith("output/"))
				{
					name = name.Substring(7);
				}

				// Don't await this one:
				_ = Task.Run(async () => {

					await Events.Upload.StoreFile.Dispatch(context, upload, path, targetDirectory + "/" + name);

					// Delete the temporary file:
					File.Delete(path);

				});
			});

			// Update the upload:
			await Update(context, upload, (Context c, Upload u, Upload orig) => {
				u.TranscodeState = 2;
			}, DataOptions.IgnorePermissions);

			// Event:
			await Events.Upload.AfterChunksUploaded.Dispatch(context, upload);

		}

		private async ValueTask ExtractTar(Stream stream, Action<string, string> onFile)
		{
			var buffer = new byte[512];

			var streamBytesRead = 0;

			while (true)
			{
				await stream.ReadAsync(buffer, 0, 100);

				var stringEnd = 0;
				for (var i = 0; i < 100; i++)
				{
					if (buffer[i] < 10)
					{
						stringEnd = i;
						break;
					}

				}

				var name = Encoding.ASCII.GetString(buffer, 0, stringEnd).Trim('\0').Trim();

				if (String.IsNullOrWhiteSpace(name))
					break;

				// Skip 24 bytes
				await stream.ReadAsync(buffer, 0, 24);

				// read the size, a 12 byte string:
				await stream.ReadAsync(buffer, 0, 12);
				var sizeString = Encoding.ASCII.GetString(buffer, 0, 12).Trim('\0').Trim();
				var size = Convert.ToInt64(sizeString, 8);

				// 512 byte alignment:
				await stream.ReadAsync(buffer, 0, 376);
				streamBytesRead += 512;

				// Directories have size=0.
				if (size > 0)
				{
					var tempFile = System.IO.Path.GetTempFileName();

					using (var str = File.Open(tempFile, FileMode.OpenOrCreate, FileAccess.Write))
					{
						// Block transfer:
						while (size > 0)
						{
							var bytesToTransfer = size > 512 ? 512 : (int)size;

							var bytesRead = await stream.ReadAsync(buffer, 0, bytesToTransfer);
							await str.WriteAsync(buffer, 0, bytesRead);

							streamBytesRead += bytesRead;
							size -= bytesRead;
						}

					}

					// Completed transferring a file:
					onFile(tempFile, name);
				}

				// 512 alignment:
				int offset = 512 - ((int)streamBytesRead % 512);
				if (offset == 512)
					offset = 0;

				await stream.ReadAsync(buffer, 0, offset);
				streamBytesRead += offset;
			}
		}

		/// <summary>
		/// Gets a transcode token for the given upload ID. It lasts 24h.
		/// </summary>
		/// <returns></returns>
		public string GetTranscodeToken(uint id)
		{
			if (_sigService == null)
			{
				_sigService = Services.Get<SignatureService>();
			}

			// Note: this must of course be a different format from the main user cookie to avoid someone attempting to auth with it.
			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.Write((byte)'1');
			writer.WriteS(DateTime.UtcNow);
			writer.Write((byte)'-');
			writer.WriteS(id);
			writer.WriteASCII("TC");

			_sigService.SignHmac256AlphaChar(writer);
			var tokenStr = writer.ToASCIIString();
			writer.Release();
			return tokenStr;
		}

		/// <summary>
		/// True if the given transcode token is a valid one.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="tokenStr"></param>
		/// <returns></returns>
		public bool IsValidTranscodeToken(uint id, string tokenStr)
		{
			if (_sigService == null)
			{
				_sigService = Services.Get<SignatureService>();
			}
			
			if (tokenStr[0] != '1' || tokenStr.Length < 67)
			{
				// Must start with version 1
				return false;
			}

			if (!_sigService.ValidateHmac256AlphaChar(tokenStr))
			{
				return false;
			}

			// Next, the end of the tx token must be TC, before the signature.

			var sigStart = tokenStr.Length - 64;
			if (tokenStr[sigStart - 1] != 'C' || tokenStr[sigStart - 2] != 'T')
			{
				return false;
			}

			var pieces = tokenStr.Substring(1, sigStart - 3).Split('-');

			if (pieces.Length != 2)
			{
				return false;
			}

			long.TryParse(pieces[0], out long dateStamp);
			uint.TryParse(pieces[1], out uint signedId);

			if (signedId != id)
			{
				return false;
			}

			var dateTime = new DateTime(dateStamp * 10000);

			// 24h expiry.
			if (dateTime.AddDays(1) < DateTime.UtcNow)
			{
				// Expired
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets an upload by its ref.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="uploadRef"></param>
		/// <returns></returns>
		public async ValueTask<Upload> Get(Context context, string uploadRef)
		{
			if (string.IsNullOrEmpty(uploadRef))
			{
				return null;
			}

			// SCHEMA:optionalPath/ID.type.type2|variant?args=1

			// Get the ID from the above. First, split off the schema:
			var pieces = uploadRef.Split(':');
			// ID is always in the last piece. Split off any optional paths:
			pieces = pieces[pieces.Length - 1].Split('/');
			// ID is again always in the last piece. Split off any types:
			pieces = pieces[pieces.Length - 1].Split('.');

			// ID is always the first piece before any types:
			if (!uint.TryParse(pieces[0], out uint id))
			{
				return null;
			}

			return await Get(context, id, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Resizes the given image such that it becomes the given width. Retains the aspect ratio and performs no cropping.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public void Resize(MagickImage current, int width)
        {
			int height = Convert.ToInt32(width * (double)current.Height / (double)current.Width);
			current.Resize(width, height);
         }

		/*
		/// <summary>
		/// Resizes the given image such that it becomes the given width. Retains the aspect ratio and performs no cropping.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public bool MagickResize(string source, int width)
        {

			using (MagickImage image = new MagickImage(source))
			{
				int height = Convert.ToInt32(width * (double)image.Height / (double)image.Width);
				var size = new MagickGeometry(width, height);
				var path = Path.GetFullPath(source);
				var filename = Path.GetFileNameWithoutExtension(source);
				var output = Path.Combine(path, filename + "-" + width.ToString(), ".webp");

				image.Resize(size);
				image.Write(output);
			}
		}
		*/

		private Dictionary<string, MagickFormat> _imageTypeMap; 

        /// <summary>
        /// True if the filetype is a supported image file.
        /// </summary>
        /// <param name="fileType">The filetype.</param>
        /// <returns>The target file type to transcode it to. Null if it is not supported.</returns>
        public MagickFormat? IsSupportedImage(string fileType)
		{
			MagickFormat targetFormat;
			if (_imageTypeMap == null)
			{
				var map = new Dictionary<string, MagickFormat>();
				foreach (var format in MagickNET.SupportedFormats)
				{
					if (!format.IsReadable)
					{
						continue;
					}

					if (format.IsMultiFrame && format.Format != MagickFormat.Gif
						&& format.Format != MagickFormat.Svg
						&& format.Format != MagickFormat.Tif
						&& format.Format != MagickFormat.Tiff
						&& format.Format != MagickFormat.Tiff64
						&& format.Format != MagickFormat.Heic
						&& format.Format != MagickFormat.Heif
						&& format.Format != MagickFormat.Avif
						&& format.Format != MagickFormat.Ico
						&& format.Format != MagickFormat.WebP
						)
					{
						continue;
					}

					targetFormat = format.Format;
					var magicFileType = format.Format.ToString().ToLower();
					
					if (!WebFriendlyFormat(targetFormat))
					{
						// Otherwise it's a web friendly image already (or was webp itself)
						targetFormat = MagickFormat.WebP;
					}

					map[magicFileType] = targetFormat;
				}

				_imageTypeMap = map;
			}

			if (!_imageTypeMap.TryGetValue(fileType, out targetFormat))
			{
				return null;
			}
			return targetFormat;

		}

		/// <summary>
		/// True if the given magick format is web friendly.
		/// </summary>
		/// <param name="format"></param>
		/// <returns></returns>
		public bool WebFriendlyFormat(MagickFormat format)
		{
			var magicFileType = format.ToString().ToLower();
			return magicFileType == "jpg" || magicFileType == "jpeg" || magicFileType == "png" || magicFileType == "avif" || magicFileType == "webp" || magicFileType == "svg";
		}

		/// <summary>
		/// Orientation tag
		/// </summary>
		private const int ExifOrientationTagId = 274;

		/// <summary>
		/// Strips orientation exif data.
		/// </summary>
		public bool NormalizeOrientation(MagickImage image)
		{
			var orientation = image.Orientation;

			if (orientation != OrientationType.TopLeft && orientation != OrientationType.Undefined)
			{
				image.AutoOrient();
				return true;
			}

			// Did nothing
			return false;
		}

		private string _autoName;

		/// <summary>
		/// Automatically generated content subdirectory name.
		/// </summary>
		public string AutoSubdirectoryName
		{
			get{
				if(_autoName == null)
				{
					// Generate an anonymised subdir name based on the hostname of this machine (such that it is consistent, but anonymised)
					using (var md5 = MD5.Create())
					{
						var result = md5.ComputeHash(Encoding.ASCII.GetBytes(System.Environment.MachineName.ToString()));
						_autoName = string.Join("", result.Select(x => x.ToString("X2")));
					}
				}
				
				return _autoName;
			}
		}
	
		/// <summary>
		/// Writes an uploaded file into the content folder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="fileName">The contents of the file. The name is used to get the filetype.</param>
		/// <param name="tempFilePath">The contents of the file.</param>
		/// <param name="privateUpload">True if this is a private upload.</param>
		/// <param name="sizes">The list of sizes, in pixels, to use if it's an image. These are width values. Optional.</param>
		/// <returns>Throws exceptions if it failed. Otherwise, returns the information about the file.</returns>
		public async Task<Upload> Create(Context context, string fileName, string tempFilePath, int[] sizes = null, bool privateUpload = false)
        {
            if (tempFilePath == null || string.IsNullOrEmpty(fileName))
            {
                throw new PublicException("Uploaded file must be provided and the name must be set", "no_name");
            }
            
            // Get the filetype:
            fileName = fileName.Trim();
            var nameParts = fileName.Split('.');
            if (nameParts.Length == 1)
            {
                throw new PublicException("Uploaded file has a name, but not a filetype. The name was '" + fileName + "'", "no_name");
            }

            var fileType = nameParts[nameParts.Length - 1];
            fileType = fileType.ToLower();
			
			var subdirectory = _configuration.Subdirectory;
			
			if(string.IsNullOrEmpty(subdirectory))
			{
				// Always generate a subdirectory name that is unique to this host
				// that avoids file collisions when multiple instances (typically dev ones) are sharing a db
				// The hostname is used to avoid millions of random folders appearing
				subdirectory = AutoSubdirectoryName;
			}
			
			// Start building up the result:
			var result = new Upload()
			{
				OriginalName = fileName,
				IsPrivate = privateUpload,
				FileType = fileType,
				UserId = context.UserId,
				CreatedUtc = DateTime.UtcNow,
				TemporaryPath = tempFilePath,
				Subdirectory = subdirectory
			};

			result = await Events.Upload.BeforeCreate.Dispatch(context, result);

			if (result == null)
			{
				// Reject it.
				return null;
			}

			result = await Events.Upload.Create.Dispatch(context, result);

			if (result == null)
			{
				// Reject it.
				return null;
			}

			result = await Events.Upload.CreatePartial.Dispatch(context, result);

			if (result == null)
			{
				return null;
			}

			// Process the upload:
			await Events.Upload.Process.Dispatch(context, result);

			result = await Events.Upload.AfterCreate.Dispatch(context, result);
			return result;
        }
	
	}

	/// <summary>
	/// A group of image sizes which can be quickly interpolated with no quality loss.
	/// </summary>
	public class ImageResizeGroup
	{
		/// <summary>
		/// Sizes in this group in descending order.
		/// </summary>
		public List<int> Sizes;

		/// <summary>
		/// Creates a group with the given size in it as the largest one in the group (first).
		/// </summary>
		/// <param name="size"></param>
		public ImageResizeGroup(int size)
		{
			Sizes = new List<int>();
			Sizes.Add(size);
		}

		/// <summary>
		/// Try adding the given size. True if it was successful.
		/// A successful add happens when the given size is a multiple of the latest.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public bool TryAdd(int size)
		{
			var latest = Sizes[Sizes.Count - 1];

			if ((latest % size) == 0)
			{
				Sizes.Add(size);
				return true;
			}

			return false;
		}
	}
}
