using System.Threading.Tasks;
using Api.Configuration;
using System;
using System.IO;
using Api.Contexts;
using System.Collections.Generic;
using Api.Eventing;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.CanvasRenderer;
using Api.Translate;
using Api.Database;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Http;
using Api.Themes;
using Api.Startup;
using System.Reflection;
using System.Linq;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Microsoft.AspNetCore.Authentication;

namespace Api.Pages
{
	/// <summary>
	/// Handles the main generation of HTML from the index.html base template at UI/public/index.html and Admin/public/index.html
	/// </summary>
	[HostType("web")]
	public partial class HtmlService : AutoService
	{
		private readonly PageService _pages;
		private readonly CanvasRendererService _canvasRendererService;
		private readonly ConfigSet<HtmlServiceConfig> _configSet;
		private readonly FrontendCodeService _frontend;
		private readonly ContextService _contextService;
		private readonly ThemeService _themeService;
		private readonly LocaleService _localeService;
		private readonly ConfigurationService _configurationService;
		private string _cacheControlHeader;
		private List<Locale> _allLocales;
		private string _siteDomains;
		private string _siteDomainPrefixes;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public HtmlService(PageService pages, CanvasRendererService canvasRendererService, FrontendCodeService frontend, ContextService ctxService,
				LocaleService localeService, ConfigurationService configurationService, ThemeService themeService)
		{
			_pages = pages;
			_frontend = frontend;
			_canvasRendererService = canvasRendererService;
			_contextService = ctxService;
			_localeService = localeService;
			_configurationService = configurationService;
			_themeService = themeService;

			_configSet = GetAllConfig<HtmlServiceConfig>();

			var pathToUIDir = AppSettings.GetString("UI");

			if (string.IsNullOrEmpty(pathToUIDir))
			{
				pathToUIDir = "UI/public";
			}

			_configSet.OnChange += () =>
			{
				BuildConfigLocaleTable();
				return new ValueTask();
			};

			BuildConfigLocaleTable();
		}

		/*
		/// <summary>
		/// Generates information about the HTML cache. Result object is JSON serialisable via newtonsoft.
		/// </summary>
		public HtmlCacheStatus GetCacheStatus()
		{
			var result = new HtmlCacheStatus();

			// Local ref to the cache object, just in case it is cleared whilst we are running.
			var c = cache;

			if (c == null)
			{
				// Empty cache.
				return result;
			}

			result.Locales = new List<HtmlCachedLocaleStatus>();

			for (var i = 0; i < c.Length; i++)
			{
				var localeEntry = c[i];

				if (localeEntry == null)
				{
					continue;
				}

				var localeStatus = new HtmlCachedLocaleStatus();
				result.Locales.Add(localeStatus);
				localeStatus.LocaleId = i + 1;
				localeStatus.CachedPages = new List<HtmlCachedPageStatus>();

				foreach (var kvp in localeEntry)
				{
					var pageInfo = new HtmlCachedPageStatus();
					pageInfo.Url = kvp.Key;
					pageInfo.AnonymousDataSize = kvp.Value.AnonymousCompressedPage == null ? null : kvp.Value.AnonymousCompressedPage.Length;
					pageInfo.NodeCount = kvp.Value.Nodes == null ? null : kvp.Value.Nodes.Count;
					pageInfo.AnonymousStateSize = kvp.Value.AnonymousCompressedState == null ? null : kvp.Value.AnonymousCompressedState.Length;
					localeStatus.CachedPages.Add(pageInfo);
				}

			}

			return result;
		}
		*/

		/// <summary>
		/// The frontend version.
		/// </summary>
		/// <returns></returns>
		public long Version
		{
			get
			{
				return _frontend.Version;
			}
		}

		private void BuildConfigLocaleTable()
		{
			_robots = null;

			if (_configSet == null || _configSet.Configurations == null || _configSet.Configurations.Count == 0)
			{
				// Not configured at all.
				_configurationTable = new HtmlServiceConfig[0];
				_defaultConfig = new HtmlServiceConfig();
				return;
			}

			// First collect highest locale ID.
			uint highest = 0;
			uint lowest = uint.MaxValue;

			foreach (var config in _configSet.Configurations)
			{
				if (config == null)
				{
					continue;
				}

				if (config.LocaleId > highest)
				{
					highest = config.LocaleId;
				}
				else if (config.LocaleId < lowest)
				{
					lowest = config.LocaleId;
				}
			}

			if (lowest == uint.MaxValue)
			{
				// Not configured at all.
				_configurationTable = new HtmlServiceConfig[0];
				_defaultConfig = new HtmlServiceConfig();
				return;
			}

			var ct = new HtmlServiceConfig[highest + 1];

			// Slot them:
			foreach (var config in _configSet.Configurations)
			{
				if (config == null)
				{
					continue;
				}

				ct[config.LocaleId] = config;
			}

			// Fill any gaps with the default entry. The default simply has the lowest ID (ideally 0 or 1).
			var defaultEntry = ct[lowest];

			for (var i = 0; i < ct.Length; i++)
			{
				if (ct[i] == null)
				{
					ct[i] = defaultEntry;
				}
			}

			_defaultConfig = defaultEntry;
			_configurationTable = ct;
			_cacheControlHeader = "public, max-age=" + defaultEntry.CacheMaxAge.ToString();
		}

		/// <summary>
		/// Configs indexed by locale.
		/// This set is fully populated: It has no nulls. If a slot is null for a given locale ID, it used the entry in slot 1. 
		/// If slot 1 was also null, it used the entry for slot 0. However if a locale is beyond the end of the set, use slot 0.
		/// </summary>
		private HtmlServiceConfig[] _configurationTable = new HtmlServiceConfig[0];
		private HtmlServiceConfig _defaultConfig = new HtmlServiceConfig();

		/// <summary>
		/// robots.txt
		/// </summary>
		private byte[] _robots;

		/// <summary>
		/// Gets robots.txt as a byte[].
		/// </summary>
		/// <returns></returns>
		public byte[] GetRobotsTxt(Context context)
		{
			var config = (context.LocaleId < _configurationTable.Length) ? _configurationTable[context.LocaleId] : _defaultConfig;

			if (_robots == null)
			{
				var sb = new StringBuilder();
				sb.Append("User-agent: *\r\n");
				// sb.Append("Disallow: /v1\r\n");
				// sb.Append("Sitemap: /sitemap.xml");

				if (config != null && config.RobotsTxt != null)
				{
					foreach (string line in config.RobotsTxt)
					{
						sb.Append(line);
						sb.Append("\r\n");
					}
				}

				sb.Append("\r\n");

				_robots = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
			}

			return _robots;
		}

		private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Renders the given page and token set as state only.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="response"></param>
		/// <param name="pageAndTokens"></param>
		/// <param name="exactUrl"></param>
		/// <returns></returns>
		public async ValueTask RenderState(Context context, HttpResponse response, PageWithTokens pageAndTokens, string exactUrl)
		{
			var writer = Writer.GetPooled();
			writer.Start(null);
			await BuildState(context, writer, pageAndTokens, exactUrl);
			await writer.CopyToAsync(response.Body);
			writer.Release();
		}

		/// <summary>
		/// Renders the state only of a page to a JSON string in the given writer.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pageAndTokens"></param>
		/// <param name="writer"></param>
		/// <param name="exactUrl"></param>
		/// <returns></returns>
		public async ValueTask BuildState(Context context, Writer writer, PageWithTokens pageAndTokens, string exactUrl)
		{
			var terminal = pageAndTokens.PageTerminal;
			var page = terminal.Page;

			var isAdmin = terminal.IsAdmin;

			object primaryObject = pageAndTokens.PrimaryObject;
			AutoService primaryService = pageAndTokens.PrimaryService;

			writer.WriteASCII("{\"url\":");
			writer.WriteEscaped(exactUrl);
			writer.Write((byte)',');
			writer.WriteS(GetAvailableDomains());
			writer.WriteASCII("\"page\":{\"bodyJson\":");

			if (terminal.Generator == null)
			{
				writer.WriteS(page.BodyJson.Get(context).ValueOf());
			}
			else
			{
				// Execute canvas graphs:
				await terminal.Generator.Generate(context, writer, pageAndTokens);
			}

			writer.WriteASCII(",\"title\":\"");
			writer.WriteS(page.Title.Get(context));
			writer.WriteASCII("\",\"id\":");
			writer.WriteS(page.Id);
			if (!string.IsNullOrEmpty(page.PrimaryContentIncludes))
			{
				writer.WriteASCII(",\"primaryContentIncludes\":");
				writer.WriteEscaped(page.PrimaryContentIncludes);
			}
			writer.Write((byte)'}');

			var cfgBytes = _configurationService.GetLatestFrontendConfigBytesJson();

			if (cfgBytes != null)
			{
				writer.WriteASCII(",\"config\":");
				writer.WriteNoLength(cfgBytes);
			}

			if (terminal.TokenNamesJson != null)
			{
				writer.WriteASCII(",\"tokenNames\":");
				writer.WriteS(terminal.TokenNamesJson);
			}

			if (pageAndTokens.TokenValues != null)
			{
				writer.WriteASCII(",\"tokens\":[");

				for (var i = 0; i < pageAndTokens.TokenValues.Count; i++)
				{
					if (i != 0)
					{
						writer.Write((byte)',');
					}

					writer.WriteEscaped(pageAndTokens.TokenValues[i]);
				}

				writer.WriteASCII("]");
			}
			else
			{
				writer.WriteASCII(",\"tokens\":null");
			}

			if (primaryObject != null)
			{
				writer.WriteASCII(",\"po\":");
				await primaryService.ObjectToJson(context, primaryObject, writer, null, page.PrimaryContentIncludes, ContextFlags.IsPrimary | ContextFlags.IsPage);
			}

			var titleStr = page.Title.Get(context);

			if (!string.IsNullOrEmpty(titleStr))
			{
				writer.WriteASCII(",\"title\":");

				if (primaryObject != null)
				{
					writer.WriteEscaped(await ReplaceTokens(context, titleStr, primaryObject));
				}
				else
				{
					writer.WriteEscaped(titleStr);
				}
			}

			var descriptionStr = page.Description.Get(context);

			if (page != null && !string.IsNullOrEmpty(descriptionStr))
			{
				writer.WriteASCII(",\"description\":");

				if (primaryObject != null)
				{
					writer.WriteEscaped(await ReplaceTokens(context, descriptionStr, primaryObject));
				}
				else
				{
					writer.WriteEscaped(descriptionStr);
				}
			}

			writer.Write((byte)'}');
		}

		/// <summary>
		/// Generated block page (it's always the same).
		/// </summary>
		private Writer _blockPage;

		/// <summary>
		/// Typically only on stage. It's the same every time.
		/// </summary>
		/// <returns></returns>
		private void RenderBlockPage(Writer targetWriter)
		{
			if (_blockPage == null)
			{
				_blockPage = CreateBlockPage();
			}

			_blockPage.CopyTo(targetWriter);
		}

		private Writer CreateBlockPage()
		{
			var writer = Writer.GetPooled();
			writer.Start(null);
			var blockPageBytes = File.ReadAllBytes("UI/public/block.html");
			writer.Write(blockPageBytes, 0, blockPageBytes.Length);
			return writer;
		}

		/// <summary>
		/// Only on development.
		/// </summary>
		/// <param name="errors"></param>
		/// <param name="writer"></param>
		/// <returns></returns>
		private void GenerateErrorPage(List<UIBuildError> errors, Writer writer)
		{
			// Your UI has bad syntax, but somebody might as well at least get a smile out of it :p
			var messages = new string[] {
				"I burnt the pastries.",
				"I burnt the pizzas.",
				"I burnt the cake again :(",
				"I burnt the chips.",
				"I burnt the microwaveable dinner somehow.",
				"I burnt the carpet.",
				"Instructions unclear, fork wedged in ceiling.",
				"Your pet ate all the food whilst you were away, but it wasn't my fault I swear.",
				"Have you tried turning it off, then off, then back on and off again?",
				"Maybe the internet got deleted?",
				"Blame Mike.",
				"Contact your system admin. If you are the system admin, I'm so sorry.",
				"You shall not pass!",
				"Ruh-roh Rorge!",
				"I'm not sure what happened, but I think I might have eaten the source code."
			};

			var rng = new Random();
			var title = "Oops! Something has gone very wrong. " + messages[rng.Next(0, messages.Length)];

			writer.WriteASCII("<!doctype html><html><head>");
			writer.WriteASCII("<meta charset=\"utf-8\" />");
			writer.WriteASCII("<title>");
			writer.WriteS(title);
			writer.WriteASCII("</title>");

			// Fail in style:
			writer.WriteASCII("<link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css\" />");
			writer.WriteASCII(@"<style>
				.callout {padding: 20px;margin: 20px 0;border: 1px solid #eee;border-left-width: 5px;border-radius: 3px;}
				.callout h4 {margin-top: 0; margin-bottom: 5px;}
				.callout p:last-child {margin-bottom: 0;}
				.callout pre {border-radius: 3px;color: #e83e8c;}
				.callout + .bs-callout {margin-top: -5px;}
				.callout-danger {border-left-color: #d9534f;}.callout-danger h4 {color: #d9534f;}
				.callout-bdc {border-left-color: #29527a;}
				.callout-bdc h4 {color: #29527a;}
				.alert-danger{margin-top: 20px;}
				@media(prefers-color-scheme: dark){
					.callout{border-color: #333}
					body{color: white;background:#222}
				}
				</style>");

			writer.WriteASCII("</head><body>");
			writer.WriteASCII("<div class=\"container\">");
			writer.WriteASCII("<div class=\"alert alert-danger\" role=\"alert\">");
			writer.WriteASCII("<b>" + errors.Count + " error(s)</b> during UI build.");
			writer.WriteASCII("</div>");

			foreach (var error in errors)
			{
				writer.WriteASCII("<div class=\"callout callout-danger\"><h4>");
				writer.WriteS(error.Title);
				writer.WriteASCII("</h4><p>");
				writer.WriteS(error.File);
				writer.WriteASCII("</p><pre>");
				writer.WriteS(HttpUtility.HtmlEncode(error.Description));
				writer.WriteASCII("</pre></div>");
			}

			writer.WriteASCII("</div></body></html>");
		}

		/// <summary>
		/// Only renders the head. The body is blank.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="writer"></param>
		/// <param name="pageWithTokens"></param>
		/// <returns></returns>
		private async ValueTask BuildHeader(Context context, Writer writer, PageWithTokens pageWithTokens)
		{
			var terminal = pageWithTokens.PageTerminal;
			var isAdmin = terminal == null ? true : terminal.IsAdmin;
			var page = terminal == null ? null : terminal.Page;
			var locale = await context.GetLocale();

			var _config = (locale.Id < _configurationTable.Length) ? _configurationTable[locale.Id] : _defaultConfig;

			writer.WriteASCII("<head>");

			// Charset must be within first 1kb of the header:
			writer.WriteASCII("<meta charset='utf-8' />");

			// NB: commented out as this currently relies on the "core-test-theme" storage key and associated styling within ui2025 branch;
			//     unknown if / when theme support will be revisited in core branch
			/*
			// check for overriding user theme preference - do it early as possible here to prevent a potential flash of the inverse colour
			head.AppendChild(new DocumentNode("script").AppendChild(new TextNode(
				@"
  (function() {
    try {
      const theme = JSON.parse(window.localStorage.getItem('core-test-theme'));
      if (theme === 'light') {
        document.documentElement.classList.add('light-mode');
      }
      if (theme === 'dark') {
        document.documentElement.classList.add('dark-mode');
      }
    } catch (e) {}
  })();
			"
			)));
			 */

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, writer);

			// Handle all Start Head Scripts in the config.
			HandleCustomScriptList(_config.StartHeadScripts, writer);

			if (_config.EnableCanonicalTag && page != null)
			{
				// todo!
				var canonicalPath = "";

				if (canonicalPath == "/")
				{
					canonicalPath = "";
				}

				var canonicalUrl = UrlCombine(_frontend.GetPublicUrl(locale.Id), canonicalPath)?.ToLower();

				writer.WriteASCII("<link rel=\"canonical\" href=");
				writer.WriteEscaped(canonicalUrl);
				writer.WriteASCII(" />");

				if (_config.EnableHrefLangTags)
				{
					// include x-default alternate
					var defaultUrl = GetPathWithoutLocale(canonicalUrl);

					writer.WriteASCII("<link rel=\"alternate\" hreflang=\"x-default\" href=");
					writer.WriteEscaped(defaultUrl);
					writer.WriteASCII(" />");

					var locales = GetAllLocales(context);

					// include alternates for each available locale
					if (locales != null && locales.Count > 0)
					{
						foreach (var altLocale in locales)
						{
							// NB: locale with ID=1 is assumed to be the primary locale
							if (_config.RedirectPrimaryLocale && altLocale.Id == 1)
							{
								continue;
							}

							var altUrl = GetLocaleUrl(altLocale, defaultUrl)?.ToLower();

							writer.WriteASCII("<link rel=\"alternate\" hreflang=\"");
							writer.WriteASCII(altLocale.Code);
							writer.WriteASCII("\" href=");
							writer.WriteEscaped(altUrl);
							writer.WriteASCII(" />");
						}

					}

				}
			}

			// favicon
			var faviconFolder = "";
			var sitePrefixes = GetAvailableDomainPrefixes();

			// allows for different favicons to be referenced for a site with multiple domains
			if (!string.IsNullOrEmpty(sitePrefixes))
			{
				string[] prefixes = sitePrefixes.Split(',');

				foreach (var prefix in prefixes)
				{
					if (page.Url == $"/{prefix}")
					{
						faviconFolder = page.Url;
					}
				}
			}

			writer.WriteASCII($"<link rel=\"icon\" sizes=\"any\" href=\"{faviconFolder}/favicon.ico\" />");
			writer.WriteASCII($"<link rel=\"icon\" type=\"image/svg+xml\" href=\"{faviconFolder}/favicon.svg\" />");
			writer.WriteASCII($"<link rel=\"apple-touch-icon\" href=\"{faviconFolder}/apple-touch-icon.png\" />");

			// Get the main CSS files. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainCssFile = await _frontend.GetMainCss(context == null ? 1 : context.LocaleId);

			writer.WriteASCII("<link rel=\"stylesheet\" href=\"");
			writer.WriteS(_config.FullyQualifyUrls ? mainCssFile.FqPublicUrl : mainCssFile.PublicUrl);
			writer.WriteASCII("\" />");

			if (isAdmin)
			{
				var mainAdminCssFile = await _frontend.GetAdminMainCss(context == null ? 1 : context.LocaleId);

				writer.WriteASCII("<link rel=\"stylesheet\" href=\"");
				writer.WriteS(_config.FullyQualifyUrls ? mainAdminCssFile.FqPublicUrl : mainAdminCssFile.PublicUrl);
				writer.WriteASCII("\" />");
			}

			var pageTitle = page == null ? null : page.Title.Get(context);
			var pageDescription = page == null ? null : page.Description.Get(context);

			if (pageWithTokens.PrimaryObject != null)
			{
				pageTitle = await ReplaceTokens(context, pageTitle, pageWithTokens.PrimaryObject);
				pageDescription = await ReplaceTokens(context, pageDescription, pageWithTokens.PrimaryObject);
			}

			writer.WriteASCII("<meta name=\"msapplication-TileColor\" content=\"#ffffff\" />");
			writer.WriteASCII("<meta name=\"theme-color\" content=\"#ffffff\" />");
			writer.WriteASCII("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
			writer.WriteASCII("<meta name=\"description\" content=");
			if (pageDescription == null)
			{
				writer.WriteASCII("\"\"");
			}
			else
			{
				writer.WriteEscaped(pageDescription);
			}
			writer.WriteASCII(" /><title>");
			if (pageTitle != null)
			{
				writer.WriteS(pageTitle);
			}
			writer.WriteASCII("</title>");

			if (page != null && (!page.CanIndex || page.NoFollow))
			{
				writer.WriteASCII("<meta name='robots' content='");

				if (!page.CanIndex)
				{
					writer.WriteASCII("noindex");
				}

				if (page.NoFollow)
				{
					if (!page.CanIndex)
					{
						writer.WriteASCII(",nofollow");
					}
					else
					{
						writer.WriteASCII("nofollow");
					}
				}

				writer.WriteASCII("' />");
			}

			/*
			 * PWA headers that should only be added if PWA mode is turned on and these files exist
			 * writer.WriteASCII("<link rel=\"apple-touch-icon\" sizes=\"180x180\" href=\"/apple-touch-icon.png\" />");
			 * writer.WriteASCII("<link rel=\"manifest\" href=\"/site.webmanifest\" />");
			 * writer.WriteASCII("<link rel=\"mask-icon\" href=\"/safari-pinned-tab.svg\" color=\"#ffffff\"/>");
			 */

#if DEBUG
			// inject dev-specific classes
			writer.WriteASCII(@"<style type=""text/css"">
			a:not([href]), a[href=""""] {
				outline: 8px solid red;
			}
			</style>");
#endif

			// Handle all End Head tags in the config.
			HandleCustomHeadList(_config.EndHeadTags, writer);

			// Handle all End Head Scripts in the config.
			HandleCustomScriptList(_config.EndHeadScripts, writer);

			// Any custom head bits:
			await Events.Page.OnWriteHeadEnd.Dispatch(context, writer, pageWithTokens);

			writer.WriteASCII("</head>");
		}

		/// <summary>
		/// The config json, if there is any.
		/// </summary>
		private byte[] _configJson = Array.Empty<byte>();

		/// <summary>
		/// </summary>
		/// <param name="context"></param>
		/// <param name="writer"></param>
		/// <param name="pageWithTokens"></param>
		/// <param name="exactUrl">The URL as it appeared in the request (not rewritten etc and includes any query string)</param>
		/// <param name="preRender">Optionally override if SSR should execute.</param>
		/// <returns></returns>
		private async ValueTask<bool> RenderPage(Context context, Writer writer, PageWithTokens pageWithTokens, string exactUrl, bool? preRender = null)
		{
			var terminal = pageWithTokens.PageTerminal;

			if (terminal == null)
			{
				return false;
			}

			var page = terminal.Page;

			if (page == null)
			{
				return false;
			}

			var isAdmin = terminal.IsAdmin;

			var latestConfigBytes = _configurationService.GetLatestFrontendConfigBytes();

			if (latestConfigBytes != _configJson)
			{
				// Note: this happens to also force theme css to be reobtained as well.
				_configJson = latestConfigBytes;
			}

			var themeConfig = _themeService.GetConfig();

#if DEBUG
			// Get the errors from the last build. If the initial one is happening right now, this'll wait for it.
			var errorList = await _frontend.GetLastBuildErrors();

			if (errorList != null)
			{
				// Outputting an error page - there's frontend errors, which means anything other than a helpful 
				// error page will very likely result in a broken page anyway.

				GenerateErrorPage(errorList, writer);
				return true;
			}
#endif
			var locale = await context.GetLocale();
			var _config = (locale.Id < _configurationTable.Length) ? _configurationTable[locale.Id] : _defaultConfig;

			// Start building the document:
			writer.WriteASCII("<!doctype html><html");

			var localeCode = locale.Code.Contains('-') ? locale.Code.Split('-')[0] : locale.Code;

			writer.WriteASCII(" class=\"");
			writer.WriteASCII(isAdmin ? "admin web no-js" : "ui web no-js");
			writer.WriteASCII("\" lang=\"");
			writer.WriteASCII(localeCode);
			writer.WriteASCII("\" data-theme=\"");
			writer.WriteASCII(isAdmin ? themeConfig.DefaultAdminThemeId : themeConfig.DefaultThemeId);
			writer.WriteASCII("\"");

			if (locale.RightToLeft)
			{
				writer.WriteASCII(" dir=\"rtl\"");
			}

			if (context.RoleId == 1)
			{
				writer.WriteASCII(" data-env=\"");
				writer.WriteASCII(Services.Environment);
				writer.WriteASCII("\"");
			}

			// Closing the <html> tag
			writer.WriteASCII(">");

			await BuildHeader(context, writer, pageWithTokens);

			writer.WriteASCII("<body data-ts=\"");
			writer.WriteASCII(_frontend.VersionString);
			writer.WriteASCII("\">");

			writer.WriteASCII("<div id='react-root'>");

			// True if either config states SSR is on, or the override is indicating it should pre-render:
			var preRenderHtml = preRender.HasValue && preRender.Value == true || _config.PreRender;

			if (preRenderHtml && !isAdmin)
			{
				// Construct page state:
				var pgStateWriter = Writer.GetPooled();
				pgStateWriter.Start(null);
				await BuildState(context, pgStateWriter, pageWithTokens, exactUrl);
				var pgStateForSSR = pgStateWriter.ToUTF8String();
				pgStateWriter.Release();

				// And the user's context:
				var publicContext = await _contextService.ToJsonString(context);

				try
				{
					var preRenderResult = await _canvasRendererService.Render(
						context.LocaleId,
						publicContext,
						null,
						pgStateForSSR,
						RenderMode.Html,
						false
					);

					if (preRenderResult.Failed)
					{
						// JS not loaded yet or otherwise not reachable by the API process.
						writer.WriteS("<h1>Hello! This site is not available just yet.</h1>"
							+ "<p>If you're a developer, check the console for a 'Done handling UI changes' " +
							"message - when that pops up, the UI has been compiled and is ready, then refresh this page.</p>" +
							"<p>Otherwise, this happens when the UI and Admin .js files aren't available to the API.</p>");
					}
					else
					{
						writer.WriteS(preRenderResult.Body);
						writer.WriteASCII("</div><script>window.gsInit=");
						writer.WriteS(publicContext);
						writer.WriteASCII(";</script><script type='application/json' id='pgState'>");
						writer.WriteS(pgStateForSSR);
						writer.WriteASCII("</script>");
					}
				}
				catch (Exception e)
				{
					// SSR failed. Return nothing and let the JS handle it for itself.
					Log.Error(LogTag, e, "Unable to render a page with SSR.");
				}
			}
			else
			{
				writer.WriteASCII("</div><script>window.gsInit=");

				// Serialise the user context:
				await _contextService.ToJsonString(context, writer);

				writer.WriteASCII(";</script><script type='application/json' id='pgState'>");
				await BuildState(context, writer, pageWithTokens, exactUrl);
				writer.WriteASCII("</script>");
			}

			// Handle all start body JS scripts
			HandleCustomScriptList(_config.StartBodyJs, writer);

			// Handle all Before Main JS scripts
			HandleCustomScriptList(_config.BeforeMainJs, writer);

			writer.WriteASCII("<script>");
			writer.Write(_configJson, 0, _configJson.Length);
			writer.WriteS(_frontend.GetServiceUrls(locale.Id));
			writer.Write(_frontend.InlineJavascriptHeader, 0, _frontend.InlineJavascriptHeader.Length);
			writer.WriteASCII("</script>");

			if (isAdmin)
			{
				// Get the main admin JS file. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
				// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
				// Admin modules must be added to page before frontend ones, as the frontend file includes UI/Start and the actual start call.
				var mainAdminJsFile = await _frontend.GetAdminMainJs(context == null ? 1 : context.LocaleId);
				WriteScriptTag(writer, _config.FullyQualifyUrls ? mainAdminJsFile.FqPublicUrl : mainAdminJsFile.PublicUrl, false);

				// Same also for the email modules:
				var mainEmailJsFile = await _frontend.GetEmailMainJs(context == null ? 1 : context.LocaleId);
				WriteScriptTag(writer, _config.FullyQualifyUrls ? mainEmailJsFile.FqPublicUrl : mainEmailJsFile.PublicUrl, false);
			}

			// Get the main JS file. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainJsFile = await _frontend.GetMainJs(context == null ? 1 : context.LocaleId);

			WriteScriptTag(writer, _config.FullyQualifyUrls ? mainJsFile.FqPublicUrl : mainJsFile.PublicUrl, _config.DeferMainJs);

			// Handle all After Main JS scripts
			HandleCustomScriptList(_config.AfterMainJs, writer);

			// Handle all End Body JS scripts
			HandleCustomScriptList(_config.EndBodyJs, writer);

			// Closing body and html:
			writer.WriteASCII("</body></html>");

			return true;
		}

		private void WriteScriptTag(Writer writer, string url, bool defer)
		{
			writer.WriteASCII("<script src=\"");
			writer.WriteS(url);
			writer.WriteASCII("\"");
			if (defer)
			{
				writer.WriteASCII(" defer async");
			}
			writer.WriteASCII("></script>");
		}

		/// <summary>
		/// Get all the site domains for use in tokeniser and url links
		/// </summary>
		/// <returns></returns>
		private string GetAvailableDomains()
		{
			if (_siteDomains != null)
			{
				return _siteDomains;
			}

			_siteDomains = "";

			var domainService = Services.Get("SiteDomainService");
			if (domainService != null)
			{
				var getSiteDomains = domainService.GetType().GetMethod("GetSiteDomains");

				_siteDomains = getSiteDomains.Invoke(domainService, null).ToString();

				if (!string.IsNullOrWhiteSpace(_siteDomains))
				{
					_siteDomains = _siteDomains + ",";
				}
			}

			return _siteDomains;
		}

		/// <summary>
		/// Get all the site domain prefixes
		/// </summary>
		/// <returns></returns>
		private string GetAvailableDomainPrefixes()
		{
			if (_siteDomainPrefixes != null)
			{
				return _siteDomainPrefixes;
			}

			_siteDomainPrefixes = "";

			var domainService = Services.Get("SiteDomainService");
			if (domainService != null)
			{
				var getSiteDomainPrefixes = domainService.GetType().GetMethod("GetSiteDomainPrefixes");

				_siteDomainPrefixes = getSiteDomainPrefixes.Invoke(domainService, null).ToString();
			}

			return _siteDomainPrefixes;
		}


		/// <summary>
		/// Used to replace tokens within a string with Primary object content
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pageField"></param>
		/// <param name="primaryObject"></param>
		/// <returns></returns>
		public async ValueTask<string> ReplaceTokens(Context context, string pageField, object primaryObject)
		{
			if (pageField == null)
			{
				return pageField;
			}

			string state = null;

			// We need to find out if there is a token to be handled.
			if (primaryObject != null)
			{

				var mode = 0; // 0= text, 1 = inside a {token.field}
				List<string> tokens = new List<string>();
				var storedIndex = 0;

				// we have one. Now, do we have a meta file value stored within the field?
				for (var i = 0; i < pageField.Length; i++)
				{
					var currentChar = pageField[i];
					if (mode == 0)
					{
						// Optional $
						if (currentChar == '$' && i < pageField.Length - 1 && pageField[i + 1] == '{')
						{
							mode = 1;
							storedIndex = i;
							i++;
						}
						else if (currentChar == '{')
						{
							// now in a token.
							mode = 1;
							storedIndex = i;
						}
					}
					else if (mode == 1)
					{
						if (currentChar == '}')
						{
							// we have the end of the token, let's get it.
							var token = pageField.Substring(storedIndex, i - storedIndex + 1);
							tokens.Add(token);
							mode = 0;
						}
					}
				}

				// Let's handle our tokens.
				foreach (var token in tokens)
				{
					// remove brackets
					var startLen = token[0] == '$' ? 2 : 1;
					var noBrackets = token.Substring(startLen, token.Length - 1 - startLen);

					// Let's split it - to get content and its field.
					var contentAndField = noBrackets.Split(".");

					// Is this valid?
					if (contentAndField.Length != 2)
					{
						// nope, no replacement or further action for this token.
						break;
					}

					// This should have a content and field since its 2 pieces
					var content = contentAndField[0];
					var field = contentAndField[1];

					// Is the content type valid?
					var systemType = ContentTypes.GetType(content);

					if (systemType == null)
					{
						// invalid content, break
						break;
					}

					var fieldInfo = systemType.GetField(field, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy | BindingFlags.Instance); // This is built in .net stuff - you're into reflection here

					if (fieldInfo == null)
					{
						break;
					}

					var value = fieldInfo.GetValue(primaryObject);

					if (value == null)
					{
						break;
					}

					var localized = value as ILocalized;

					// We need to swap out the string with the value.
					var strValue = (localized != null) ? localized.GetStringValue(context) : value.ToString();

					if (strValue.StartsWith('{') && strValue.EndsWith('}'))
					{
						if (state == null)
						{
							state = "{\"po\": " + JsonConvert.SerializeObject(primaryObject, jsonSettings) + "}";
						}

						var renderResult = await _canvasRendererService.Render(context, strValue, state, RenderMode.Text);
						strValue = renderResult.Text;
					}

					pageField = pageField.Replace(token, strValue);
				}
			}

			return pageField;
		}

		/// <summary>
		/// Handles adding a custom script list (if there even is one set) into the given writer. They'll be appended.
		/// </summary>
		private void HandleCustomHeadList(List<HeadTag> list, Writer writer, bool permitRemote = true)
		{
			if (list == null)
			{
				return;
			}

			foreach (var headTag in list)
			{
				if (headTag.IsRel && !permitRemote)
				{
					continue;
				}

				var html = headTag.GetHtml();
				writer.WriteS(html);
			}

		}

		/// <summary>
		/// Handles adding a custom script list (if there even is one set) into the given node. They'll be appended.
		/// </summary>
		private void HandleCustomScriptList(List<BodyScript> list, Writer writer, bool permitRemote = true)
		{
			if (list == null)
			{
				return;
			}

			foreach (var bodyScript in list)
			{
				//Does this script have content?
				var htmlStr = bodyScript.GetHtml(out bool isRemote);

				if (!permitRemote && isRemote)
				{
					continue;
				}

				writer.WriteS(htmlStr);
			}

		}

		private void CopyToMd5(Writer writer, System.Security.Cryptography.MD5 md5)
		{
			var currentBuffer = writer.FirstBuffer;
			while (currentBuffer != null)
			{
				var blockSize = (currentBuffer == writer.LastBuffer) ? writer.CurrentFill : currentBuffer.Length;
				md5.TransformBlock(currentBuffer.Bytes, 0, blockSize, null, 0);
				currentBuffer = currentBuffer.After;
			}
		}

		private string CreateMd5HashString(System.Security.Cryptography.MD5 md5)
		{
			md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			var hashBytes = md5.Hash;

			// Convert the byte array to hexadecimal string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hashBytes.Length; i++)
			{
				sb.Append(hashBytes[i].ToString("x2"));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Performs the main routing and generates HTML when needed.
		/// </summary>
		/// <param name="httpContext">The http context</param>
		/// <param name="basicContext">The main API context (the basic, partially loaded variant)</param>
		/// <param name="pageAndTokens">The page and its tokens being rendered.</param>
		/// <returns></returns>
		public async ValueTask<bool> RouteBasicContextRequest(HttpContext httpContext, Context basicContext, PageWithTokens pageAndTokens)
		{
			// Full context is required for pages:
			var context = await httpContext.Request.GetContext(basicContext);

			return await RouteRequest(httpContext, context, pageAndTokens);
		}

		/// <summary>
		/// Performs the main routing and generates HTML when needed.
		/// </summary>
		/// <param name="httpContext">The http context</param>
		/// <param name="context">The main API context</param>
		/// <param name="pageAndTokens">The page and its tokens being rendered.</param>
		/// <returns></returns>
		public async ValueTask<bool> RouteRequest(HttpContext httpContext, Context context, PageWithTokens pageAndTokens)
		{
			HttpRequest request = httpContext.Request;
			HttpResponse response = httpContext.Response;

			// Permission check - can the user actually see this page?
			// If not, is there another role they must be where they can see the page?
			// i.e. typically that is in the form of the page requiring the user to login first.


			var _config = (context.LocaleId < _configurationTable.Length) ? _configurationTable[context.LocaleId] : _defaultConfig;

			response.ContentType = "text/html";
			// response.Headers["Content-Encoding"] = "gzip";

			var writer = Writer.GetPooled();
			writer.Start(null);

			// If we have a block wall password set, and there either isn't a time limit or the limit is in the future, and the user is not an admin:
			if (
				_config.BlockWallPassword != null &&
				(_config.BlockWallActiveUntil == null || _config.BlockWallActiveUntil.Value > DateTime.UtcNow) &&
				(context.Role == null || !context.Role.CanViewAdmin)
			)
			{
				// Cookie check - have they set the password cookie?
				var cookie = request.Cookies["protect"];

				if (string.IsNullOrEmpty(cookie) || cookie != _config.BlockWallPassword)
				{
					response.Headers["Cache-Control"] = "no-store";
					response.Headers["Pragma"] = "no-cache";
					RenderBlockPage(writer);
					await writer.CopyToAsync(response.Body);
					writer.Release();
					return true;
				}
			}

			bool pullFromCache = (
				!_config.DisablePageCache &&
				_config.CacheMaxAge > 0 &&
				_config.CacheAnonymousPages &&
				context.UserId == 0 &&
				context.RoleId == 6
			);

			if (!pullFromCache)
			{
				response.Headers["Cache-Control"] = "no-store";
				response.Headers["Pragma"] = "no-cache";
			}

			response.StatusCode = 200;

			await Events.Page.BeforeNavigate.Dispatch(context, pageAndTokens);

			if (context.UserId != 0)
			{
				// Update the token:
				context.SendToken(response);
			}

			await RenderPage(context, writer, pageAndTokens, request.Path + request.QueryString);
			await writer.CopyToAsync(response.Body);
			writer.Release();
			return true;
		}

		/// <summary>
		/// Generates the just the site header for a given page. 
		/// If you don't provide a page, you'll get a generic admin header with no page specific content in it.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="responseStream"></param>
		/// <param name="pageWithTokens"></param>
		/// <returns></returns>
		public async ValueTask BuildHeaderOnly(Context context, Stream responseStream, PageWithTokens? pageWithTokens = null)
		{
			// Does the locale exist? (intentionally using a blank context here - it must only vary by localeId)
			var locale = await context.GetLocale();

			if (locale == null)
			{
				// Dodgy locale - quit:
				return;
			}

			Writer writer = Writer.GetPooled();
			writer.Start(null);
			if (pageWithTokens == null)
			{
				// Generic admin header
				await BuildHeader(context, writer, new PageWithTokens() { });
			}
			else
			{
				await BuildHeader(context, writer, pageWithTokens.Value);
			}
			await writer.CopyToAsync(responseStream);
			writer.Release();
		}

		/// <summary>
		/// Get all the active locales 
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		private List<Locale> GetAllLocales(Context ctx)
		{
			if (_allLocales != null && _allLocales.Any())
			{
				return _allLocales;
			}

			// Get all the current locales:
			var locales = _localeService.Where("").ListAll(ctx).Result;

			if (locales != null && locales.Any())
			{
				_allLocales = locales;
			}
			else
			{
				_allLocales = new List<Locale>();
			}

			return _allLocales;
		}

		/// <summary>
		/// Return the canonical version of the given URL.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="locales"></param>
		/// <returns></returns>
		private string GetCanonicalUrl(string url, List<Locale> locales)
		{
			var parsedUrl = new Uri(url);

			if (locales != null && locales.Count(l => l.Id != 1) > 0)
			{

				foreach (var altLocale in locales.Where(l => l.Id != 1))
				{
					var lowerLocale = altLocale.Code.ToLower();
					//var port = parsedUrl.Port > -1 && parsedUrl.Port != 80 ? $":{parsedUrl.Port}" : "";

					if (parsedUrl.LocalPath.StartsWith("/" + lowerLocale))
					{
						return UrlCombine($"{parsedUrl.Scheme}://{parsedUrl.Host}", parsedUrl.PathAndQuery.Substring(lowerLocale.Length + 1));
					}
				}
			}

			return url;
		}


		/// <summary>
		/// Return a locale-specific version of the given URL.
		/// </summary>
		/// <param name="locale"></param>
		/// <param name="url"></param>
		/// <returns></returns>
		private string GetLocaleUrl(Locale locale, string url)
		{
			var parsedUrl = new Uri(url);
			var lowerLocale = locale.Code.ToLower();
			//var port = parsedUrl.Port > -1 && parsedUrl.Port != 80 ? $":{parsedUrl.Port}" : "";

			if (parsedUrl != null && parsedUrl.PathAndQuery == "/")
			{
				return UrlCombine($"{parsedUrl.Scheme}://{parsedUrl.Host}", lowerLocale);
			}
			else
			{
				return UrlCombine($"{parsedUrl.Scheme}://{parsedUrl.Host}", lowerLocale, parsedUrl.PathAndQuery);
			}
		}

		/// <summary>
		/// Strips any locale prefix (e.g. /en-us/) from the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string GetPathWithoutLocale(string path)
		{

			if (_allLocales != null && _allLocales.Any())
			{
				var parsedUrl = new Uri(path);

				foreach (var locale in _allLocales)
				{
					var localeCode = "/" + locale.Code.ToLower();

					if (parsedUrl.LocalPath.StartsWith(localeCode))
					{
						return path.Replace(localeCode, "");
					}

				}

			}

			return path;
		}

		/// <summary>
		/// Return the locale supplied in the given URL.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private string GetLocaleFromUrl(string url)
		{

			if (_allLocales != null && _allLocales.Any())
			{
				var parsedUrl = new Uri(url);

				foreach (var locale in _allLocales)
				{
					var localePrefix = "/" + locale.Code.ToLower() + "/";

					if (parsedUrl.AbsolutePath.StartsWith(localePrefix))
					{
						return locale.Code.ToLower();
					}

				}

			}

			return "";
		}

		/// <summary>
		/// Combine segments of a URL, ensuring no double slashes.
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static string UrlCombine(params string[] items)
		{
			return string.Join("/", items.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim('/', '\\')));
		}
	}

}
