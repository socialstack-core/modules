using System;
using Api.Database;
using Api.Users;
using Api.AutoForms;

namespace Api.Metrics
{
    /// <summary>
    /// A specific source of metrics.
    /// </summary>
    public partial class MetricSource : VersionedContent<uint>
    {
		/// <summary>
		/// The metric that this will be supplying data to.
		/// </summary>
		public uint MetricId;

        /// <summary>
        /// The name of an API event to use as a source. For example, "AnswerBeforeCreate".
        /// </summary>
        [Module("Admin/Event/Select")]
        public string EventName; 
    }
}
