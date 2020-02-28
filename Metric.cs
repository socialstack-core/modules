using System;
using Api.Database;
using Api.Users;

namespace Api.Metrics
{
    /// <summary>
    /// Metrics are used to measure the amount of occurrences of triggers (called a metric source) over time.
    /// </summary>
    public partial class Metric : RevisionRow
    {
        /// <summary>
        /// A name for this metric.
        /// </summary>
        public string Name;
		
		/// <summary>
		/// The display mode for this metric.
		/// </summary>
		public int Mode = 1;
    }
}
