using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Handles metric source endpoints. 
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial interface IMetricSourceService
    {
        /// <summary>
		/// Delete a metric source by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a metric source by its ID.
		/// </summary>
		Task<MetricSource> Get(Context context, int id);

		/// <summary>
		/// Create a new metric source.
		/// </summary>
		Task<MetricSource> Create(Context context, MetricSource metric);

		/// <summary>
		/// Updates the database with the given metric source data. It must have an ID set.
		/// </summary>
		Task<MetricSource> Update(Context context, MetricSource metric);

		/// <summary>
		/// List a filtered set of metric sources.
		/// </summary>
		/// <returns></returns>
		Task<List<MetricSource>> List(Context context, Filter<MetricSource> filter);
    }
}
