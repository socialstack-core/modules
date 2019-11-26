using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.Eventing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Notifications;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Handles Metrics.
    /// Instanced automatically. Use Injection to use this service, or Startup.Services.Get. 
    /// </summary>
    public partial class MetricService : IMetricService
    {
        private IDatabaseService _database;

        private readonly Query<Metric> deleteQuery;
        private readonly Query<Metric> createQuery;
        private readonly Query<Metric> selectQuery;
        private readonly Query<Metric> listQuery;
        private readonly Query<Metric> updateQuery;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Service.Get. 
        /// </summary>
        /// <param name="database"></param>
        public MetricService(IDatabaseService database)
        {
            _database = database;

            // Start preparing the queries. Doing this ahead of time leads to excellent performance savings,
            // whilst also using a high-level abstraction as another plugin entry point.
            deleteQuery = Query.Delete<Metric>();
            createQuery = Query.Insert<Metric>();
            updateQuery = Query.Update<Metric>();
            selectQuery = Query.Select<Metric>();
            listQuery = Query.List<Metric>();
        }

        /// <summary>
        /// List a filtered set of metrics
        /// </summary>
        /// <returns></returns>
        public async Task<List<Metric>> List(Context context, Filter<Metric> filter)
        {
            filter = await Events.MetricBeforeList.Dispatch(context, filter);
            var list = await _database.List(listQuery, filter);
            list = await Events.MetricAfterList.Dispatch(context, list);
            return list;
        }

        /// <summary>
        /// Deletes a metric by its ID
        /// </summary>
        public async Task<bool> Delete(Context context, int id, bool deleteContent = true)
        {
            await _database.Run(deleteQuery, id);

            // Ok!
            return true;
        }

        /// <summary>
        /// Gets a metric by its ID.
        /// </summary>
        public async Task<Metric> Get(Context context, int id)
        {
            Metric metric = await _database.Select(selectQuery, id);
            metric = await Events.MetricAfterLoad.Dispatch(context, metric);
            return metric;
        }

        /// <summary>
        /// Create a metric
        /// </summary>
        public async Task<Metric> Create(Context context, Metric metric)
        {
            metric = await Events.MetricBeforeCreate.Dispatch(context, metric);

            // Note: The Id field is automtically updated by Run here.
            if (metric == null || !await _database.Run(createQuery, metric))
            {
                return null;
            }

            metric = await Events.MetricAfterCreate.Dispatch(context, metric);
            return metric;
        }

        /// <summary>
        /// Updates the given metric. If it doesn't exist, it will create it instead. 
        /// </summary>
        public async Task<Metric> Update(Context context, Metric metric)
        {

            
            metric = await Events.MetricBeforeUpdate.Dispatch(context, metric);

            if (metric == null || !await _database.Run(updateQuery, metric, metric.Id))
            {
                return null;
            }

            metric = await Events.MetricAfterUpdate.Dispatch(context, metric);
            return metric;
        }
    }
}
