using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Matchmakers
{
	/// <summary>
	/// Handles matchmakers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MatchmakerService : AutoService<Matchmaker>
    {
		// private Dictionary<int, Matchmaker> matchmakerLookup;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MatchmakerService() : base(Events.Matchmaker)
        {
			// Example admin page install:
			InstallAdminPages("Matchmakers", "fa:fa-rocket", new string[] { "id", "name" });
			
			Cache(new CacheConfig<Matchmaker>(){
				Retain = true,
				Preload = true,
				OnCacheLoaded = () => {
					// The cache ID index is a matchmaker lookup.
					// matchmakerLookup = GetCacheForLocale(1).GetPrimary();

					return new ValueTask();
				}
			});
		}
	}
    
}
