using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PublishGroups
{
	/// <summary>
	/// Handles publishGroupContents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPublishGroupContentService
    {
		/// <summary>
		/// Delete a publishGroupContent by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a publishGroupContent by its ID.
		/// </summary>
		Task<PublishGroupContent> Get(Context context, int id);

		/// <summary>
		/// Create a publishGroupContent.
		/// </summary>
		Task<PublishGroupContent> Create(Context context, PublishGroupContent e);

		/// <summary>
		/// Updates the database with the given publishGroupContent data. It must have an ID set.
		/// </summary>
		Task<PublishGroupContent> Update(Context context, PublishGroupContent e);

		/// <summary>
		/// List a filtered set of publishGroupContents.
		/// </summary>
		/// <returns></returns>
		Task<List<PublishGroupContent>> List(Context context, Filter<PublishGroupContent> filter);

	}
}
