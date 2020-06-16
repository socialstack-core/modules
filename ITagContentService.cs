using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Tags
{
	/// <summary>
	/// Handles tagContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ITagContentService
    {
		/// <summary>
		/// Delete a tagContent by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a tagContent by its ID.
		/// </summary>
		Task<TagContent> Get(Context context, int id);

		/// <summary>
		/// Create a tagContent.
		/// </summary>
		Task<TagContent> Create(Context context, TagContent e);

		/// <summary>
		/// Updates the database with the given tagContent data. It must have an ID set.
		/// </summary>
		Task<TagContent> Update(Context context, TagContent e);

		/// <summary>
		/// List a filtered set of tagContents.
		/// </summary>
		/// <returns></returns>
		Task<List<TagContent>> List(Context context, Filter<TagContent> filter);

	}
}
