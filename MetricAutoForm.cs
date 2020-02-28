using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Used when creating or updating a metric
    /// </summary>
    public partial class MetricAutoForm : AutoForm<Metric>
    {
        /// <summary>
        /// A nice name for this metric.
        /// </summary>
        public string Name;

		/// <summary>
		/// The mode of this metric.
		/// </summary>
		public int Mode = 1;
    }
}
