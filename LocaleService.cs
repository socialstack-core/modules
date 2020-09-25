using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.DatabaseDiff;
using System;
using Api.Startup;

namespace Api.Translate
{
	/// <summary>
	/// Handles locales - the core of the translation (localisation) system.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(2)]
	public partial class LocaleService : AutoService<Locale>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LocaleService() : base(Events.Locale)
		{		
			InstallAdminPages("Locales", "fa:fa-globe-europe", new string[] { "id", "name" });

			Cache(new CacheConfig<Locale>() {
				OnCacheLoaded = async () => {

					// Get the default cache:
					var defaultCache = GetCacheForLocale(1);

					// Does it have anything in it?
					if (defaultCache.Count() == 0)
					{
						// Create the default locale now:
						await Create(new Context(), new Locale()
						{
							Code = "en",
							Name = "English",
							Id = 1
						});
					}

				}
			});
		}

		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		public string CookieName => "Locale";
	}
    
}
