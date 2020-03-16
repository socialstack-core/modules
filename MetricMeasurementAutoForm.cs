using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Used when creating or updating a metric measurement.
    /// </summary>
    public partial class MetricMeasurementAutoForm : AutoForm<MetricMeasurement>
    {
		/// <summary>
		/// The metric that this will be supplying data to.
		/// </summary>
		public int MetricId;
	}
}
