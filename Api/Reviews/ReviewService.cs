using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Reviews
{
	/// <summary>
	/// Handles reviews.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ReviewService : AutoService<Review>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ReviewService() : base(Events.Review)
        {
			InstallAdminPages("Reviews", "fa:fa-rocket", new string[] { "id", "tripName", "tripDate", "clientName" });
#if !DEBUG
			Cache();
#endif
		}
	}
    
}
