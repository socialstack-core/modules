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


namespace Api.Uploads
{
	/// <summary>
	/// Handles uploading of files related to particular pieces of content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class UploadService : IUploadService
    {
        private IDatabaseService _database;

		private readonly Query<Upload> deleteQuery;
		private readonly Query<Upload> createQuery;
		private readonly Query<Upload> selectQuery;
		private readonly Query<Upload> updateQuery;
		private readonly Query<Upload> listQuery;


		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public UploadService(IDatabaseService database)
        {
            _database = database;

			// Start preparing the queries. Doing this ahead of time leads to excellent performance savings, 
			// whilst also using a high-level abstraction as another plugin entry point.
			deleteQuery = Query.Delete<Upload>();
			createQuery = Query.Insert<Upload>();
			updateQuery = Query.Update<Upload>();
			selectQuery = Query.Select<Upload>();
			listQuery = Query.List<Upload>();
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
                fileType == "png" || fileType == "gif" || fileType == "bmp"
            );
        }

		/// <summary>
		/// List a filtered set of uploads.
		/// </summary>
		/// <returns></returns>
		public async Task<List<Upload>> List(Context context, Filter<Upload> filter)
		{
			filter = await Events.UploadBeforeList.Dispatch(context, filter);
			var list = await _database.List(listQuery, filter);
			list = await Events.UploadAfterList.Dispatch(context, list);
			return list;
		}

		/// <summary>
		/// Deletes an entry by its ID.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Delete(Context context, int entryId)
        {
			// Delete the entry:
			await _database.Run(deleteQuery, entryId);

			// Ok!
			return true;
		}

		/// <summary>
		/// Gets a single upload by its ID.
		/// </summary>
		public async Task<Upload> Get(Context context, int id)
		{
			return await _database.Select(selectQuery, id);
		}

		/// <summary>
		/// Writes an uploaded file into the content folder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentType">The type of content which is related to this upload.
		/// For example, if it's a user avatar, provide the user type here.</param>
		/// <param name="contentId">E.g. A product ID, forum ID, gallery ID etc.</param>
		/// <param name="file">The contents of the file. The name is used to get the filetype.</param>
		/// <param name="sizes">The list of sizes, in pixels, to use if it's an image. These are width values. Optional.</param>
		/// <returns>Throws exceptions if it failed. Otherwise, returns the information about the file.</returns>
		public async Task<Upload> Create(Context context, Type contentType, int contentId, IFormFile file, int[] sizes = null)
		{
			return await Create(context, ContentTypes.GetId(contentType), contentId, file, sizes);
		}

		/// <summary>
		/// Writes an uploaded file into the content folder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentTypeId">The ContentType Id for the content. See also: ContentTypes class.</param>
		/// <param name="contentId">E.g. A product ID, forum ID, gallery ID etc.</param>
		/// <param name="file">The contents of the file. The name is used to get the filetype.</param>
		/// <param name="sizes">The list of sizes, in pixels, to use if it's an image. These are width values. Optional.</param>
		/// <returns>Throws exceptions if it failed. Otherwise, returns the information about the file.</returns>
		public async Task<Upload> Create(Context context, int contentTypeId, int contentId, IFormFile file, int[] sizes = null)
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
				ContentId = contentId,
				FileType = fileType,
				ContentTypeId = contentTypeId
			};

			result = await Events.UploadBeforeCreate.Dispatch(context, result);

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
			
			Image current = null;

			if (sizes != null && IsSupportedImage(fileType))
			{
				result.IsImage = true;
				try
				{
					current = Image.FromFile(tempFile);
					result.Width = current.Width;
					result.Height = current.Height;
				}
				catch (OutOfMemoryException e)
				{
					// https://msdn.microsoft.com/en-us/library/4sahykhd(v=vs.110).aspx
					// Not actually an image (or not an image we support).
					// Dump the file and reject the request.
					File.Delete(tempFile);

					throw new Exception("Uploaded file is a corrupt image or is too big. Underlying exception: " + e.ToString());
				}
			}
			
			// Obtain an ID now:
			await _database.Run(createQuery, result);

			// The path where we'll write the image:
			var writePath = System.IO.Path.GetFullPath(result.GetFilePath("original"));

			// Create the dirs:
			var dir = System.IO.Path.GetDirectoryName(writePath);

			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			// Relocate the temp file:
			System.IO.File.Move(tempFile, writePath);

			// Resize
            if (current != null)
            {
                foreach (var imageSize in sizes)
                {
                    // Resize it now:
                    Resize(current, result.GetFilePath(imageSize.ToString()), imageSize);
                }
            }
            
            current.Dispose();

			result = await Events.UploadAfterCreate.Dispatch(context, result);
			return result;
        }
	
	}
    
}
