using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.PublishGroups
{
	/// <summary>
	/// Handles publishGroups.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IPublishGroupService
    {
		/// <summary>
		/// Delete a publishGroup by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a publishGroup by its ID.
		/// </summary>
		Task<PublishGroup> Get(Context context, int id);

		/// <summary>
		/// Create a publishGroup.
		/// </summary>
		Task<PublishGroup> Create(Context context, PublishGroup e);

		/// <summary>
		/// Updates the database with the given publishGroup data. It must have an ID set.
		/// </summary>
		Task<PublishGroup> Update(Context context, PublishGroup e);

		/// <summary>
		/// List a filtered set of publishGroups.
		/// </summary>
		/// <returns></returns>
		Task<List<PublishGroup>> List(Context context, Filter<PublishGroup> filter);

	}
}
