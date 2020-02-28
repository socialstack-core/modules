using System;
using Api.Database;
using Api.Users;


namespace Api.Metrics
{
    /// <summary>
    /// A specific source of metrics.
    /// </summary>
    public partial class MetricSource : RevisionRow
    {
		/// <summary>
		/// The metric that this will be supplying data to.
		/// </summary>
		public int MetricId;
		
        /// <summary>
        /// The name of an API event to use as a source. For example, "AnswerBeforeCreate".
        /// </summary>
        public string EventName; 
    }
}
