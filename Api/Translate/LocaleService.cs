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

			Events.Locale.AfterUpdate.AddEventListener(async (Context context, Locale locale) => {
				await UpdateMaps();
				return locale;
			});

			Events.Locale.AfterCreate.AddEventListener(async (Context context, Locale locale) => {
				await UpdateMaps();
				return locale;
			});

			Events.Page.BeforeParseUrl.AddEventListener(async (Context context, Pages.UrlInfo url, Microsoft.AspNetCore.Http.QueryString query) => {

				// Does the locale have a page prefix?
				// If so, apply it now.
				var locale = await context.GetLocale();
				if (!string.IsNullOrEmpty(locale.PagePath))
				{
					// Act like this pagePath is in the URL all the time.
					var strippedUrl = url.AllocateString();

					if (string.IsNullOrEmpty(strippedUrl))
					{
						url.Url = locale.PagePath;
					}
					else
					{
						if (strippedUrl.StartsWith("en-admin"))
						{
							// No changes
							return url;
						}

						url.Url = locale.PagePath + "/" + strippedUrl;
					}

					url.Start = 0;
					url.Length = url.Url.Length;
				}

				return url;

			});

			Events.ContextAfterAnonymous.AddEventListener((Context context, Context result, HttpRequest request) =>
			{
				if (_multiDomain)
				{
					// Future feature: a domain can potentially signal >1 language.
					// I.e. handle domain and Accept-Language simultaneously.
					// Helloworld.ch - Switzerland - can be Swiss French, German etc.
					var host = request.Host.Value;

					// Get by site locale ID:
					var localeId = GetByDomain(host);

					if (localeId.HasValue && localeId.Value != context.LocaleId)
					{
						context.LocaleId = localeId.Value;
					}
				}
				else if (cfg.HandleCloudFlareHeader && result != null)
				{
					// Identify most suitable locale from headers
					StringValues ipCountry;

					// check for CloudFlare (NB: NOT CloudFront!) header
					if (request.Headers.TryGetValue("CF-IPCountry", out ipCountry) && !string.IsNullOrEmpty(ipCountry))
					{
						// will be a 2-character country code (e.g. "US")
						var ipCountryHeader = ipCountry.FirstOrDefault();
						ipCountryHeader = ipCountryHeader.ToLower();

						// update context LocaleId if we have a suitable locale available
						UpdateContextLocale(context, ipCountryHeader);
					}
					else if (cfg.HandleAcceptLanguageHeader && result != null)
					{
						StringValues acceptLangs;

						// Identify most suitable locale from the accept-lang header.
						if (request.Headers.TryGetValue("Accept-Language", out acceptLangs) && !string.IsNullOrEmpty(acceptLangs))
						{
							// Locale header is set:
							var acceptLanguageHeader = acceptLangs.FirstOrDefault();
							acceptLanguageHeader = acceptLanguageHeader.ToLower();

							acceptLanguageHeader = acceptLanguageHeader.ToLower();
							var langsUpTo = acceptLanguageHeader.IndexOf(';');
							var index = 0;

							if (langsUpTo == -1)
							{
								var localeId = GetId(acceptLanguageHeader);

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
									var localeId = GetId(langCode);

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

				}

				return new ValueTask<Context>(result);
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

			// Initial map build:
			await UpdateMaps();
		}

		/// <summary>
		/// Gets locale ID by its case insensitive domain and optional port. "www.mysite.co.uk" or "localhost:5050"
		/// </summary>
		/// <param name="domain"></param>
		/// <returns>null if not found.</returns>
		public uint? GetByDomain(string domain)
		{
			if (_domainMap == null || domain == null)
			{
				return null;
			}

			if (!_domainMap.TryGetValue(domain, out uint result))
			{
				return null;
			}

			return result;
		}

		/// <summary>
		/// Gets locale ID by its case insensitive code. Can contain hyphens, such as "en-gb".
		/// </summary>
		/// <param name="localeCode"></param>
		/// <returns>null if not found.</returns>
		public uint? GetId(string localeCode)
		{
			if (_codeMap == null)
			{
				return null;
			}

			if (!_codeMap.TryGetValue(localeCode, out uint result))
			{
				return null;
			}

			return result;
		}

		/// <summary>
		/// A mapping of alpha-2 country code (e.g. "us") -> full country code (e.g. "en-US").
		/// </summary>
		private static readonly Dictionary<string, string> countryToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			{ "ar", "es-AR" },
			{ "au", "en-AU" },
			{ "br", "pt-BR" },
			{ "ca", "en-CA" },
			{ "cl", "es-CL" },
			{ "cn", "zh-CN" },
			{ "co", "es-CO" },
			{ "de", "de-DE" },
			{ "dk", "da-DK" },
			{ "eg", "ar-EG" },
			{ "es", "es-ES" },
			{ "fi", "fi-FI" },
			{ "fr", "fr-FR" },
			{ "gb", "en-GB" },
			{ "gr", "el-GR" },
			{ "id", "id-ID" },
			{ "il", "he-IL" },
			{ "in", "hi-IN" },
			{ "it", "it-IT" },
			{ "jp", "ja-JP" },
			{ "kr", "ko-KR" },
			{ "mx", "es-MX" },
			{ "my", "ms-MY" },
			{ "nl", "nl-NL" },
			{ "no", "nb-NO" },
			{ "pe", "es-PE" },
			{ "pk", "ur-PK" },
			{ "pl", "pl-PL" },
			{ "pt", "pt-PT" },
			{ "ru", "ru-RU" },
			{ "sa", "ar-SA" },
			{ "se", "sv-SE" },
			{ "th", "th-TH" },
			{ "tr", "tr-TR" },
			{ "us", "en-US" },
			{ "ve", "es-VE" },
			{ "vn", "vi-VN" },
			{ "za", "en-ZA" }
		};

		/// <summary>
		/// Updates the given context LocaleId if the alpha-2 country code supplied relates to an available locale.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="countryCode"></param>
		private void UpdateContextLocale(Context context, string countryCode)
		{
			string languageCode;

			// check for alpha-2 country code in dictionary (e.g. "us") and return the matching full country code (e.g. "en-US")
			if (countryToLanguageMap.TryGetValue(countryCode.ToLower(), out languageCode))
			{
				var localeId = GetId(languageCode);

				if (localeId.HasValue)
				{
					context.LocaleId = localeId.Value;
				}
			}
		}

		private async ValueTask UpdateMaps()
		{
			var all = await Where(DataOptions.IgnorePermissions).ListAll(new Context());

			var cm = new Dictionary<string, uint>();

			foreach (var locale in all)
			{
				if (!string.IsNullOrWhiteSpace(locale.Code))
				{
					// Primary locale code
					cm[locale.Code.Trim().ToLower()] = locale.Id;
				}

				if (!string.IsNullOrWhiteSpace(locale.Aliases))
				{
					// Seconadary locale map codes
					var secondaryCodes = locale.Aliases.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var secondaryCode in secondaryCodes)
					{
						cm[secondaryCode.Trim().ToLower()] = locale.Id;
					}
				}
			}

			_codeMap = cm;

			// Domain maps next. Domain map can be null if it is not a multi domain site.
			var domainMap = new Dictionary<string, uint>();

			foreach (var locale in all)
			{
				if (!string.IsNullOrWhiteSpace(locale.Domains))
				{
					var domains = locale.Domains.Trim().Split(',');

					foreach (var domainToAdd in domains)
					{
						domainMap[domainToAdd] = locale.Id;
					}
				}
			}

			if (domainMap.Count == 0)
			{
				_domainMap = null;
				_multiDomain = false;
			}
			else
			{
				_multiDomain = true;
				_domainMap = domainMap;
			}

		}
		
		/// <summary>
		/// A mapping of locale code -> ID. Uses IDs such that it does not need locale specific variations.
		/// </summary>
		private Dictionary<string, uint> _codeMap;

		/// <summary>
		/// A mapping of domain -> ID. Uses IDs such that it does not need locale specific variations.
		/// </summary>
		private Dictionary<string, uint> _domainMap;

		/// <summary>
		/// True if the domains field is used on locales.
		/// </summary>
		private bool _multiDomain;

		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		public string CookieName => "Locale";
	}
    
}
