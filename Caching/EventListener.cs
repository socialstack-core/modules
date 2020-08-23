using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Startup;
using Api.Companies;
using System;
using System.Threading.Tasks;
using Api.Permissions;
using Api.Videos;
using Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace Api.Startup
{

	/// <summary>
	/// Listens out for service start and then sets up any manual caching config in the appsettings.json.
	/// </summary>
	[EventListener]
	public class CacheSetupEventListener
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public CacheSetupEventListener()
		{
			Events.ServicesAfterStart.AddEventListener(async (Context ctx, object src) =>
			{
				// Get cfg from appsettings:
				var cfg = AppSettings.GetSection("Caching").Get<SiteCacheConfig>();
				
				if(cfg != null && cfg.Services != null)
				{
					// Caching config section declares additional core services which should also have caching turned on.
					
					foreach(var kvp in cfg.Services)
					{
						var autoSvc = Services.Get(kvp.Key) as AutoService;
						
						if(autoSvc == null)
						{
							Console.WriteLine("[WARN] A service called '" + kvp.Key + "' is in your Caching config in your appsettings, but it doesn't exist in this project.");
							continue;
						}
						
						// Turn on caching now:
						await autoSvc.SetupCacheNow(kvp.Value);
					}
					
				}
				
				return src;
			}, 50);
		}
	}
}