using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Notifications;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Handles metric endpoints. 
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial interface IMetricService
    {
        /// <summary>
        /// Delete a metric by its ID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <param name="deleteContent"></param>
        /// <returns></returns>
        Task<bool> Delete(Context context, int id, bool deleteContent = true);

        /// <summary>
        /// Get a metric by its ID.
        /// </summary>
        Task<Metric> Get(Context context, int id);

        /// <summary>
        /// Create a new metric.
        /// </summary>
        Task<Metric> Create(Context context, Metric metric);

        /// <summary>
        /// Updates the database with the give metric data. It must have an ID set.
        /// </summary>
        Task<Metric> Update(Context context, Metric metric);

        /// <summary>
        /// Lists a filtered set of metrics.
        /// </summary>
        /// <returns></returns>
        Task<List<Metric>> List(Context context, Filter<Metric> filter);
    }
}
