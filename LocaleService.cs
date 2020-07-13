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
	public partial class LocaleService : AutoService<Locale>, ILocaleService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LocaleService() : base(Events.Locale)
		{		
			InstallAdminPages("Locales", "fa:fa-globe-europe", new string[] { "id", "name" });

			Cache();
		}

		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		public string CookieName => "Locale";
	}
    
}
