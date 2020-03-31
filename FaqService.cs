using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;

namespace Api.Faqs
{
	/// <summary>
	/// Handles articles - containers for individual article posts.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class FaqService : AutoService<Faq>, IFaqService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public FaqService() : base(Events.Faq) {
			
			InstallAdminPages("Faqs", "fa:fa-question-circle", new string[] { "id", "name" });
			
		}
	
	}
    
}
