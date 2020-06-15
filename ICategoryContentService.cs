using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Categories
{
	/// <summary>
	/// Handles categoryContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ICategoryContentService
    {
		/// <summary>
		/// Delete a categoryContent by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a categoryContent by its ID.
		/// </summary>
		Task<CategoryContent> Get(Context context, int id);

		/// <summary>
		/// Create a categoryContent.
		/// </summary>
		Task<CategoryContent> Create(Context context, CategoryContent e);

		/// <summary>
		/// Updates the database with the given categoryContent data. It must have an ID set.
		/// </summary>
		Task<CategoryContent> Update(Context context, CategoryContent e);

		/// <summary>
		/// List a filtered set of categoryContents.
		/// </summary>
		/// <returns></returns>
		Task<List<CategoryContent>> List(Context context, Filter<CategoryContent> filter);

	}
}
