using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Eventing;
using Api.Contexts;
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
	[LoadPriority(5)]
	public partial class LocaleService : AutoService<Locale>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public LocaleService() : base(Events.Locale)
		{		
			InstallAdminPages("Locales", "fa:fa-globe-europe", new string[] { "id", "name" });

			Cache(new CacheConfig<Locale>() {
				LowFrequencySequentialIds = true,
				OnCacheLoaded = OnCacheLoaded
			});

			var cfg = GetConfig<LocaleServiceConfig>();

			Events.Locale.BeforeCreate.AddEventListener((Context context, Locale locale) =>
			{
				if (string.IsNullOrEmpty(locale.Code))
				{
					throw new PublicException("At least a locale code is required", "locale_code_required");
				}

				return new ValueTask<Locale>(locale);
			});

			Events.Locale.BeforeUpdate.AddEventListener((Context context, Locale locale, Locale original) =>
			{
				if (string.IsNullOrEmpty(locale.Code))
				{
					throw new PublicException("At least a locale code is required", "locale_code_required");
				}

				return new ValueTask<Locale>(locale);
			});

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

						if (langsUpTo == -1)
						{
							var localeId = await GetId(acceptLanguageHeader);

							if (localeId.HasValue)
							{
								context.LocaleId = localeId.Value;
							}
						}
						else
						{

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
				}

				return result;
			});

		}

		/// <summary>
		/// Runs when the cache has loaded.
		/// </summary>
		/// <returns></returns>
		private async ValueTask OnCacheLoaded()
		{
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
				}, DataOptions.IgnorePermissions);
			}
		}

		/// <summary>
		/// Gets locale ID by its case insensitive code. Can contain hyphens, such as "en-gb".
		/// </summary>
		/// <param name="localeCode"></param>
		/// <returns>null if not found.</returns>
		public async ValueTask<uint?> GetId(string localeCode)
		{
			if (_codeMap == null)
			{
				var all = await Where(DataOptions.IgnorePermissions).ListAll(new Context());

				var map = new Dictionary<string, uint>();

				foreach (var locale in all)
				{
					if (!string.IsNullOrWhiteSpace(locale.Code)) {
						// Primary locale code
						map[locale.Code.Trim().ToLower()] = locale.Id;
					}

					if (!string.IsNullOrWhiteSpace(locale.Aliases))
					{
						// Seconadary locale map codes
						var secondaryCodes = locale.Aliases.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
						foreach (var secondaryCode in secondaryCodes)
						{
							map[secondaryCode.Trim().ToLower()] = locale.Id;
						}
					}
				}

				_codeMap = map;
			}

			if (!_codeMap.TryGetValue(localeCode, out uint result))
			{
				return null;
			}

			return result;
		}

		/// <summary>
		/// A mapping of locale code -> ID. Uses IDs such that it does not need locale specific variations.
		/// </summary>
		private Dictionary<string, uint> _codeMap;

		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		public string CookieName => "Locale";
	}
    
}
