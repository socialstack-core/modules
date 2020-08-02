using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Huddles
{
	/// <summary>
	/// Handles huddleLoadMetrics.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IHuddleLoadMetricService
    {
		/// <summary>
		/// Delete a huddleLoadMetric by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a huddleLoadMetric by its ID.
		/// </summary>
		Task<HuddleLoadMetric> Get(Context context, int id);

		/// <summary>
		/// Create a huddleLoadMetric.
		/// </summary>
		Task<HuddleLoadMetric> Create(Context context, HuddleLoadMetric e);

		/// <summary>
		/// Updates the database with the given huddleLoadMetric data. It must have an ID set.
		/// </summary>
		Task<HuddleLoadMetric> Update(Context context, HuddleLoadMetric e);

		/// <summary>
		/// List a filtered set of huddleLoadMetrics.
		/// </summary>
		/// <returns></returns>
		Task<List<HuddleLoadMetric>> List(Context context, Filter<HuddleLoadMetric> filter);

	}
}
