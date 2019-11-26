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
        /// The ID that is derived by determining what 15 minute slot the metric is measuring.
        /// </summary>
        public int Id;

        /// <summary>
        /// The count of notification sent in this 15 minute block.
        /// </summary>
        public int Count;
    }
}
