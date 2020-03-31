using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.GalleryEntries
{
	/// <summary>
	/// Handles gallery entries.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IGalleryEntryService
	{
		/// <summary>
		/// Deletes a entry by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id, bool deleteContent = true);

		/// <summary>
		/// Gets a single entry by its ID.
		/// </summary>
		Task<GalleryEntry> Get(Context context, int id);

		/// <summary>
		/// Creates a new entry.
		/// </summary>
		Task<GalleryEntry> Create(Context context, GalleryEntry entry);

		/// <summary>
		/// Updates the given entry.
		/// </summary>
		Task<GalleryEntry> Update(Context context, GalleryEntry entry);

		/// <summary>
		/// List a filtered set of replies.
		/// </summary>
		/// <returns></returns>
		Task<List<GalleryEntry>> List(Context context, Filter<GalleryEntry> filter);

	}
}
