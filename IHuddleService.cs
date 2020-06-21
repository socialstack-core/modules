using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Huddles
{
	/// <summary>
	/// Handles huddles.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IHuddleService
    {
		/// <summary>
		/// Delete a huddle by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a huddle by its ID.
		/// </summary>
		Task<Huddle> Get(Context context, int id);

		/// <summary>
		/// Create a huddle.
		/// </summary>
		Task<Huddle> Create(Context context, Huddle e);

		/// <summary>
		/// Updates the database with the given huddle data. It must have an ID set.
		/// </summary>
		Task<Huddle> Update(Context context, Huddle e);

		/// <summary>
		/// List a filtered set of huddles.
		/// </summary>
		/// <returns></returns>
		Task<List<Huddle>> List(Context context, Filter<Huddle> filter);
		
		/// <summary>
		/// Creates a signed join URL.
		/// </summary>
		Task<string> SignUrl(Context context, Huddle e);
	}
}
