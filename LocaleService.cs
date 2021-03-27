using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
using Api.DatabaseDiff;
using System;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Linq;

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

			var cfg = GetConfig<LocaleServiceConfig>();

			Events.ContextAfterAnonymous.AddEventListener(async (Context context, Context result, HttpRequest request) =>
			{
				if (cfg.HandleAcceptLanguageHeader && result != null)
				{
					// Identify most suitable locale from the accept-lang header.
					StringValues acceptLangs;

					// Could also handle Accept-Language here. For now we use a custom header called Locale (an ID).
					if (request.Headers.TryGetValue("Accept-Language", out acceptLangs) && !string.IsNullOrEmpty(acceptLangs))
					{
						// Locale header is set:
						var acceptLanguageHeader = acceptLangs.FirstOrDefault();

						var langsUpTo = acceptLanguageHeader.IndexOf(';');
						var index = 0;

						while (index < langsUpTo)
						{
							var next = acceptLanguageHeader.IndexOf(',', index + 1);

							if (next > langsUpTo)
							{
								break;
							}

							if (next == -1 || next > langsUpTo)
							{
								next = langsUpTo;
							}

							string langCode = acceptLanguageHeader.Substring(index, next - index);

							var localeId = await GetId(langCode);

							if (localeId.HasValue)
							{
								context.LocaleId = localeId.Value;
								break;
							}

							index = next + 1;
						}

					}
				}

				return result;
			});

		}

		/// <summary>
		/// Gets locale ID by its case insensitive code. Can contain hyphens, such as "en-gb".
		/// </summary>
		/// <param name="localeCode"></param>
		/// <returns>null if not found.</returns>
		public async ValueTask<int?> GetId(string localeCode)
		{
			if (_codeMap == null)
			{
				var all = await List(new Context(), new Filter<Locale>(), DataOptions.IgnorePermissions);

				var map = new Dictionary<string, int>();

				foreach (var locale in all)
				{
					if (string.IsNullOrEmpty(locale.Code))
					{
						continue;
					}

					// Primary locale code (may have an aliases field in the future):
					var mainCode = locale.Code.Trim().ToLower();
					map[mainCode] = locale.Id;
				}

				_codeMap = map;
			}

			if (!_codeMap.TryGetValue(localeCode, out int result))
			{
				return null;
			}

			return result;
		}

		/// <summary>
		/// A mapping of locale code -> ID. Uses IDs such that it does not need locale specific variations.
		/// </summary>
		private Dictionary<string, int> _codeMap;

		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		public string CookieName => "Locale";
	}
    
}
