using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Handles metric measurement endpoints. 
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial interface IMetricMeasurementService
    {
        /// <summary>
		/// Delete a metric measurement by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a metric measurement by its ID.
		/// </summary>
		Task<MetricMeasurement> Get(Context context, int id);

		/// <summary>
		/// Create a new metric measurement.
		/// </summary>
		Task<MetricMeasurement> Create(Context context, MetricMeasurement metric);

		/// <summary>
		/// Updates the database with the given metric measurement data. It must have an ID set.
		/// </summary>
		Task<MetricMeasurement> Update(Context context, MetricMeasurement metric);

		/// <summary>
		/// List a filtered set of metric measurements.
		/// </summary>
		/// <returns></returns>
		Task<List<MetricMeasurement>> List(Context context, Filter<MetricMeasurement> filter);
    }
}
