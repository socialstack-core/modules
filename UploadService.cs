using Api.Configuration;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using Api.Startup;
using Api.Pages;
//using ImageMagick;

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

			Events.Page.BeforeAdminPageInstall.AddEventListener((Context context, Pages.Page page, CanvasRenderer.CanvasNode canvas, Type contentType, AdminPageType pageType) =>
			{
				if (contentType == typeof(Upload))
				{
					if(pageType == AdminPageType.Single)
					{
						// Installing admin page for a particular upload.
						/*
						 Add media display.
						*/
					}
					else if(pageType == AdminPageType.List)
					{
						// Installing admin page for the list of uploads.
						// The create button is actually an uploader.
						canvas.Module = "Admin/Layouts/MediaCenter";
						canvas.Data.Clear();
					}
				}

				return new ValueTask<Pages.Page>(page);
			});

			Events.Upload.Process.AddEventListener(async (Context context, Upload upload) => {

				Image current = null;

				if (IsSupportedImage(upload.FileType))
				{
					upload.IsImage = true;

					if (_configuration.ProcessImages)
					{
						try
						{
							current = Image.FromFile(upload.TemporaryPath);

							if (NormalizeOrientation(current))
							{
								// The image was rotated - we need to overwrite the original:
								current.Save(upload.TemporaryPath);
							}

							// Resize

							var sizes = _configuration.ImageSizes;

							if (sizes != null)
							{
								foreach (var imageSize in sizes)
								{
									// Resize it now:
									var resizedTempFile = Resize(current, imageSize);

									// Ask to store it:
									await Events.Upload.StoreFile.Dispatch(context, upload, resizedTempFile, imageSize.ToString());

									//MagickResize(tempFile, imageSize);
								}
							}

							upload.Width = current.Width;
							upload.Height = current.Height;

							// Done with it:
							current.Dispose();
						}
						catch (OutOfMemoryException e)
						{
							// https://msdn.microsoft.com/en-us/library/4sahykhd(v=vs.110).aspx
							// Not actually an image (or not an image we support).
							// Dump the file and reject the request.
							File.Delete(upload.TemporaryPath);
							upload.TemporaryPath = null;

							Console.WriteLine("Uploaded file is a corrupt image or is too big. Underlying exception: " + e.ToString());
						}
						catch (Exception e)
						{
							// Either the image format is unknown or you don't have the required libraries to decode this format [GDI+ status: UnknownImageFormat]
							// Just ignore this one.
							Console.WriteLine("Unsupported image format was not resized. Underlying exception: " + e.ToString());
						}
					}
				}
				else if (IsOtherImage(upload.FileType))
				{
					upload.IsImage = true;
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

			InstallAdminPages("Media", "fa:fa-film", new string[] { "id", "name" });
		}

		/// <summary>
		/// Gets the file bytes of the given ref, if it is a file ref. Supports remote filesystems as well.
		/// </summary>
		/// <param name="fileRef"></param>
		/// <param name="sizeName"></param>
		/// <param name="altExtension"></param>
		/// <returns></returns>
		public ValueTask<byte[]> GetFileBytes(string fileRef, string sizeName = "original", string altExtension = null)
		{
			var refMeta = FileRef.Parse(fileRef);

			var path = refMeta.GetFilePath(sizeName, altExtension);

			var result = File.ReadAllBytes(path);

			return new ValueTask<byte[]>(result);
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

			// SCHEMA:optionalPath/ID.type.type2

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
		/// <returns>The temp file path the image is at.</returns>
        public string Resize(Image current, int width)
        {
            int height = Convert.ToInt32(width * (double)current.Height / (double)current.Width);
            var canvas = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(canvas))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(current, 0, 0, width, height);
            }

			var targetPath = System.IO.Path.GetTempFileName();

            using (var stream = new FileStream(targetPath, FileMode.OpenOrCreate))
            {
                canvas.Save(stream, current.RawFormat);
            }

            return targetPath;
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

        /// <summary>
        /// True if the filetype is a supported image file.
        /// </summary>
        /// <param name="fileType">The filetype.</param>
        /// <returns></returns>
        public bool IsSupportedImage(string fileType)
        {
            // https://msdn.microsoft.com/en-us/library/4sahykhd(v=vs.110).aspx
            return (
                fileType == "jpg" || fileType == "jpeg" || fileType == "tiff" || 
                fileType == "png" || fileType == "bmp"
            );
        }
		
        /// <summary>
        /// True if the filetype is a non-resizeable image file.
        /// </summary>
        /// <param name="fileType">The filetype.</param>
        /// <returns></returns>
        public bool IsOtherImage(string fileType)
        {
            return (
                fileType == "svg" || fileType == "apng" || fileType == "avif" || 
                fileType == "webp" || fileType == "gif"
            );
        }

		/// <summary>
		/// Orientation tag
		/// </summary>
		private const int ExifOrientationTagId = 274;

		/// <summary>
		/// Strips orientation exif data.
		/// </summary>
		public bool NormalizeOrientation(Image image)
		{
			if (Array.IndexOf(image.PropertyIdList, ExifOrientationTagId) > -1)
			{
				int orientation;

				orientation = image.GetPropertyItem(ExifOrientationTagId).Value[0];

				if (orientation >= 1 && orientation <= 8)
				{
					switch (orientation)
					{
					case 2:
						image.RotateFlip(RotateFlipType.RotateNoneFlipX);
						break;
					case 3:
						image.RotateFlip(RotateFlipType.Rotate180FlipNone);
						break;
					case 4:
						image.RotateFlip(RotateFlipType.Rotate180FlipX);
						break;
					case 5:
						image.RotateFlip(RotateFlipType.Rotate90FlipX);
						break;
					case 6:
						image.RotateFlip(RotateFlipType.Rotate90FlipNone);
						break;
					case 7:
						image.RotateFlip(RotateFlipType.Rotate270FlipX);
						break;
					case 8:
						image.RotateFlip(RotateFlipType.Rotate270FlipNone);
						break;
					}

					image.RemovePropertyItem(ExifOrientationTagId);
					return true;
				}
			}

			return false;
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

			// Start building up the result:
			var result = new Upload()
			{
				OriginalName = fileName,
				IsPrivate = privateUpload,
				FileType = fileType,
				UserId = context.UserId,
				CreatedUtc = DateTime.UtcNow,
				TemporaryPath = tempFilePath
			};

			result = await Events.Upload.BeforeCreate.Dispatch(context, result);

			if (result == null)
			{
				// Reject it.
				return null;
			}

			if (result.Id != 0)
			{
				// Explicit ID has been provided.
				await _database.Run<Upload, uint>(context, createWithIdQuery, result);
			}
			else
			{
				// Obtain an ID now:
				await _database.Run<Upload, uint>(context, createQuery, result);
			}
			
			// Process the upload:
			await Events.Upload.Process.Dispatch(context, result);

			result = await Events.Upload.AfterCreate.Dispatch(context, result);
			return result;
        }
	
	}
}
