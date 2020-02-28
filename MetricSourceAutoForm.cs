using Newtonsoft.Json;
using Api.AutoForms;
using System;

namespace Api.Metrics
{
    /// <summary>
    /// Used when creating or updating a metric source
    /// </summary>
    public partial class MetricSourceAutoForm : AutoForm<MetricSource>
    {
        /// <summary>
        /// The name of the event we're listening for.
        /// </summary>
		[Module("Admin/Event/Select")]
        public string EventName;
		
		/// <summary>
		/// The metric that this will be supplying data to.
		/// </summary>
		public int MetricId;
	}
}
