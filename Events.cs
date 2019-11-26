using Api.Metrics;
using Api.Permissions;
using System.Collections.Generic;

namespace Api.Eventing
{
    /// <summary>
    /// Events are instanced automatically.
    /// You can however specify a custom type or instance them yourself if you'd liked to do so.
    /// </summary>
    public partial class Events
    {
        #region Service events

        /// <summary>
        /// Just before a new metric is create. The given event won't have an ID yet. Return null to cancel the creation.
        /// </summary>
        public static EventHandler<Metric> MetricBeforeCreate;

        /// <summary>
        /// Just after a metric has been created. The given metric object will now have an ID.
        /// </summary>
        public static EventHandler<Metric> MetricAfterCreate;

        /// <summary>
        /// Just before a metric is being deleted. Return null to cancel the deletion.
        /// </summary>
        public static EventHandler<Metric> MetricBeforeDelete;

        /// <summary>
        /// Just after a metric has been deleted.
        /// </summary>
        public static EventHandler<Metric> MetricAfterDelete;

        /// <summary>
        /// Just before updating a Metric. Optionally make additional changes, or return null to cancel the update. 
        /// </summary>
        public static EventHandler<Metric> MetricBeforeUpdate;

        /// <summary>
        /// Just after updating a metric.
        /// </summary>
        public static EventHandler<Metric> MetricAfterUpdate;

        /// <summary>
        /// Just after a metric was loaded.
        /// </summary>
        public static EventHandler<Metric> MetricAfterLoad;

        /// <summary>
        /// Just before a service loads a metric list.
        /// </summary>
        public static EventHandler<Filter<Metric>> MetricBeforeList;

        /// <summary>
        /// Just after a metric was loaded.
        /// </summary>
        public static EventHandler<List<Metric>> MetricAfterList;
        #endregion

        #region Controller events

        /// <summary>
        /// Create a new metric.
        /// </summary>
        public static EndpointEventHandler<MetricAutoForm> MetricCreate;

        /// <summary>
        /// Delete a metric.
        /// </summary>
        public static EndpointEventHandler<Metric> MetricDelete;

        /// <summary>
        /// Update metric metadate.
        /// </summary>
        public static EndpointEventHandler<MetricAutoForm> MetricUpdate;

        /// <summary>
        /// Load metric metadata.
        /// </summary>
        public static EndpointEventHandler<Metric> MetricLoad;

        /// <summary>
        /// List metrics.
        /// </summary>
        public static EndpointEventHandler<Filter<Metric>> MetricList;

        #endregion
    }

}
