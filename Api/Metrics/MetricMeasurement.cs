using System;
using Api.Database;
using Api.Users;

namespace Api.Metrics
{
    /// <summary>
    /// Metrics measurements are used to measure the number of times a metric source was triggered in 15 minute blocks. 
    /// </summary>
    [DatabaseField(AutoIncrement = false)]
    public partial class MetricMeasurement : Content<uint>
    {
        /// <summary>
        /// The count of triggers in this 15 minute block.
        /// </summary>
        public int Count; 
		
		/// <summary>
		/// The source this came from.
		/// </summary>
		public uint SourceId;
		
		/// <summary>
		/// The metric this belongs to.
		/// </summary>
		public uint MetricId;
    }
}
