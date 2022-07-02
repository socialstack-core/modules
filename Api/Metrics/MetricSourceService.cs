using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Api.Eventing;

namespace Api.Metrics
{
    /// <summary>
    /// Handles Metrics.
    /// Instanced automatically. Use Injection to use this service, or Startup.Services.Get. 
    /// </summary>
    public partial class MetricSourceService : AutoService<MetricSource>
    {
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MetricSourceService() : base(Events.MetricSource) {

			InstallAdminPages(new string[] { "id", "eventName" });

		}

	}
}
