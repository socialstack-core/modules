using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Translate
{
	/// <summary>
	/// Handles translations.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class TranslationService : AutoService<Translation>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TranslationService() : base(Events.Translation)
        {
			// Always cache by default:
			Cache();
			
			// The above is such that translations are always content-synced, 
			// which is important as they are internally cached in a variety of ways.
			
			// Example admin page install:
			InstallAdminPages(null, null, new string[] { "id", "module", "original", "translation" });
		}
	}
    
}
