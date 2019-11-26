using System;
using Api.Database;
using Api.Users;

namespace Api.Metrics
{
    /// <summary>
    /// Metrics are used to measure the number of notifications created in 15 minute blocks. 
    /// </summary>
    [DatabaseField(AutoIncrement = false)]
    public partial class Metric : RevisionRow
    {
        /// <summary>
        /// The count of notification sent in this 15 minute block.
        /// </summary>
        public int Count; 
    }
}
