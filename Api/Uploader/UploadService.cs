using Api.Contexts;
using Api.Eventing;
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
using Api.ColourConsole;
using System.Globalization;
using System.Text.RegularExpressions;
using Api.Automations;
using Api.Translate;

namespace Api.Uploader
{
    /// <summary>
    /// Handles uploading of files related to particular pieces of content.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class UploadService : AutoService<Upload>
    {
        private UploaderConfig _configuration;

        private static readonly Regex nonAlphaNumericRegex = new Regex("[^a-zA-Z0-9]");

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public UploadService() : base(Events.Upload)
        {
            _configuration = GetConfig<UploaderConfig>();
            UpdateConfig();
            _configuration.OnChange += () =>
            {
                UpdateConfig();
                return new ValueTask();
            };

            // The cron expression runs it every hour.
            Events.Automation("image usage", "0 0 * ? * * *").AddEventListener(async (Context context, AutomationRunInfo runInfo) =>
            {
                await UpdateImageUsage(context);

                return runInfo;
            });


            Events.Upload.BeforeCreate.AddEventListener((Context context, Upload upload) =>
            {

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

                upload.Alt = ExtractAltText(upload);

                return new ValueTask<Upload>(upload);
            }, 9);

            Events.Upload.Process.AddEventListener(async (Context context, Upload upload) =>
            {
                await ProcessImage(context, upload);
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

            Events.Upload.StoreFile.AddEventListener((Context context, Upload upload, string tempFile, string variantName) =>
            {

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
                            WriteColourLine.Error("Unable to set file permissions - skipping. File was " + writePath + " with error " + e.ToString());
                        }
                    }

                    upload = null;
                }

                return new ValueTask<Upload>(upload);
            }, 15);

            Events.Upload.ListFiles.AddEventListener(async (Context context, FileMetaStream metaStream) =>
            {

                if (metaStream.Handled)
                {
                    return metaStream;
                }

                metaStream.Handled = true;

                // Default filesystem handler.
                // Get the complete path:
                var basePath = metaStream.SearchPrivate ? "Content/content-private/" : "Content/content/";

                var filePath = System.IO.Path.GetFullPath(basePath + metaStream.SearchDirectory);

                if (metaStream.Cancelled)
                {
                    return metaStream;
                }

                // dir to search is:
                var dirInfo = new System.IO.DirectoryInfo(filePath);

                // Go!
                foreach (var file in dirInfo.EnumerateFiles("*.*", new EnumerationOptions() { RecurseSubdirectories = true }))
                {
                    if (metaStream.Cancelled)
                    {
                        return metaStream;
                    }

                    metaStream.FileSize = (ulong)file.Length;
                    metaStream.IsDirectory = false;
                    metaStream.LastModifiedUtc = file.LastWriteTimeUtc;
					metaStream.Path = file.FullName.Substring(filePath.Length);

                    await metaStream.OnFile(metaStream);
                    metaStream.FilesListed++;
                }

                /*
				// Directories as well (ignore this!)
				foreach (var dir in dirInfo.EnumerateDirectories())
				{
					if (metaStream.Cancelled)
					{
						return metaStream;
					}

					metaStream.FileSize = 0;
					metaStream.IsDirectory = true;
					metaStream.Path = dir.FullName;

					await metaStream.OnFile(metaStream);
					metaStream.FilesListed++;
				}
				*/

                return metaStream;
            }, 15);

            Events.Upload.ReadFile.AddEventListener(async (Context context, byte[] result, string storagePath, bool isPrivate) =>
            {

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

            Events.Upload.OpenFile.AddEventListener((Context context, Stream result, string storagePath, bool isPrivate) =>
            {

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
        /// Extract alt names from image file data
        /// </summary>
        /// <param name="context"></param>
        public async void UpdateAltNames(Context context)
        {
            var allUploads = await Where().ListAll(context);

            foreach (var upload in allUploads)
            {
                if (string.IsNullOrEmpty(upload.Alt))
                {
                    var altName = ExtractAltText(upload);

                    if (!string.IsNullOrEmpty(altName))
                    {
                        Console.WriteLine($"{upload.Id} {upload.OriginalName} -> {altName}");

                        await Update(context, upload, (Context ctx, Upload up, Upload orig) =>
                        {
                            up.Alt = altName;
                        }, DataOptions.IgnorePermissions);
                    }
                }
            }
        }

        /// <summary>
        /// Update all the image counts
        /// </summary>
        /// <param name="context"></param>
        public async ValueTask UpdateImageUsage(Context context)
        {
            Console.WriteLine("Updating image usage counts");

            var usageMap = new Dictionary<uint, int>();

            // Get all the current locales:
            var _locales = Services.Get<LocaleService>();
            var locales = await _locales.Where("").ListAll(context);

            foreach (var locale in locales)
            {
                context.LocaleId = locale.Id;

                // loop through all content and look for any media refs in use 
                foreach (var kvp in Services.All)
                {
                    await kvp.Value.ActiveRefs(context, usageMap);
                }
            }

            // Load every upload 
            var allUploads = await Where().ListAll(context);

            // Update counts as necessary
            foreach (var upload in allUploads)
            {
                int? usage = usageMap.ContainsKey(upload.Id) ? usageMap[upload.Id] : null;

                await Update(context, upload, (Context ctx, Upload up, Upload orig) =>
                {
                    up.UsageCount = usage;
                }, DataOptions.IgnorePermissions);
            }

            Console.WriteLine("Updated image usage counts - " + usageMap.Sum(x => x.Value));

        }


        /// <summary>
        /// Creates resized and transcoded versions of images for the given upload.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="upload"></param>
        /// <param name="existingFiles">If a target file is listed in existingFiles, it will be skipped.</param>
        /// <returns></returns>
        public async Task<Upload> ProcessImage(Context context, Upload upload, List<FileConsistencyInfo> existingFiles = null)
        {
            MagickImage current = null;

            var imageFormatInfo = IsSupportedImage(upload.FileType);

            if (imageFormatInfo == null)
            {
                return upload;
            }

            if (imageFormatInfo.Value.Format == MagickFormat.Svg)
            {
                // No action needed here.
                return upload;
            }

            var transcodeTo = imageFormatInfo.Value.TranscodeTo;

            int? width = null;
            int? height = null;
            string variants = null;
            string blurHash = null;
            var hasUpdates = false;
            byte[] origFileBytes = null;

            if (_configuration.ProcessImages || existingFiles != null)
            {
                try
                {
                    MagickFormat currentFormat = imageFormatInfo.Value.Format;

                    if (upload.TemporaryPath != null)
                    {
                        current = new MagickImage(upload.TemporaryPath);

                        if (NormalizeOrientation(current))
                        {
                            // The image was rotated - we need to overwrite the original:
                            current.Write(upload.TemporaryPath);
                        }

                        width = current.Width;
                        height = current.Height;
                        hasUpdates = true;
                    }
                    else
                    {
                        width = upload.Width;
                        height = upload.Height;

                        if (width == null || height == null)
                        {
                            // Read the original file and load it:
                            origFileBytes = await ReadFile(upload);
                            current = new MagickImage(origFileBytes);

                            width = current.Width;
                            height = current.Height;
                            hasUpdates = true;
                        }
                    }

                    // If transcoded format is not the same as the actual original, or we want webp always:
                    var willTranscode = (transcodeTo != currentFormat);
                    MagickFormat? doubleOutput = null;
                    string doubleFormatNameNoDot = null;
                    string doubleFormatName = null;

                    if (_configuration.TranscodeToWebP && transcodeTo != MagickFormat.WebP && transcodeTo != MagickFormat.Svg)
                    {
                        // This means it's a web friendly format (jpg, png etc) but not svg.
                        // A webp is required in this scenario.
                        doubleOutput = transcodeTo;
                        transcodeTo = MagickFormat.WebP;
                        willTranscode = true;
                        doubleFormatNameNoDot = doubleOutput.Value.ToString().ToLower();
                        doubleFormatName = "." + doubleFormatNameNoDot;
                        variants = "webp";
                    }

                    var transcodedFormatName = transcodeTo.ToString().ToLower();
                    var formatName = "." + transcodedFormatName;

                    if (willTranscode && transcodedFormatName != upload.FileType && !HasVariationInSet(existingFiles, "original", transcodedFormatName))
                    {
                        variants = transcodedFormatName;

                        // Save original as well, but in the new format:
                        var fullSizeTranscoded = System.IO.Path.GetTempFileName();

                        if (current == null)
                        {
                            // File will be required by one or all of the following operations.
                            if (upload.TemporaryPath == null)
                            {
                                if (origFileBytes == null)
                                {
                                    origFileBytes = await ReadFile(upload);
                                }
                                current = new MagickImage(origFileBytes);
                            }
                            else
                            {
                                current = new MagickImage(upload.TemporaryPath);
                            }
                        }

                        current.Format = transcodeTo;
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
                                if (current != null)
                                {
                                    current.Dispose();
                                    current = null;
                                }
                            }

                            for (var sizeId = 0; sizeId < sizeGroup.Sizes.Count; sizeId++)
                            {
                                var imageSize = sizeGroup.Sizes[sizeId];
                                var imageSizeStr = imageSize.ToString();

                                var hasTargetFile = HasVariationInSet(existingFiles, imageSizeStr, transcodedFormatName);
                                var hasDoubleOutputFile = !doubleOutput.HasValue || HasVariationInSet(existingFiles, imageSizeStr, doubleFormatNameNoDot);
                                var doBlurhash = _smallestSize == imageSize && _configuration.GenerateBlurhash && upload.Blurhash == null;

                                if (hasTargetFile && hasDoubleOutputFile && !doBlurhash)
                                {
                                    // Nothing to do with this size.
                                    // Already got one (or both) files required, or blurhash is known.
                                    continue;
                                }

                                if (current == null)
                                {
                                    // File will be required by one or all of the following operations.
                                    if (upload.TemporaryPath == null)
                                    {
                                        if (origFileBytes == null)
                                        {
                                            origFileBytes = await ReadFile(upload);
                                        }
                                        current = new MagickImage(origFileBytes);
                                    }
                                    else
                                    {
                                        current = new MagickImage(upload.TemporaryPath);
                                    }
                                }

                                // Resize it now:
                                Resize(current, imageSize);

                                if (!hasTargetFile)
                                {
                                    // output the file:
                                    current.Format = transcodeTo;
                                    var resizedTempFile = System.IO.Path.GetTempFileName();
                                    current.Write(resizedTempFile);

                                    // Ask to store it:
                                    await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, imageSizeStr + formatName);
                                }

                                if (doBlurhash)
                                {
                                    // Generate the blurhash using this smallest version of the image (probably 32px)
                                    blurHash = BlurHashEncoder.Encode(current, width > height);
                                    hasUpdates = true;
                                }

                                if (!hasDoubleOutputFile)
                                {
                                    // output the file:
                                    current.Format = doubleOutput.Value;
                                    var resizedTempFile = System.IO.Path.GetTempFileName();
                                    current.Write(resizedTempFile);

                                    // Ask to store it:
                                    await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, imageSizeStr + doubleFormatName);
                                }
                            }
                        }
                    }

                    // image cropping
                    var crops = _configuration.ImageCrops;

                    if (crops != null)
                    {
                        for (var cropId = 0; cropId < crops.Length; cropId++)
                        {
                            // each crop should be in "[width]x[height]" format
                            // (retina-type formats should be defined in "[width]x[height]@2x" format)
                            var crop = _configuration.ImageCrops[cropId];

                            // check for an optional zoom 
                            string[] sizeZoom = crop.Split('@');
                            int zoom = 1;
                            int targetWidth = 0, targetHeight = 0;

                            if (sizeZoom.Length == 2)
                            {
                                crop = sizeZoom[0];
                                string zoomFactor = sizeZoom[1].ToLower();

                                if (zoomFactor.EndsWith("x"))
                                {

                                    if (!int.TryParse(zoomFactor.Remove(zoomFactor.Length - 1), out zoom))
                                    {
                                        zoom = 1;
                                    }

                                }

                            }

                            // grab target width / height
                            string[] dims = crop.Split('x');

                            if (dims.Length == 2)
                            {
                                if (int.TryParse(dims[0], out targetWidth) && int.TryParse(dims[1], out targetHeight))
                                {
                                    targetWidth *= zoom;
                                    targetHeight *= zoom;
                                }
                            }

                            if (targetWidth > 0 && targetHeight > 0)
                            {
                                // Must reload the original image as the previous size group destructively lost data
                                if (current != null)
                                {
                                    current.Dispose();
                                    current = null;
                                }

                                var hasTargetFile = HasVariationInSet(existingFiles, _configuration.ImageCrops[cropId], transcodedFormatName);
                                var hasDoubleOutputFile = !doubleOutput.HasValue || HasVariationInSet(existingFiles, _configuration.ImageCrops[cropId], doubleFormatNameNoDot);
                                // ??
                                /*
                                var doBlurhash = _smallestSize == imageSize && _configuration.GenerateBlurhash && upload.Blurhash == null;

                                if (hasTargetFile && hasDoubleOutputFile && !doBlurhash)
                                {
                                    // Nothing to do with this size.
                                    // Already got one (or both) files required, or blurhash is known.
                                    continue;
                                }
                                */

                                if (current == null)
                                {
                                    // File will be required by one or all of the following operations.
                                    if (upload.TemporaryPath == null)
                                    {
                                        if (origFileBytes == null)
                                        {
                                            origFileBytes = await ReadFile(upload);
                                        }
                                        current = new MagickImage(origFileBytes);
                                    }
                                    else
                                    {
                                        current = new MagickImage(upload.TemporaryPath);
                                    }
                                }

                                int focalX = upload.FocalX == null ? 50 : (int)upload.FocalX;
                                int focalY = upload.FocalY == null ? 50 : (int)upload.FocalY;

                                // Resize it now:
                                ResizeWithFocalPoint(current, targetWidth, targetHeight, focalX, focalY);


                                if (!hasTargetFile)
                                {
                                    // output the file:
                                    current.Format = transcodeTo;
                                    var resizedTempFile = Path.GetTempFileName();
                                    current.Write(resizedTempFile);

                                    // Ask to store it:
                                    await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, _configuration.ImageCrops[cropId] + formatName);
                                }

                                // TODO
                                /*
                                if (doBlurhash)
                                {
                                    // Generate the blurhash using this smallest version of the image (probably 32px)
                                    blurHash = BlurHashEncoder.Encode(current, width > height);
                                    hasUpdates = true;
                                }
								*/

                                if (!hasDoubleOutputFile)
                                {
                                    // output the file:
                                    current.Format = doubleOutput.Value;
                                    var resizedTempFile = Path.GetTempFileName();
                                    current.Write(resizedTempFile);

                                    // Ask to store it:
                                    await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, _configuration.ImageCrops[cropId] + doubleFormatName);
                                }

                            }

                        }

                    }

                    if (current != null)
                    {
                        // Done with it:
                        current.Dispose();
                        current = null;
                    }
                }
                catch (Exception e)
                {
                    // Either the image format is unknown or you don't have the required libraries to decode this format [GDI+ status: UnknownImageFormat]
                    // Just ignore this one.
                    if (upload.TemporaryPath != null)
                    {
                        File.Delete(upload.TemporaryPath);
                        upload.TemporaryPath = null;
                    }

                    Console.WriteLine("Unsupported image format was not resized. Underlying exception: " + e.ToString());
                }
            }

            if (hasUpdates)
            {
                // trigger update to set width/height isImage fields:
                upload = await Update(context, upload, (Context ctx, Upload up, Upload orig) =>
                {
                    up.IsImage = true;
                    up.Width = width;
                    up.Height = height;

                    if (string.IsNullOrEmpty(up.Blurhash) && blurHash != null)
                    {
                        up.Blurhash = blurHash;
                    }

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
            else if (!upload.IsImage)
            {
                // At least mark as an img:
                upload = await Update(context, upload, (Context ctx, Upload up, Upload orig) =>
                {
                    up.IsImage = true;
                }, DataOptions.IgnorePermissions);
            }

            return upload;
        }

        /// <summary>
        /// True if the given variant + filetype is in the given set of files.
        /// </summary>
        /// <param name="existingFiles"></param>
        /// <param name="variant"></param>
        /// <param name="fileType"></param>
        /// <returns></returns>
        private bool HasVariationInSet(List<FileConsistencyInfo> existingFiles, string variant, string fileType)
        {
            if (existingFiles == null)
            {
                return false;
            }

            for (var i = 0; i < existingFiles.Count; i++)
            {
                var file = existingFiles[i];

                if (file.Variant == variant && file.FileType == fileType)
                {
                    return true;
                }
            }

            return false;
        }

        private string ExtractAltText(Upload upload)
        {
            if (string.IsNullOrWhiteSpace(upload.OriginalName))
            {
                return string.Empty;
            }

            var name = Path.GetFileNameWithoutExtension(upload.OriginalName);
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            // split out into an array of 'words'
            var names = nonAlphaNumericRegex.Replace(name, " ").Split(" ", StringSplitOptions.RemoveEmptyEntries);

            // ignore any numbers
            names = names.Where(s => !double.TryParse(s, out _)).ToArray();

            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
            return string.Join(" ", names.Select(s => ti.ToTitleCase(s)));
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

            for (var i = sizeList.Count - 2; i >= 0; i--)
            {
                var currentSize = sizeList[i];

                if (currentSize < currentMin)
                {
                    currentMin = currentSize;
                }

                var added = false;

                for (var x = 0; x < sizeGroups.Count; x++)
                {
                    if (sizeGroups[x].TryAdd(currentSize))
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
        /// <param name="isPrivate">True if listing in the private area.</param>
        /// <param name="relativePath">Path relative to the public/private area.</param>
        /// <param name="onFileListed"></param>
        /// <returns></returns>
        public async Task<FileMetaStream> ListFiles(bool isPrivate, string relativePath, Func<FileMetaStream, ValueTask> onFileListed)
        {
            var metaStream = new FileMetaStream();
            metaStream.SearchPrivate = isPrivate;
            metaStream.SearchDirectory = relativePath;
            metaStream.OnFile = onFileListed;

            // Trigger a read file event. The default handler will read from the file system, 
            // and the CloudHosts module adds a handler if it is handling uploads.
            return await Events.Upload.ListFiles.Dispatch(readBytesContext, metaStream);
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
            await ExtractTar(tarStream, (string path, string name) =>
            {

                // Transfer to storage now.

                // Name may start with output directory:
                if (name.StartsWith("output/"))
                {
                    name = name.Substring(7);
                }

                // Don't await this one:
                _ = Task.Run(async () =>
                {

                    await Events.Upload.StoreFile.Dispatch(context, upload, path, targetDirectory + "/" + name);

                    // Delete the temporary file:
                    File.Delete(path);

                });
            });

            // Update the upload:
            await Update(context, upload, (Context c, Upload u, Upload orig) =>
            {
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
		/// Checks each file in the file system if it matches the current upload policy.
		/// Future: Also will update the DB with missing entries and replace refs if e.g. webp just became available for a particular file.
		/// As you can therefore guess, this is very slow!
		/// </summary>
		/// <param name="context"></param>
		/// <param name="regenerateIfBefore">If set, any non-original files created before the specified UTC date will be regenerated.</param>
		/// <returns></returns>
		public async ValueTask FileConsistency(Context context, DateTime? regenerateIfBefore = null)
        {
            var localBuffer = new List<FileConsistencyInfo>();
            ulong latestNumber = 0;
            var upload = new Upload();

            // Process the local buffer.
            var processBuffer = async () =>
            {

                if (localBuffer.Count == 0)
                {
                    return;
                }

                // Does the buffer have all the sizes we want in it?
                int originalIndex = -1;

                for (var i = 0; i < localBuffer.Count; i++)
                {
                    var file = localBuffer[i];

                    if (file.Path.IndexOf("original", file.DashOffset) == file.DashOffset + 1)
                    {
                        originalIndex = i;
                        break;
                    }
                }

                if (originalIndex == -1)
                {
                    return;
                }

                var original = localBuffer[originalIndex];
                string originalPath = original.Path;
                var uploadId = (uint)original.Number;

                // Get the upload:
                var upload = await Get(context, uploadId);

                if (upload == null)
                {
                    // Create it now.

                    upload = await Create(context, new Upload()
                    {
                        Id = uploadId,
                        OriginalName = original.FileName,
                        IsPrivate = false,
                        FileType = original.FileType,
                        UserId = context.UserId,
                        CreatedUtc = DateTime.UtcNow,
                        TemporaryPath = null,
                        Subdirectory = original.Subdirectory
                    });
                }

                if (upload.Subdirectory != original.Subdirectory)
                {
                    // Multiple uploads with the same ID. Skip!
                    return;
                }

                await ProcessImage(context, upload, localBuffer);
                Console.WriteLine("Process buffer for " + upload.Id);

                // Clear it:
                localBuffer.Clear();
            };

            var dirs = await ListFiles(false, "", async (FileMetaStream fileInfo) =>
            {

                // A consistency capable file is of the form:
                // (DIR/)?NUMBER-original.(.*)

                // If current number is different from latest number, process localBuffer.
                var path = fileInfo.Path;
                var pathLength = path.Length;

                path = path.Replace('\\', '/');

                var startOffset = path.IndexOf('/');
                var directoryOffset = startOffset;

                // Offset to 1 after the /, or from -1 to 0.
                startOffset++;

                // Is it a number followed by a dash?
                ulong num = 0;
                var numValid = false;
                var dashOffset = -1;

                for (var i = startOffset; i < path.Length; i++)
                {
                    var currentChar = path[i];

                    if (currentChar == '-')
                    {
                        dashOffset = i;
                        if (num != 0)
                        {
                            numValid = true;
                        }
                        break;
                    }
                    else if (currentChar >= '0' && currentChar <= '9')
                    {
                        num *= 10;
                        num += (ulong)(currentChar - '0');
                    }
                    else
                    {
                        break;
                    }
                }

                if (numValid)
                {
                    if (num != latestNumber)
                    {
                        latestNumber = num;
                        await processBuffer();
                    }

                    var typeIndex = path.IndexOf('.', dashOffset);
                    var fileName = path.Substring(directoryOffset + 1);
                    var fileType = path.Substring(typeIndex + 1);
                    var variant = path.Substring(dashOffset + 1, typeIndex - dashOffset - 1);

                    if (variant != "original" && regenerateIfBefore.HasValue)
                    {
                        // Skip this file if it is older than the specified UTC timestamp.
                        if (fileInfo.LastModifiedUtc < regenerateIfBefore)
                        {
                            return;
                        }
                    }

                    localBuffer.Add(new FileConsistencyInfo()
                    {
                        Number = num,
                        Path = path,
                        DashOffset = dashOffset,
                        Variant = variant,
                        FileName = fileName,
                        FileType = fileType,
                        DirectoryOffset = directoryOffset
                    });
                }
            });

            // Process last file in buffer:
            await processBuffer();

            Console.WriteLine("Files discovered: " + dirs.FilesListed);

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

        /// <summary>
        /// Resizes the given image to the given dimensions. Does not retain the aspect ratio and performs cropping where necessary.
        /// mimics a command line similar to:
		/// convert input.jpg -resize [width]x[height]^ -extent [width]x[height]+[offsetX]+[offsetY] output.jpg
        /// </summary>
        /// <param name="current"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="focalX"></param>
        /// <param name="focalY"></param>
        public void ResizeWithFocalPoint(MagickImage current, int width, int height, int focalX, int focalY)
        {
            // NB: suffixed with caret to ensure aspect ratio maintained on initial resize
            IMagickGeometry sizeZoomGeometry = new MagickGeometry(width.ToString() + "x" + height.ToString() + "^");
            current.Resize(sizeZoomGeometry);

            int offsetX = 0;
            int offsetY = 0;

            // as the above resize maintained the original aspect ratio, it's likely that one dimension will still be out of whack;
            // figure out which and offset this proportionally using the focal point
            if (current.Width > width)
            {
                offsetX = (int)Math.Round((current.Width - width) * (focalX / 100f));
            }

            if (current.Height > height)
            {
                offsetY = (int)Math.Round((current.Height - height) * (focalY / 100f));
            }

            // extract a chunk of the resized image which matches the image dimensions we originally requested
            IMagickGeometry extentGeometry = new MagickGeometry(width.ToString() + "x" + height.ToString() + "+" + offsetX + "+" + offsetY);
            current.Extent(extentGeometry);
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

        private Dictionary<string, SupportedImageFormat> _imageTypeMap;

        /// <summary>
        /// True if the filetype is a supported image file.
        /// </summary>
        /// <param name="fileType">The filetype.</param>
        /// <returns>The target file type to transcode it to. Null if it is not supported.</returns>
        public SupportedImageFormat? IsSupportedImage(string fileType)
        {
            MagickFormat targetFormat;
            if (_imageTypeMap == null)
            {
                var map = new Dictionary<string, SupportedImageFormat>();
                foreach (var format in MagickNET.SupportedFormats)
                {
                    if (!format.SupportsReading)
                    {
                        continue;
                    }

                    if (format.SupportsMultipleFrames && format.Format != MagickFormat.Gif
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

                    map[magicFileType] = new SupportedImageFormat()
                    {
                        TranscodeTo = targetFormat,
                        Format = format.Format
                    };
                }

                _imageTypeMap = map;
            }

            if (!_imageTypeMap.TryGetValue(fileType, out SupportedImageFormat siFormat))
            {
                return null;
            }

            return siFormat;

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
            get
            {
                if (_autoName == null)
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

            if (string.IsNullOrEmpty(subdirectory))
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

    /// <summary>
    /// A supported image format.
    /// </summary>
    public struct SupportedImageFormat
    {
        /// <summary>
        /// Original format.
        /// </summary>
        public MagickFormat Format;
        /// <summary>
        /// Transcode this format to the given one.
        /// </summary>
        public MagickFormat TranscodeTo;
    }
}
