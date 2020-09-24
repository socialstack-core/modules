using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Uploader
{
	/// <summary>
	/// Handles uploading of files related to particular pieces of content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IUploadService
    {

		/// <summary>
		/// Gets an upload by its ref.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="uploadRef"></param>
		/// <returns></returns>
		Task<Upload> Get(Context context, string uploadRef);

		/// <summary>
		/// Gets a single upload metadata by its ID.
		/// </summary>
		Task<Upload> Get(Context context, int uploadId);

		/// <summary>
		/// Delete an upload by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="uploadId"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int uploadId);

		/// <summary>
		/// Writes an uploaded file into the content folder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="file">The contents of the file. The name is used to get the filetype.</param>
		/// <param name="sizes">The list of sizes, in pixels, to use if it's an image. These are width values, and source aspect is retained.
		/// If you don't provide sizes, the default config set is used instead.</param>
		/// <returns>Throws exceptions if it failed. Otherwise, returns the information about the file.</returns>
		Task<Upload> Create(Context context, IFormFile file, int[] sizes = null);


		/// <summary>
		/// Updates the meta for a particular upload.
		/// </summary>
		Task<Upload> Update(Context context, Upload upload);

		/// <summary>
		/// List a filtered set of uploads.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		Task<List<Upload>> List(Context context, Filter<Upload> filter);

	}
}
