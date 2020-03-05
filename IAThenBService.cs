using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.IfAThenB
{
	/// <summary>
	/// Handles a then b rules.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IAThenBService
	{
		/// <summary>
		/// Deletes an a then b rule by its ID.
		/// Optionally includes uploaded content refs in there too.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Gets a single a then b rule by its ID.
		/// </summary>
		Task<AThenB> Get(Context context, int id);

		/// <summary>
		/// Creates a new a then b rule.
		/// </summary>
		Task<AThenB> Create(Context context, AThenB athenb);

		/// <summary>
		/// Updates the given a then b rule.
		/// </summary>
		Task<AThenB> Update(Context context, AThenB athenb);

		/// <summary>
		/// List a filtered set of a then b rules.
		/// </summary>
		/// <returns></returns>
		Task<List<AThenB>> List(Context context, Filter<AThenB> filter);
	}
}
