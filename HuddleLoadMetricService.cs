using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Huddles
{
	/// <summary>
	/// Handles huddleLoadMetrics.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class HuddleLoadMetricService : AutoService<HuddleLoadMetric>, IHuddleLoadMetricService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public HuddleLoadMetricService() : base(Events.HuddleLoadMetric)
        {
			// Example admin page install:
			// InstallAdminPages("HuddleLoadMetrics", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
