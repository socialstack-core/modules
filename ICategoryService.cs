using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Categories
{
	/// <summary>
	/// Handles categories - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ICategoryService
    {
		/// <summary>
		/// Delete a category by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a category by its ID.
		/// </summary>
		Task<Category> Get(Context context, int id);

		/// <summary>
		/// Create a new category.
		/// </summary>
		Task<Category> Create(Context context, Category category);

		/// <summary>
		/// Updates the database with the given category data. It must have an ID set.
		/// </summary>
		Task<Category> Update(Context context, Category category);

		/// <summary>
		/// List a filtered set of categories.
		/// </summary>
		/// <returns></returns>
		Task<List<Category>> List(Context context, Filter<Category> filter);

	}
}
