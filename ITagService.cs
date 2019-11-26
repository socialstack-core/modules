using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Tags
{
	/// <summary>
	/// Handles tags - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ITagService
    {
		/// <summary>
		/// Delete a tag by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a tag by its ID.
		/// </summary>
		Task<Tag> Get(Context context, int id);

		/// <summary>
		/// Create a new tag.
		/// </summary>
		Task<Tag> Create(Context context, Tag tag);

		/// <summary>
		/// Updates the database with the given tag data. It must have an ID set.
		/// </summary>
		Task<Tag> Update(Context context, Tag tag);

		/// <summary>
		/// List a filtered set of tags.
		/// </summary>
		/// <returns></returns>
		Task<List<Tag>> List(Context context, Filter<Tag> filter);

	}
}
