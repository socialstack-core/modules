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
			_configuration = AppSettings.GetSection("Uploader").Get<UploaderConfig>();

			if (_configuration == null)
			{
				// Create a default object:
				_configuration = new UploaderConfig();
			}
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
			if (!int.TryParse(pieces[0], out int id))
			{
				return null;
			}

			return await Get(context, id);
		}

		/// <summary>
		/// Resizes the given image such that it becomes the given width. Retains the aspect ratio and performs no cropping.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="targetPath"></param>
		/// <param name="width"></param>
		/// <returns></returns>
        public bool Resize(Image current, string targetPath, int width)
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

            using (var stream = new FileStream(targetPath, FileMode.OpenOrCreate))
            {
                canvas.Save(stream, current.RawFormat);
            }

            return true;
         }
		
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
		/// <param name="file">The contents of the file. The name is used to get the filetype.</param>
		/// <param name="sizes">The list of sizes, in pixels, to use if it's an image. These are width values. Optional.</param>
		/// <returns>Throws exceptions if it failed. Otherwise, returns the information about the file.</returns>
		public async Task<Upload> Create(Context context, IFormFile file, int[] sizes = null)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
            {
                throw new ArgumentNullException("Uploaded file must be provided and the name must be set");
            }
            
            // Get the filetype:
            var fileName = file.FileName.Trim();
            var nameParts = fileName.Split('.');
            if (nameParts.Length == 1)
            {
                throw new Exception("Uploaded file has a name, but not a filetype. The name was '" + file.FileName + "'");
            }

            var fileType = nameParts[nameParts.Length - 1];
            fileType = fileType.ToLower();

			// Write to a temporary path first:
			var tempFile = System.IO.Path.GetTempFileName();

			// Start building up the result:
			var result = new Upload()
			{
				OriginalName = fileName,
				FileType = fileType,
				UserId = context.UserId,
				CreatedUtc = DateTime.UtcNow
			};

			result = await Events.Upload.BeforeCreate.Dispatch(context, result);

			if (result == null)
			{
				// Reject it.
				return null;
			}

			// Save the content now:
			using (var fileStream = new FileStream(tempFile, FileMode.OpenOrCreate))
			{
				await file.CopyToAsync(fileStream);
			}
			
			var saveOriginal = false;
			Image current = null;

			if (IsSupportedImage(fileType))
			{
				result.IsImage = true;
				try
				{
					current = Image.FromFile(tempFile);
					
					if(NormalizeOrientation(current)){
						// The image was rotated - we need to save it as the original.
						saveOriginal = true;
					}
					
					result.Width = current.Width;
					result.Height = current.Height;
					
				}
				catch (OutOfMemoryException e)
				{
					// https://msdn.microsoft.com/en-us/library/4sahykhd(v=vs.110).aspx
					// Not actually an image (or not an image we support).
					// Dump the file and reject the request.
					File.Delete(tempFile);

					Console.WriteLine("Uploaded file is a corrupt image or is too big. Underlying exception: " + e.ToString());
				}
				catch
				{
					// Either the image format is unknown or you don't have the required libraries to decode this format [GDI+ status: UnknownImageFormat]
					// Just ignore this one.
					Console.WriteLine("Unsupported image format was not resized.");
				}
			}else if(IsOtherImage(fileType))
			{
				result.IsImage = true;
			}

			if (result.Id != 0)
			{
				// Explicit ID has been provided.
				await _database.Run(context, createWithIdQuery, result);
			}
			else
			{
				// Obtain an ID now:
				await _database.Run(context, createQuery, result);
			}
			
			// The path where we'll write the image:
			var writePath = System.IO.Path.GetFullPath(result.GetFilePath("original"));

			// Create the dirs:
			var dir = System.IO.Path.GetDirectoryName(writePath);

			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			
			// Resize
            if (current != null)
			{
				if (sizes == null)
				{
					// Use the default set of sizes.
					sizes = _configuration.ImageSizes;
				}
				
				foreach (var imageSize in sizes)
                {
                    // Resize it now:
                    Resize(current, result.GetFilePath(imageSize.ToString()), imageSize);
				}
				
			}
			
			if(saveOriginal){
				// Save img as the original file:
				current.Save(writePath);

				if (current != null)
				{
					current.Dispose();
					current = null;
				}
			
			}
			else
			{
				if (current != null)
				{
					current.Dispose();
					current = null;
				}
				
				// Relocate the temp file:
				System.IO.File.Move(tempFile, writePath);
			}
			
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// Set perms on the newly uploaded file:
				try
				{
					Chmod.SetRead(writePath);
				}
				catch(Exception e)
				{
					Console.WriteLine("Unable to set file permissions - skipping. File was " + writePath + " with error " + e.ToString());
				}
			}
			
			result = await Events.Upload.AfterCreate.Dispatch(context, result);
			return result;
        }
	
	}
    
}
