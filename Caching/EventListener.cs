using Api.Contexts;
using Api.Database;
using Api.DatabaseDiff;
using Api.Eventing;
using Api.Startup;
using System;
using System.Threading.Tasks;
using Api.Permissions;
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
			SiteCacheConfig scc = null;

			// Caching config section declares additional core services which should also have caching turned on.
			// This handler reads that config section and applies its outcomes.

			Events.Service.BeforeCreate.AddEventListener((Context ctx, AutoService svc) =>
			{
				// Get cfg from appsettings:
				if (scc == null)
				{
					scc = AppSettings.GetSection("Caching").Get<SiteCacheConfig>();

					if (scc == null)
					{
						scc = new SiteCacheConfig();
					}
				}

				if(scc.Services != null)
				{
					if (scc.Services.TryGetValue(svc.GetType().Name, out CacheConfig cfg))
					{
						// Apply cache config:
						svc.Cache(cfg);
					}
				}

				return new ValueTask<AutoService>(svc);
			}, 2);
		}
	}
}