using System;
using Api.Database;
using Api.Users;
using Api.AutoForms;


namespace Api.Metrics
{
    /// <summary>
    /// A live metric source is one which is actively listening for updates.
    /// </summary>
    public partial class LiveMetricSource
    {
		/// <summary>
		/// The source meta.
		/// </summary>
		public MetricSource Source;
		
		/// <summary>
		/// The metric that this will be supplying data to.
		/// </summary>
		public Metric Metric;

		/// <summary>
		/// The latest count of triggers in the last 30 second window. Resets to 0 whenever the database is updated.
		/// </summary>
		public int Count;
    }
}
