using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using System;
using System.Collections.Concurrent;

namespace Api.Matchmakers
{
	/// <summary>
	/// Handles matchServers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MatchServerService : AutoService<MatchServer>
    {
		private ConcurrentDictionary<uint, MatchServer> matchServerLookup;
		private Random randomiser = new Random();
		private ConcurrentDictionary<uint, List<MatchServer>> matchServerRegionalLookup = new ConcurrentDictionary<uint, List<MatchServer>>();

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MatchServerService() : base(Events.MatchServer)
        {
			InstallAdminPages("Match Servers", "fa:fa-users", new string[] { "id", "address" });
			
			Cache(new CacheConfig<MatchServer>(){
				Retain = true,
				Preload = true,
				OnCacheLoaded = () => {
					// The cache ID index is a math server lookup.
					// That'll be useful when allocating a server.
					matchServerLookup = GetCacheForLocale(1).GetPrimary();
					foreach (var ms in matchServerLookup.Values)
					{
						if(!matchServerRegionalLookup.TryGetValue(ms.RegionId, out List<MatchServer> region))
						{
							region = new List<MatchServer>();
							matchServerRegionalLookup[ms.RegionId] = region;
						}

						region.Add(ms);
					}

					return new ValueTask();
				}
			});
		}

		/// <summary>
		/// Allocates a match server for the given matchmaker, primarily considering region.
		/// </summary>
		public MatchServer Allocate(Context context, Matchmaker matchmaker)
		{
			if (!matchServerRegionalLookup.TryGetValue(matchmaker.RegionId, out List<MatchServer> regionSet))
			{
				return null;
			}

			// Basic rng for now. Will need ability to remove a 
			// server from the available pool when a match is allocated to it for heavier activities.
			var index = randomiser.Next(regionSet.Count);
			return regionSet[index];
		}

	}

}
