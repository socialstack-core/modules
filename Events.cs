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
        /// <summary>
		/// Set of events for a Metric.
		/// </summary>
		public static EventGroup<Metric> Metric;
		
        /// <summary>
		/// Set of events for a MetricMeasurement.
		/// </summary>
		public static EventGroup<MetricMeasurement> MetricMeasurement;
		
        /// <summary>
		/// Set of events for a MetricSource.
		/// </summary>
		public static EventGroup<MetricSource> MetricSource;
    }

}
