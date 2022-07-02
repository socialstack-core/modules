using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Galleries
{
	/// <summary>
	/// Handles creations of galleries - containers for image uploads.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IGalleryService
	{
		/// <summary>
		/// Deletes a Gallery by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single Gallery by its ID.
		/// </summary>
		Task<Gallery> Get(Context context, int id);

		/// <summary>
		/// Creates a new Gallery.
		/// </summary>
		Task<Gallery> Create(Context context, Gallery gallery);

		/// <summary>
		/// Updates the given Gallery.
		/// </summary>
		Task<Gallery> Update(Context context, Gallery gallery);

		/// <summary>
		/// List a filtered set of galleries.
		/// </summary>
		/// <returns></returns>
		Task<List<Gallery>> List(Context context, Filter<Gallery> filter);

	}
}
