using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Uploads
{
	/// <summary>
	/// Handles uploading of files related to particular pieces of content.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IUploadService
    {
		/// <summary>
		/// Delete an upload by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="entryId"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int entryId);

		/// <summary>
		/// Writes an uploaded file into the content folder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="contentTypeId">The ContentType Id for the content. See also: ContentTypes class.</param>
		/// <param name="contentId">E.g. A product ID, forum ID, gallery ID etc.</param>
		/// <param name="file">The contents of the file. The name is used to get the filetype.</param>
		/// <param name="sizes">The list of sizes, in pixels, to use if it's an image. These are width values. Optional.</param>
		/// <returns>Throws exceptions if it failed. Otherwise, returns the information about the file.</returns>
		Task<Upload> Create(Context context, int contentTypeId, int contentId, IFormFile file, int[] sizes = null);

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
		Task<Upload> Create(Context context, Type contentType, int contentId, IFormFile file, int[] sizes = null);

		/// <summary>
		/// List a filtered set of uploads.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="filter"></param>
		/// <returns></returns>
		Task<List<Upload>> List(Context context, Filter<Upload> filter);

	}
}
