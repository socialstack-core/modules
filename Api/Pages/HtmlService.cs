using System.Threading.Tasks;
using Api.Configuration;
using System;
using System.IO;
using System.IO.Compression;
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

namespace Api.Pages
{
	/// <summary>
	/// Handles the main generation of HTML from the index.html base template at UI/public/index.html and Admin/public/index.html
	/// </summary>

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
				cache = null;
				BuildConfigLocaleTable();
				return new ValueTask();
			};

			BuildConfigLocaleTable();

			Events.Page.AfterUpdate.AddEventListener((Context context, Page page) =>
			{
				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterDelete.AddEventListener((Context context, Page page) =>
			{
				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterCreate.AddEventListener((Context context, Page page) =>
			{
				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Page>(page);
			});

			Events.Page.Received.AddEventListener((Context context, Page page, int mode) =>
			{

				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Page>(page);
			});

			Events.Translation.AfterUpdate.AddEventListener((Context context, Translation tr) =>
			{
				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Translation>(tr);
			});

			Events.Translation.AfterDelete.AddEventListener((Context context, Translation tr) =>
			{
				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Translation>(tr);
			});

			Events.Translation.AfterCreate.AddEventListener((Context context, Translation tr) =>
			{
				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Translation>(tr);
			});

			Events.Translation.Received.AddEventListener((Context context, Translation tr, int mode) =>
			{

				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Translation>(tr);
			});
		}

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
		/// Types that have had an update event handler added to them. These handlers listen for updates (including remote ones), 
		/// obtain the URL of the thing that changed, and then clear the cached entry if there is one.
		/// </summary>
		private Dictionary<int, bool> eventHandlersByContentTypeId = new Dictionary<int, bool>();

		/// <summary>
		/// Used for thread aware cache updates.
		/// </summary>
		private readonly object cacheLock = new object();

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
		/// User specific state data. This combined with pageState indicates a page load.
		/// </summary>
		/// <returns></returns>
		private async ValueTask<string> BuildUserGlobalStateJs(Context context)
		{
			return await _contextService.ToJsonString(context);
		}

		/// <summary>
		/// Url -> nodes that are as pre-generated as possible. For example, for anon users it is completely precompressed. 
		/// Locale sensitive; indexed by locale.Id-1.
		/// </summary>
		private ConcurrentDictionary<string, CachedPageData>[] cache = null;

		/// <summary>
		/// Clear the cache
		/// </summary>
		public void ClearCache()
		{
			cache = null;
		}

		/// <summary>
		/// Renders the state only of a page to a JSON string.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pageAndTokens"></param>
		/// <param name="response"></param>
		/// <param name="cacheUrl">Provide this if you would like your state response to potentially come from the cache for the given URL.
		/// The URL itself will be ignored if pageAndTokens.StatusCode is 404 - it just has to be not null for the cache behaviour to potentially occur.</param>
		/// <returns></returns>
		public async ValueTask RenderState(Context context, PageWithTokens pageAndTokens, HttpResponse response, string cacheUrl = null)
		{
			var terminal = pageAndTokens.PageTerminal;
			var page = terminal.Page;

			var isAdmin = terminal.IsAdmin;
			var locale = await context.GetLocale();
			var _config = (locale.Id < _configurationTable.Length) ? _configurationTable[locale.Id] : _defaultConfig;

			bool pullFromCache = (
				!_config.DisablePageCache &&
				cacheUrl != null &&
				_config.CacheMaxAge > 0 &&
				_config.CacheAnonymousPages &&
				!isAdmin &&
				context.UserId == 0 && context.RoleId == 6
			);

			if (pullFromCache)
			{
				pullFromCache = await Events.Context.CanUseCache.Dispatch(context, pullFromCache);
			}

			ConcurrentDictionary<string, CachedPageData> localeCache = null;
			CachedPageData cpd = null;

			if (pullFromCache)
			{
				if (pageAndTokens.StatusCode == 404)
				{
					cacheUrl = page == null ? "/404" : page.Url;
				}

				// Try load from cache now.

				lock (cacheLock)
				{
					if (cache == null)
					{
						cache = new ConcurrentDictionary<string, CachedPageData>[context.LocaleId];
					}
					else if (cache.Length < context.LocaleId)
					{
						Array.Resize(ref cache, (int)context.LocaleId);
					}

					localeCache = cache[context.LocaleId - 1];

					if (localeCache == null)
					{
						cache[context.LocaleId - 1] = localeCache = new ConcurrentDictionary<string, CachedPageData>();
					}
				}

				if (localeCache.TryGetValue(cacheUrl, out cpd) && cpd.AnonymousCompressedState != null)
				{
					// Copy direct to target stream.
					response.Headers["Content-Encoding"] = "gzip";
					await response.Body.WriteAsync(cpd.AnonymousCompressedState);
					return;
				}
			}

			object primaryObject = pageAndTokens.PrimaryObject;
			AutoService primaryService = pageAndTokens.PrimaryService;

			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{" + GetAvailableDomains() + "\"page\":{\"bodyJson\":");

			if (isAdmin || terminal.Generator == null)
			{
				writer.WriteS(page.BodyJson);
			}
			else
			{
				// Execute canvas graphs:
				await terminal.Generator.Generate(context, writer, pageAndTokens.PrimaryObject);
			}

			writer.WriteASCII(",\"title\":\"");
			writer.WriteS(page.Title);
			writer.WriteASCII("\",\"id\":");
			writer.WriteS(page.Id);
			writer.Write((byte)'}');

			var cfgBytes = _configurationService.GetLatestFrontendConfigBytesJson();

			if (cfgBytes != null)
			{
				writer.WriteASCII(",\"config\":");
				writer.WriteNoLength(cfgBytes);
			}

			if (terminal.UrlTokenNamesJson != null)
			{
				writer.WriteASCII(",\"tokenNames\":");
				writer.WriteS(terminal.UrlTokenNamesJson);
			}

			writer.WriteASCII(",\"tokens\":");
			writer.WriteS(pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null");

			if (primaryObject != null)
			{
				writer.WriteASCII(",\"po\":");
				await primaryService.ObjectToTypeAndIdJson(context, primaryObject, writer);
			}

			if (page != null && !string.IsNullOrEmpty(page.Title))
			{
				writer.WriteASCII(",\"title\":");
				var titleStr = page.Title;

				if (primaryObject != null)
				{
					writer.WriteEscaped(await ReplaceTokens(context, titleStr, primaryObject));
				}
				else
				{
					writer.WriteEscaped(titleStr);
				}
			}

			if (page != null && !string.IsNullOrEmpty(page.Description))
			{
				writer.WriteASCII(",\"description\":");
				var descriptionStr = page.Description;

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

			if (pullFromCache)
			{
				// Put writer contents in to the cache.
				if (cpd == null)
				{
					// Just in case it was created by someone else since rendering the state:
					localeCache.TryGetValue(cacheUrl, out cpd);

					if (cpd == null)
					{
						cpd = new CachedPageData(null);
						localeCache.TryAdd(cacheUrl, cpd);
					}
				}

				using var ms = new MemoryStream();
				await WriteWriterCompressed(writer, ms);
				cpd.AnonymousCompressedState = ms.ToArray();

				response.Headers["Content-Encoding"] = "gzip";
				await response.Body.WriteAsync(cpd.AnonymousCompressedState);
			}
			else
			{
				await writer.CopyToAsync(response.Body);
			}

			writer.Release();
		}

		/// <summary>
		/// Generated block page (it's always the same).
		/// </summary>
		private List<DocumentNode> _blockPage;

		/// <summary>
		/// Typically only on stage. It's the same every time.
		/// </summary>
		/// <returns></returns>
		private List<DocumentNode> GenerateBlockPage()
		{
			if (_blockPage != null)
			{
				return _blockPage;
			}

			var doc = new Document();
			doc.Title = "Unpublished website";

			// Charset must be within first 1kb of the header:
			doc.Head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));
			doc.Head.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"));
			doc.Head.AppendChild(new DocumentNode("meta", true).With("name", "robots").With("content", "noindex"));

			doc.Head.AppendChild(new DocumentNode("title").AppendChild(new TextNode(doc.Title)));

			// Fail in style:
			doc.Head.AppendChild(new DocumentNode("link").With("rel", "stylesheet").With("href", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"));
			doc.Head.AppendChild(new DocumentNode("style").AppendChild(new TextNode(
				@"
.block-page { 
	background: linear-gradient(to right, #9E40B5,#350EE5, #100862);
	color: #fff;
	min-height: calc(100vh - env(safe-area-inset-top,0) - env(safe-area-inset-bottom,0));
}

.block-page h1 {
	font-size: 3rem;
	text-align: center;
}

.block-page h2 {
	font-size: 2rem;
	line-height: 1.5;
	text-align: center;
	opacity: .8;
}

.block-page form {
	max-width: 500px;
	margin: 0 auto;
}

.block-page .pwd-box {
	padding: 0 10vw;
}

svg {
	width: 1.25rem;
	height: 1.25rem;
}

.block-page h3 {
	display: flex;
    margin-top: 2rem;
    font-size: 1.25rem;
    align-items: center;
    gap: .5rem;
    justify-content: center;
    opacity: .6;
}

.block-page p {
	text-align: center;
	opacity: .75;
}

@media (max-aspect-ratio: 4/3) and (hover: none) and (orientation: portrait) {
 
	.block-page h1 {
		font-size: 4rem;
	}

	.block-page h2 {
		font-size: 2.75rem;
	}

	.block-page form {
		max-width: 80vw;
	}

	.block-page input {
		font-size: 2.5rem;
	}

	svg {
		width: 2.5rem;
		height: 2.5rem;
	}

	.block-page h3 {
		margin-top: 3rem;
		font-size: 2.5rem;
		gap: .75rem;
	}

	.block-page p {
		font-size: 2.25rem;
		max-width: 600px;
		margin: 0 auto;
	}

}

@media (max-width: 540px) and (hover: none) and (orientation: portrait) {

    .block-page h1 {
        font-size: 7vw;
    }

    .block-page h2,
    .block-page input,
    .block-page h3 {
        font-size: 4vw;
    }

    svg {
        width: 4vw;
        height: 4vw;
    }

    .block-page p {
        font-size: 4.5vw;
        margin-bottom: 0.75em;
    }

}
				"
			)));

			// NB: use this to target landscape touch devices if necessary
			//@media (min-aspect-ratio: 5/3) and (hover: none) and (orientation: landscape)

			var body = doc.Body;

			var center = new DocumentNode("div").With("class", "d-flex justify-content-center align-items-center block-page");
			body.AppendChild(center);

			var container = new DocumentNode("div").With("class", "pwd-box");
			center.AppendChild(container);

			var header = new DocumentNode("h1");
			header.AppendChild(new TextNode("Welcome! This site hasn't been published yet"));
			container.AppendChild(header);

			header = new DocumentNode("h2");
			header.AppendChild(new TextNode("If you're a content owner, you can preview this site by entering the password below."));
			container.AppendChild(header);

			var form = new DocumentNode("form").With("action", "").With("method", "POST").With("id", "content_pwd_form").With("class", "d-flex justify-content-center mt-5");
			form.AppendChild(new DocumentNode("input", true).With("type", "password").With("id", "password").With("class", "form-control form-control-lg"));
			form.AppendChild(new DocumentNode("input", true).With("type", "submit").With("class", "btn btn-primary btn-lg ml-2").With("value", "Go"));

			container.AppendChild(form);

			var iconPath = new DocumentNode("path").With("fill", "currentColor").With("d", "M500 10C229.4 10 10 229.4 10 500s219.4 490 490 490 490-219.4 490-490S770.6 10 500 10zm-3.1 832.9c-39.9 0-72.3-31.7-72.3-70.9s32.4-70.9 72.3-70.9 72.3 31.7 72.3 70.9-32.4 70.9-72.3 70.9zM691.1 442c-14.1 22.3-44.3 52.7-90.4 91.1-23.9 19.9-38.7 35.9-44.5 48-5.8 12.1-8.4 33.7-7.9 64.9H445.4c-.3-14.8-.4-23.8-.4-27 0-33.3 5.5-60.7 16.5-82.2s33.1-45.7 66.1-72.5c33-26.9 52.9-44.5 59.3-52.8 9.9-13.2 14.9-27.7 14.9-43.5 0-22-8.9-40.8-26.5-56.5-17.6-15.7-41.4-23.5-71.3-23.5-28.8 0-52.9 8.2-72.3 24.5s-36 52.4-40 74.7c-3.7 21-105.2 29.9-104-12.7 1.2-42.6 23.5-89 61.5-122.6 38.1-33.6 88-50.4 149.9-50.4 65.1 0 116.9 17 155.3 51 38.5 34 57.7 73.6 57.7 118.7.1 24.9-6.9 48.5-21 70.8z");
			var icon = new DocumentNode("svg").With("xmlns", "http://www.w3.org/2000/svg").With("viewBox", "0 0 1000 1000");
			icon.AppendChild(iconPath);

			header = new DocumentNode("h3");
			header.AppendChild(icon);
			header.AppendChild(new TextNode("What is this?"));
			container.AppendChild(header);

			var em = new DocumentNode("em");
			em.AppendChild(new TextNode("block password"));

			var paragraph = new DocumentNode("p");
			paragraph.AppendChild(new TextNode("To access this site, you'll need to request a copy of the&nbsp;"));
			paragraph.AppendChild(em);
			paragraph.AppendChild(new TextNode("."));
			container.AppendChild(paragraph);

			var paragraph2 = new DocumentNode("p");
			paragraph2.AppendChild(new TextNode("Please note, this is not the same as a user account password."));
			container.AppendChild(paragraph2);

			body.AppendChild(new DocumentNode("script").With("type", "text/javascript").AppendChild(new TextNode(
				@"
				function setCookie(name,value,days) {
					var expires = """";
					if (days) {
						var date = new Date();
						date.setTime(date.getTime() + (days*24*60*60*1000));
						expires = ""; expires = "" + date.toUTCString();

					}
					document.cookie = name + ""="" + (value || """")  + expires + ""; path=/"";
				}
				var pwd_form = document.getElementById('content_pwd_form');

				pwd_form.onsubmit = function()
				{
					var password = document.getElementById('password').value;
					setCookie(""protect"", password, 60);
					window.location.reload(true);
				};
			"
			)));

			var flatNodes = doc.Flatten();

			// Swap all the TextNodes for byte blocks.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is TextNode node1)
				{
					var bytes = System.Text.Encoding.UTF8.GetBytes(node1.TextContent);
					flatNodes[i] = new RawBytesNode(bytes);
				}
			}

			_blockPage = flatNodes;
			return flatNodes;
		}

		/// <summary>
		/// Only on development.
		/// </summary>
		/// <param name="errors"></param>
		/// <returns></returns>
		private List<DocumentNode> GenerateErrorPage(List<UIBuildError> errors)
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
			var doc = new Document();
			doc.Title = "Oops! Something has gone very wrong. " + messages[rng.Next(0, messages.Length)];

			// Charset must be within first 1kb of the header:
			doc.Head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));
			doc.Head.AppendChild(new DocumentNode("title").AppendChild(new TextNode(doc.Title)));

			// Fail in style:
			doc.Head.AppendChild(new DocumentNode("link").With("rel", "stylesheet").With("href", "https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"));
			doc.Head.AppendChild(new DocumentNode("style").AppendChild(new TextNode(
				@".callout {padding: 20px;margin: 20px 0;border: 1px solid #eee;border-left-width: 5px;border-radius: 3px;}
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
				}"
			)));

			var body = doc.Body;
			var container = new DocumentNode("div").With("class", "container");
			body.AppendChild(container);

			var introError = new DocumentNode("div").With("class", "alert alert-danger").With("role", "alert");
			introError.AppendChild(new TextNode("<b>" + errors.Count + " error(s)</b> during UI build."));
			container.AppendChild(introError);

			foreach (var error in errors)
			{
				var errorMessage = new DocumentNode("div").With("class", "callout callout-danger");

				var header = new DocumentNode("h4");
				header.AppendChild(new TextNode(error.Title));
				errorMessage.AppendChild(header);

				var errorFile = new DocumentNode("p");
				errorFile.AppendChild(new TextNode(error.File));
				errorMessage.AppendChild(errorFile);

				var descript = new DocumentNode("pre").AppendChild(new TextNode(HttpUtility.HtmlEncode(error.Description)));
				errorMessage.AppendChild(descript);
				container.AppendChild(errorMessage);
			}

			var flatNodes = doc.Flatten();

			// Swap all the TextNodes for byte blocks.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is TextNode node1)
				{
					var bytes = System.Text.Encoding.UTF8.GetBytes(node1.TextContent);
					flatNodes[i] = new RawBytesNode(bytes);
				}
			}

			return flatNodes;
		}

		/// <summary>
		/// Only renders the header. The body is blank.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="locale"></param>
		/// <returns></returns>
		private async ValueTask<List<DocumentNode>> RenderHeaderOnly(Context context, Locale locale)
		{
			var themeConfig = _themeService.GetConfig();
			var localeCode = locale.Code.Contains('-') ? locale.Code.Split('-')[0] : locale.Code;

			// Generate the document:
			var doc = new Document();
			doc.Path = "/";
			doc.Title = ""; // Todo: permit {token} values in the title which refer to the primary object.
			doc.Html.With("class", "ui head-only").With("lang", localeCode)
				.With("data-theme", themeConfig.DefaultAdminThemeId);

			if (locale.RightToLeft)
			{
				doc.Html.With("dir", "rtl");
			}

			if (context.RoleId == 1)
			{
				doc.Html.With("data-env", Services.Environment);
			}

			var head = doc.Head;

			var _config = (locale.Id < _configurationTable.Length) ? _configurationTable[locale.Id] : _defaultConfig;

			// If there are tokens, get the primary object:

			// Charset must be within first 1kb of the header:
			head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, head, false);

			// favicon
			// https://evilmartians.com/chronicles/how-to-favicon-in-2021-six-files-that-fit-most-needs
			/*
            <link rel="icon" href="/favicon.ico" sizes="any"><!-- 32×32 -->
            <link rel="icon" href="/icon.svg" type="image/svg+xml">
            <link rel="apple-touch-icon" href="/apple-touch-icon.png"><!-- 180×180 -->
            <link rel="manifest" href="/manifest.webmanifest">
            */

			head.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "32x32").With("href", "/favicon-32x32.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "16x16").With("href", "/favicon-16x16.png"));

			// Get the main CSS files. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainCssFile = await _frontend.GetMainCss(context == null ? 1 : context.LocaleId);
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", _config.FullyQualifyUrls ? mainCssFile.FqPublicUrl : mainCssFile.PublicUrl));

			var mainAdminCssFile = await _frontend.GetAdminMainCss(context == null ? 1 : context.LocaleId);
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", _config.FullyQualifyUrls ? mainAdminCssFile.FqPublicUrl : mainAdminCssFile.PublicUrl));
			head.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", _config.AppThemeColor))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"));

			// Handle all End Head tags in the config.
			HandleCustomHeadList(_config.EndHeadTags, head, false);

			// Build the flat HTML for the page:
			var flatNodes = doc.Flatten();

			// Note: Although gzip does support multiple concatenated gzip blocks, browsers do not implement this part of the gzip spec correctly.
			// Unfortunately that means no part of the stream can be pre-compressed; must compress the whole thing and output that.

			// Swap all the TextNodes for byte blocks.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is TextNode node1)
				{
					var bytes = Encoding.UTF8.GetBytes(node1.TextContent);
					flatNodes[i] = new RawBytesNode(bytes);
				}
			}

			return flatNodes;
		}

		/// <summary>
		/// Note that context may only be used for the role information, not specific user details.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="locale"></param>
		/// <param name="pageMeta"></param>
		/// <returns></returns>
		private async ValueTask<List<DocumentNode>> RenderNativeAppPage(Context context, Locale locale, MobilePageMeta pageMeta)
		{
			var themeConfig = _themeService.GetConfig();
			var localeCode = locale.Code.Contains('-') ? locale.Code.Split('-')[0] : locale.Code;

			// Generate the document:
			var doc = new Document();
			doc.Path = "/";
			doc.Title = ""; // Todo: permit {token} values in the title which refer to the primary object.
			doc.Html
				.With("class", "ui mobile").With("lang", localeCode)
				.With("data-theme", themeConfig.DefaultThemeId);

			if (locale.RightToLeft)
			{
				doc.Html.With("dir", "rtl");
			}

			if (context.RoleId == 1)
			{
				doc.Html.With("data-env", Services.Environment);
			}

			var head = doc.Head;

			var _config = (locale.Id < _configurationTable.Length) ? _configurationTable[locale.Id] : _defaultConfig;

			// If there are tokens, get the primary object:

			// Charset must be within first 1kb of the header:
			head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, head, false);

			// Handle all Start Head Scripts in the config.
			HandleCustomScriptList(_config.StartHeadScripts, head, false);

			head.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "32x32").With("href", "/favicon-32x32.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "16x16").With("href", "/favicon-16x16.png"));

			// Get the main CSS files. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", "pack/main." + locale.Code + ".css"));

			head.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", _config.AppThemeColor))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"));

#if DEBUG
            // inject dev-specific classes
            head.AppendChild(new DocumentNode("style").With("type", "text/css").AppendChild(new TextNode(
                @"
				a:not([href]), a[href=""""] {
					outline: 8px solid red;
				}
			"
            )));
#endif

			if (pageMeta.Cordova)
			{
				head.AppendChild(
						new DocumentNode("script")
						.With("src", "cordova.js")
				);
			}

			/*
			 * PWA headers that should only be added if PWA mode is turned on and these files exist
			  .AppendChild(new DocumentNode("link", true).With("rel", "apple-touch-icon").With("sizes", "180x180").With("href", "/apple-touch-icon.png"))
			  .AppendChild(new DocumentNode("link", true).With("rel", "manifest").With("href", "/site.webmanifest"))
			  .AppendChild(new DocumentNode("link", true).With("rel", "mask-icon").With("href", "/safari-pinned-tab.svg").With("color", "#ffffff"))
			 */

			// Handle all End Head tags in the config.
			HandleCustomHeadList(_config.EndHeadTags, head, false);

			// Handle all End Head Scripts in the config.
			HandleCustomScriptList(_config.EndHeadScripts, head, false);

			var reactRoot = new DocumentNode("div").With("id", "react-root");

			var body = doc.Body;
			body.AppendChild(reactRoot);

			// Handle all start body JS scripts
			HandleCustomScriptList(_config.StartBodyJs, body, false);

			// Handle all Before Main JS scripts
			HandleCustomScriptList(_config.BeforeMainJs, body, false);

			body.AppendChild(
					new DocumentNode("script")
					.AppendChild(new TextNode(_frontend.GetServiceUrls(locale.Id)))
					.AppendChild(new TextNode(_frontend.InlineJavascriptHeader))
			);

			if (pageMeta.IncludePages)
			{
				var allPages = await _pages.Where("!(Url startsWith ?)").Bind("/en-admin").ListAll(context);

				var writer = Writer.GetPooled();
				writer.Start(null);
				writer.WriteASCII("var pages=[");
				for (var i = 0; i < allPages.Count; i++)
				{
					if (i != 0)
					{
						writer.Write((byte)',');
					}
					await _pages.ToJson(context, allPages[i], writer);
				}
				writer.Write((byte)']');
				var outputUtf8Bytes = writer.AllocatedResult();
				writer.Release();

				body.AppendChild(
						new DocumentNode("script")
						.AppendChild(new RawBytesNode(outputUtf8Bytes))
				);
			}

			if (!string.IsNullOrEmpty(pageMeta.CustomJs))
			{
				body.AppendChild(
						new DocumentNode("script")
						.AppendChild(new TextNode(pageMeta.CustomJs))
				);
			}

			body.AppendChild(
					new DocumentNode("script")
					.AppendChild(new TextNode("storedToken=true;apiHost='" + pageMeta.ApiHost + "';config={pageRouter:{hash:true,localRouter:onRoutePage}};"))
			);

			var mainJs = new DocumentNode("script").With("src", "pack/main." + locale.Code + ".js");
			doc.MainJs = mainJs;
			body.AppendChild(mainJs);

			// Handle all After Main JS scripts
			HandleCustomScriptList(_config.AfterMainJs, body, false);

			// Handle all End Body JS scripts
			HandleCustomScriptList(_config.EndBodyJs, body, false);

			// Build the flat HTML for the page:
			var flatNodes = doc.Flatten();

			// Note: Although gzip does support multiple concatenated gzip blocks, browsers do not implement this part of the gzip spec correctly.
			// Unfortunately that means no part of the stream can be pre-compressed; must compress the whole thing and output that.

			// Swap all the TextNodes for byte blocks.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is TextNode node1)
				{
					var bytes = Encoding.UTF8.GetBytes(node1.TextContent);
					flatNodes[i] = new RawBytesNode(bytes);
				}
			}

			return flatNodes;
		}

		/// <summary>
		/// The config json, if there is any.
		/// </summary>
		private RawBytesNode _configJson = new RawBytesNode(Array.Empty<byte>());

		/// <summary>
		/// Note that context may only be used for the role information, not specific user details.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pageAndTokens"></param>
		/// <param name="path"></param>
		/// <param name="preRender">Optionally override if SSR should execute.</param>
		/// <returns></returns>
		private async ValueTask<List<DocumentNode>> RenderPage(Context context, PageWithTokens pageAndTokens, string path, bool? preRender = null)
		{
			var terminal = pageAndTokens.PageTerminal;

			if (terminal == null)
			{
				return null;
			}

			var page = terminal.Page;

			if (page == null)
			{
				return null;
			}

			var isAdmin = terminal.IsAdmin;
			var locales = GetAllLocales(context);

			var latestConfigBytes = _configurationService.GetLatestFrontendConfigBytes();

			if (latestConfigBytes != _configJson.Bytes)
			{
				// Note: this happens to also force theme css to be reobtained as well.
				// Cache dump:
				cache = null;
				_configJson.Bytes = latestConfigBytes;
			}

			var themeConfig = _themeService.GetConfig();

#if !DEBUG
			CachedPageData cpd;

			if (cache != null && context.LocaleId <= cache.Length && !pageAndTokens.Multiple)
			{
				var localeCache = cache[context.LocaleId - 1];

				if (localeCache != null && localeCache.TryGetValue(path, out cpd) && cpd.Nodes != null)
				{
					return cpd.Nodes;
				}
			}
#endif

#if DEBUG
            // Get the errors from the last build. If the initial one is happening right now, this'll wait for it.
            var errorList = await _frontend.GetLastBuildErrors();

            if (errorList != null)
            {
                // Outputting an error page - there's frontend errors, which means anything other than a helpful 
                // error page will very likely result in a broken page anyway.

                return GenerateErrorPage(errorList);

            }
#endif

			// Get the locale:
			var locale = await context.GetLocale();

			// Start building the document:
			var doc = new Document();

			// If there are tokens, get the primary object:
			var urlTokens = terminal.UrlTokens;
			if (urlTokens != null && pageAndTokens.TokenValues != null)
			{
				var countA = pageAndTokens.TokenValues.Count;

				if (countA > 0 && countA == urlTokens.Count)
				{
					var primaryToken = urlTokens[countA - 1];
					doc.PrimaryContentTypeId = primaryToken.ContentTypeId;
					doc.PrimaryObjectService = primaryToken.Service;
					doc.PrimaryObjectType = primaryToken.ContentType;
					doc.PrimaryObject = pageAndTokens.PrimaryObject;
				}
			}

			var localeCode = locale.Code.Contains('-') ? locale.Code.Split('-')[0] : locale.Code;

			// Generate the document:
			doc.Path = path;
			doc.Title = page.Title; // Todo: permit {token} values in the title which refer to the primary object.
			doc.SourcePage = page;
			doc.Html
				.With("class", isAdmin ? "admin web" : "ui web")
				.With("lang", localeCode)
				.With("data-theme", isAdmin ? themeConfig.DefaultAdminThemeId : themeConfig.DefaultThemeId);

			if (locale.RightToLeft)
			{
				doc.Html.With("dir", "rtl");
			}

			if (context.RoleId == 1)
			{
				doc.Html.With("data-env", Services.Environment);
			}

			var head = doc.Head;

			var _config = (locale.Id < _configurationTable.Length) ? _configurationTable[locale.Id] : _defaultConfig;

			// True if either config states SSR is on, or the override is indicating it should pre-render:
			var preRenderHtml = preRender.HasValue && preRender.Value == true || _config.PreRender;

			// Charset must be within first 1kb of the header:
			head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, head);

			// Handle all Start Head Scripts in the config.
			HandleCustomScriptList(_config.StartHeadScripts, head);

			var canonicalPath = path;

			if (canonicalPath == "/")
			{
				canonicalPath = "";
			}

			var canonicalUrl = UrlCombine(_frontend.GetPublicUrl(locale.Id), canonicalPath)?.ToLower();

			if (!_config.DisableCanonicalTag)
			{
				head.AppendChild(new DocumentNode("link", true).With("rel", "canonical").With("href", canonicalUrl));
			}

			if (!_config.DisableHrefLangTags)
			{
				// include x-default alternate
				var defaultUrl = GetPathWithoutLocale(canonicalUrl);
				head.AppendChild(new DocumentNode("link", true).With("rel", "alternate").With("hreflang", "x-default").With("href", defaultUrl));

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
						head.AppendChild(new DocumentNode("link", true).With("rel", "alternate").With("hreflang", altLocale.Code).With("href", altUrl));
					}

				}

			}

			head.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "32x32").With("href", "/favicon-32x32.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "16x16").With("href", "/favicon-16x16.png"));

			// Get the main CSS files. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainCssFile = await _frontend.GetMainCss(context == null ? 1 : context.LocaleId);
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", _config.FullyQualifyUrls ? mainCssFile.FqPublicUrl : mainCssFile.PublicUrl));

			if (isAdmin)
			{
				var mainAdminCssFile = await _frontend.GetAdminMainCss(context == null ? 1 : context.LocaleId);
				head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", _config.FullyQualifyUrls ? mainAdminCssFile.FqPublicUrl : mainAdminCssFile.PublicUrl));
			}

			var pageTitle = page.Title;
			var pageDescription = page.Description;

			if (doc.PrimaryObject != null)
			{
				pageTitle = await ReplaceTokens(context, page.Title, doc.PrimaryObject);
				pageDescription = await ReplaceTokens(context, page.Description, doc.PrimaryObject);
			}

			head.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"))
				.AppendChild(new DocumentNode("meta", true).With("name", "description").With("content", pageDescription))
				.AppendChild(new DocumentNode("title").AppendChild(new TextNode(pageTitle)));

			var robotsDirectives = new List<string>();

			if (page.NoIndex)
			{
				robotsDirectives.Add("noindex");
			}

			if (page.NoFollow)
			{
				robotsDirectives.Add("nofollow");
			}

			if (robotsDirectives.Count > 0)
			{
				head.AppendChild(new DocumentNode("meta", true).With("name", "robots").With("content", string.Join(",", robotsDirectives)));
			}

			/*
			 * PWA headers that should only be added if PWA mode is turned on and these files exist
			  .AppendChild(new DocumentNode("link", true).With("rel", "apple-touch-icon").With("sizes", "180x180").With("href", "/apple-touch-icon.png"))
			  .AppendChild(new DocumentNode("link", true).With("rel", "manifest").With("href", "/site.webmanifest"))
			  .AppendChild(new DocumentNode("link", true).With("rel", "mask-icon").With("href", "/safari-pinned-tab.svg").With("color", "#ffffff"))
			 */


#if DEBUG
            // inject dev-specific classes
            head.AppendChild(new DocumentNode("style").With("type", "text/css").AppendChild(new TextNode(
                @"
				a:not([href]), a[href=""""] {
					outline: 8px solid red;
				}
			"
            )));
#endif

			// Handle all End Head tags in the config.
			HandleCustomHeadList(_config.EndHeadTags, head);

			// Handle all End Head Scripts in the config.
			HandleCustomScriptList(_config.EndHeadScripts, head);

			var body = doc.Body;

			string constantPageJson = null;

			if (isAdmin || terminal.Generator == null)
			{
				constantPageJson = page.BodyJson;
			}
			else
			{
				var isConstant = await terminal.Generator.IsConstant();

				if (isConstant)
				{
					// Execute canvas graphs:
					var w = Writer.GetPooled();
					w.Start(null);
					await terminal.Generator.Generate(context, w, pageAndTokens.PrimaryObject);
					constantPageJson = w.ToUTF8String();
					w.Release();
				}
			}

			var writer = Writer.GetPooled();
			writer.Start(null);
			writer.WriteASCII(",\"title\":\"");
			writer.WriteS(page.Title);
			writer.WriteASCII("\",\"id\":");
			writer.WriteS(page.Id);
			writer.Write((byte)'}');
			writer.WriteASCII(",\"tokens\":");
			writer.WriteS((pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null"));
			if (terminal.UrlTokenNamesJson != null)
			{
				writer.WriteASCII(",\"tokenNames\":");
				writer.WriteS(terminal.UrlTokenNamesJson);
			}
			writer.WriteASCII(",\"po\":");

			if (doc.PrimaryObject != null)
			{
				await doc.PrimaryObjectService.ObjectToTypeAndIdJson(context, doc.PrimaryObject, writer);
			}
			else
			{
				writer.WriteS("null");
			}

			writer.WriteS("}");
			var pgStateEnd = writer.AllocatedResult();
			writer.Release();

			body.AppendChild(new TextNode("<div id='react-root'>"));



			if (preRenderHtml && !isAdmin)
			{
				var pgState = constantPageJson == null ? null : "{" + GetAvailableDomains() + "\"page\":{\"bodyJson\":" + constantPageJson + Encoding.UTF8.GetString(pgStateEnd);

				body.AppendChild(new SubstituteNode(  // This is where user and page specific global state will be inserted. It gets substituted in.
					async (Context ctx, Writer writer, PageWithTokens pat) =>
					{
						// Serialise the user context:
						var publicContext = await _contextService.ToJsonString(ctx);

						var pgStateForSSR = pgState;
						var pageJson = constantPageJson;

						if (constantPageJson == null)
						{
							// Re-exec canvas generator.

							var w = Writer.GetPooled();
							w.Start(null);
							await terminal.Generator.Generate(ctx, w, pat.PrimaryObject);
							pageJson = w.ToUTF8String();
							w.Reset(null);

							w.WriteASCII("{\"page\":{\"bodyJson\":");
							w.WriteS(pageJson);
							w.Write(pgStateEnd, 0, pgStateEnd.Length);
							pgStateForSSR = w.ToUTF8String();

							w.Release();
						}

						try
						{
							var preRender = await _canvasRendererService.Render(ctx.LocaleId,
								publicContext,
								pageJson,
								pgStateForSSR,
								RenderMode.Html,
								false
							);

							if (preRender.Failed)
							{
								// JS not loaded yet or otherwise not reachable by the API process.
								writer.WriteS("<h1>Hello! This site is not available just yet.</h1>"
									+ "<p>If you're a developer, check the console for a 'Done handling UI changes' " +
									"message - when that pops up, the UI has been compiled and is ready, then refresh this page.</p>" +
									"<p>Otherwise, this happens when the UI and Admin .js files aren't available to the API.</p>");
							}
							else
							{
								writer.WriteS(preRender.Body);
								writer.WriteS("</div><script>window.gsInit=");
								writer.WriteS(publicContext);
								writer.WriteS(";</script><script type='application/json' id='pgState'>");
								writer.WriteS(pgStateForSSR);
								writer.WriteS("</script>");
							}
						}
						catch (Exception e)
						{
							// SSR failed. Return nothing and let the JS handle it for itself.
							Log.Error(LogTag, e, "Unable to render a page with SSR.");
						}
					}
				));
			}
			else
			{
				body.AppendChild(new TextNode("</div>"));

				// Add the global state init substitution node:
				body.AppendChild(
					new DocumentNode("script")
					.AppendChild(new TextNode("window.gsInit="))
					.AppendChild(new SubstituteNode(  // This is where user and page specific global state will be inserted. It gets substituted in.
						async (Context ctx, Writer writer, PageWithTokens pat) =>
						{
							// Serialise the user context:
							await _contextService.ToJsonString(ctx, writer);
						}
					))
					.AppendChild(new TextNode(";"))
				);

				body.AppendChild(new TextNode("<script type='application/json' id='pgState'>{" + GetAvailableDomains() + "\"page\":{\"bodyJson\":"));

				if (constantPageJson != null)
				{
					body.AppendChild(
						new RawBytesNode(
								Encoding.UTF8.GetBytes(constantPageJson)
						)
					);
				}
				else
				{
					body.AppendChild(
						new SubstituteNode(
								async (Context ctx, Writer writer, PageWithTokens pat) =>
								{
									// Run the graph engine.
									await terminal.Generator.Generate(ctx, writer, pat.PrimaryObject);
								}
						)
					);
				}

				body.AppendChild(
					new RawBytesNode(
							pgStateEnd
					)
				);

				body.AppendChild(new TextNode("</script>"));
			}

			// Handle all start body JS scripts
			HandleCustomScriptList(_config.StartBodyJs, body);

			// Handle all Before Main JS scripts
			HandleCustomScriptList(_config.BeforeMainJs, body);

			body.AppendChild(
					new DocumentNode("script")
					.AppendChild(_configJson)
					.AppendChild(new TextNode(_frontend.GetServiceUrls(locale.Id)))
					.AppendChild(new TextNode(_frontend.InlineJavascriptHeader))
			);

			if (isAdmin)
			{
				// Get the main admin JS file. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
				// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
				// Admin modules must be added to page before frontend ones, as the frontend file includes UI/Start and the actual start call.
				var mainAdminJsFile = await _frontend.GetAdminMainJs(context == null ? 1 : context.LocaleId);
				var mainAdminJs = new DocumentNode("script").With("src", _config.FullyQualifyUrls ? mainAdminJsFile.FqPublicUrl : mainAdminJsFile.PublicUrl);
				body.AppendChild(mainAdminJs);

				// Same also for the email modules:
				var mainEmailJsFile = await _frontend.GetEmailMainJs(context == null ? 1 : context.LocaleId);
				var mainEmailJs = new DocumentNode("script").With("src", _config.FullyQualifyUrls ? mainEmailJsFile.FqPublicUrl : mainEmailJsFile.PublicUrl);
				body.AppendChild(mainEmailJs);
			}

			// Get the main JS file. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainJsFile = await _frontend.GetMainJs(context == null ? 1 : context.LocaleId);

			var mainJs = new DocumentNode("script").With("src", _config.FullyQualifyUrls ? mainJsFile.FqPublicUrl : mainJsFile.PublicUrl);

			if (_config.DeferMainJs)
			{
				mainJs.With("defer").With("async");
			}

			doc.MainJs = mainJs;
			body.AppendChild(mainJs);

			// Handle all After Main JS scripts
			HandleCustomScriptList(_config.AfterMainJs, body);

			// Handle all End Body JS scripts
			HandleCustomScriptList(_config.EndBodyJs, body);

			// Trigger an event for things to modify the html however they need:
			doc = await Events.Page.Generated.Dispatch(context, doc);

			// Build the flat HTML for the page:
			var flatNodes = doc.Flatten();

			if (pageAndTokens.StatusCode != 404)
			{

				// As it's being cached, may need to cache content type as well:
				/*
				if (doc.PrimaryObject != null)
				{
					if (!eventHandlersByContentTypeId.ContainsKey(doc.PrimaryContentTypeId))
					{
						// Mark as added:
						eventHandlersByContentTypeId[doc.PrimaryContentTypeId] = true;

						// Get the event group:
						var evtGroup = doc.PrimaryObjectService.GetEventGroup();

						var methodInfo = GetType().GetMethod(nameof(AttachPrimaryObjectEventHandler));

						// Invoke attach:
						var setupType = methodInfo.MakeGenericMethod(new Type[] {
								doc.PrimaryObjectService.ServicedType,
								doc.PrimaryObjectService.IdType
							});

						setupType.Invoke(this, new object[] {
								evtGroup
							});
					}
				}
                */

				lock (cacheLock)
				{
					if (cache == null)
					{
						cache = new ConcurrentDictionary<string, CachedPageData>[context.LocaleId];
					}
					else if (cache.Length < context.LocaleId)
					{
						Array.Resize(ref cache, (int)context.LocaleId);
					}

					var localeCache = cache[context.LocaleId - 1];

					if (localeCache == null)
					{
						cache[context.LocaleId - 1] = localeCache = new ConcurrentDictionary<string, CachedPageData>();
					}
					localeCache[path] = new CachedPageData(flatNodes);
				}
			}
			// Note: Although gzip does support multiple concatenated gzip blocks, browsers do not implement this part of the gzip spec correctly.
			// Unfortunately that means no part of the stream can be pre-compressed; must compress the whole thing and output that.

			// Swap all the TextNodes for byte blocks. 
			// Virtually all of the request is pre-utf8 encoded and remains that way for multiple requests, up until the cache clears.
			// The only place it isn't the case is on any substitution nodes, such as the spot where user state is swapped in per-request.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is TextNode node1)
				{
					var bytes = Encoding.UTF8.GetBytes(node1.TextContent);
					flatNodes[i] = new RawBytesNode(bytes);
				}
			}

			return flatNodes;
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
						if (currentChar == '{')
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
					var noBrackets = token.Substring(1, token.Length - 2);

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

					// We need to swap out the string with the value.
					var strValue = value.ToString();

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
		/// Adds the primary object event handlers to the given event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="evtGroup"></param>
		public void AttachPrimaryObjectEventHandler<T, ID>(EventGroup<T, ID> evtGroup)
			 where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			evtGroup.Received.AddEventListener(async (Context ctx, T content, int mode) =>
			{
				if (content != null && cache != null)
				{
					// Get the URL for this thing. If it's cached, clear it:
					var url = await _pages.GetUrl(ctx, content, UrlGenerationScope.UI);

					if (!string.IsNullOrEmpty(url))
					{
						// Clear from each locale cache.
						for (var i = 0; i < cache.Length; i++)
						{
							var localeCache = cache[i];
							if (localeCache == null)
							{
								continue;
							}

							// Request a removal:
							localeCache.Remove(url, out _);
						}
					}
				}

				return content;
			});

			evtGroup.AfterUpdate.AddEventListener(async (Context ctx, T content) =>
			{
				if (content != null && cache != null)
				{
					// Get the URL for this thing. If it's cached, clear it:
					var url = await _pages.GetUrl(ctx, content, UrlGenerationScope.UI);

					if (!string.IsNullOrEmpty(url))
					{
						// Clear from each locale cache.
						for (var i = 0; i < cache.Length; i++)
						{
							var localeCache = cache[i];
							if (localeCache == null)
							{
								continue;
							}

							// Request a removal:
							localeCache.Remove(url, out _);
						}
					}
				}

				return content;
			});
		}

		/// <summary>
		/// Handles adding a custom script list (if there even is one set) into the given node. They'll be appended.
		/// </summary>
		private void HandleCustomHeadList(List<HeadTag> list, DocumentNode head, bool permitRemote = true)
		{
			if (list == null)
			{
				return;
			}

			foreach (var headTag in list)
			{
				DocumentNode node;

				// is the headTag a link or meta?
				if (headTag.Rel != null)
				{
					if (!permitRemote)
					{
						continue;
					}

					node = new DocumentNode("link", true);
					node.With("rel", headTag.Rel);

					if (!string.IsNullOrEmpty(headTag.As))
					{
						node.With("as", headTag.As);
						node.With("crossorigin", headTag.CrossOrigin);
					}

					if (headTag.Href != null)
					{
						node.With("href", headTag.Href);
					}
				}
				else
				{
					node = new DocumentNode("meta", true);
					node.With("name", headTag.Name);

					if (headTag.Content != null)
					{
						node.With("content", headTag.Content);
					}
				}

				if (headTag.Attributes != null)
				{
					foreach (var kvp in headTag.Attributes)
					{
						node.With(kvp.Key, kvp.Value);
					}
				}

				head.AppendChild(node);
			}

		}

		/// <summary>
		/// Handles adding a custom script list (if there even is one set) into the given node. They'll be appended.
		/// </summary>
		private void HandleCustomScriptList(List<BodyScript> list, DocumentNode body, bool permitRemote = true)
		{
			if (list == null)
			{
				return;
			}

			foreach (var bodyScript in list)
			{
				//Does this script have content?
				DocumentNode node;

				if (!string.IsNullOrEmpty(bodyScript.NoScriptText))
				{
					node = new DocumentNode("noscript").AppendChild(new TextNode(bodyScript.NoScriptText));
				}
				else if (!string.IsNullOrEmpty(bodyScript.Content))
				{
					node = new DocumentNode("script").AppendChild(new TextNode(bodyScript.Content));
				}
				else
				{

					if (!permitRemote)
					{
						continue;
					}

					node = new DocumentNode("script");

					// Expect an src:
					if (bodyScript.Src != null)
					{
						node.With("src", bodyScript.Src);
					}
				}

				if (bodyScript.Async)
				{
					node.With("async");
				}

				if (bodyScript.Defer)
				{
					node.With("defer");
				}

				if (bodyScript.Type != null)
				{
					node.With("type", bodyScript.Type);
				}

				if (bodyScript.Id != null)
				{
					node.With("id", bodyScript.Id);
				}

				if (bodyScript.Attributes != null)
				{
					foreach (var kvp in bodyScript.Attributes)
					{
						node.With(kvp.Key, kvp.Value);
					}
				}

				body.AppendChild(node);
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

		private async ValueTask<string> WriteCompressedAndHash(Context context, List<DocumentNode> flatNodes, Stream str, PageWithTokens pat)
		{
			var outputStream = new GZipStream(str, CompressionMode.Compress);

			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{

				var writer = Writer.GetPooled();
				writer.Start(null);

				// Build the final output.
				for (var i = 0; i < flatNodes.Count; i++)
				{
					var node = flatNodes[i];

					if (node is RawBytesNode node1)
					{
						md5.TransformBlock(node1.Bytes, 0, node1.Bytes.Length, null, 0);
						await outputStream.WriteAsync(node1.Bytes);
					}
					else
					{
						// Substitute node
						var subNode = (SubstituteNode)node;
						await subNode.OnGenerate(context, writer, pat);
						CopyToMd5(writer, md5);
						await writer.CopyToAsync(outputStream);
						writer.Reset(null);
					}
				}

				writer.Release();
				await outputStream.FlushAsync();
				await outputStream.DisposeAsync();

				return CreateMd5HashString(md5);
			}
		}

		private async ValueTask WriteWriterCompressed(Writer writer, Stream str)
		{
			var outputStream = new GZipStream(str, CompressionMode.Compress);
			await writer.CopyToAsync(outputStream);
			await outputStream.FlushAsync();
			await outputStream.DisposeAsync();
		}

		private async ValueTask WriteCompressed(Context context, List<DocumentNode> flatNodes, Stream str, PageWithTokens pat)
		{
			var outputStream = new GZipStream(str, CompressionMode.Compress);

			var writer = Writer.GetPooled();
			writer.Start(null);

			// Build the final output.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is RawBytesNode node1)
				{
					await outputStream.WriteAsync(node1.Bytes);
				}
				else
				{
					// Substitute node
					var subNode = (SubstituteNode)node;
					await subNode.OnGenerate(context, writer, pat);
					await writer.CopyToAsync(outputStream);
					writer.Reset(null);
				}
			}

			writer.Release();
			await outputStream.FlushAsync();
			await outputStream.DisposeAsync();
		}

		/// <summary>
		/// Obtains bytes of a memory cached anonymous page. These pages are stored in memory compressed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public async ValueTask<byte[]> GetCachedAnonymousPage(Context context, string path)
		{
			var _config = (context.LocaleId < _configurationTable.Length) ? _configurationTable[context.LocaleId] : _defaultConfig;

			if (!_config.CacheAnonymousPages)
			{
				// The cache is turned off.
				throw new Exception("Using this feature requires turning on CacheAnonymousPages in HtmlService config.");
			}

			var pageAndTokens = await _pages.GetPage(context, null, path, QueryString.Empty, true);

			ConcurrentDictionary<string, CachedPageData> localeCache;

			lock (cacheLock)
			{
				if (cache == null)
				{
					cache = new ConcurrentDictionary<string, CachedPageData>[context.LocaleId];
				}
				else if (cache.Length < context.LocaleId)
				{
					Array.Resize(ref cache, (int)context.LocaleId);
				}

				localeCache = cache[context.LocaleId - 1];
				if (localeCache == null)
				{
					cache[context.LocaleId - 1] = localeCache = new ConcurrentDictionary<string, CachedPageData>();
				}
			}

			var page = pageAndTokens.Page;
			var cachePath = (pageAndTokens.StatusCode == 404) ? (page == null ? "/404" : page.Url) : path;

			CachedPageData cpd;

			if (!localeCache.TryGetValue(cachePath, out cpd) || cpd.AnonymousCompressedPage == null)
			{
				// Generate the page now and ensure it is stored in the cache.
				var flatNodes = await RenderPage(context, pageAndTokens, cachePath, true);

				using var ms = new MemoryStream();
				var hash = await WriteCompressedAndHash(context, flatNodes, ms, pageAndTokens);

				cpd = new CachedPageData(flatNodes, ms.ToArray(), hash, _config.CacheMaxAge);

				localeCache[cachePath] = cpd;
			}

			return cpd.AnonymousCompressedPage;
		}

		/// <summary>
		/// Generates the base HTML for the given site relative url.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="updateContext"></param>
		/// <param name="isAdmin"></param>
		/// <returns></returns>
		public async ValueTask BuildPage(Context context, HttpRequest request, HttpResponse response, bool updateContext = false, bool isAdmin = false)
		{
			string path = request.Path;
			Microsoft.AspNetCore.Http.QueryString searchQuery = request.QueryString;

			// If services have not finished starting up yet, wait.
			var svcWaiter = Services.StartupWaiter;

			if (svcWaiter != null)
			{
				await svcWaiter.Task;
			}

			var _config = (context.LocaleId < _configurationTable.Length) ? _configurationTable[context.LocaleId] : _defaultConfig;

			if (_config.ForceLowercaseUrls)
			{
				bool cotainsUpperCase = path.Any(char.IsUpper);
				bool hasTrailingSlash = path.Length > 1 && path[path.Length - 1] == '/';

				if (cotainsUpperCase)
				{
					var newLocation = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
					newLocation = newLocation.ToLower();
					response.StatusCode = 301;
					response.Headers.Location = newLocation;
					return;
				}
				else if (hasTrailingSlash)
				{
					var newLocation = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
					newLocation = newLocation.Remove(newLocation.Length - 1, 1);
					response.StatusCode = 301;
					response.Headers.Location = newLocation;
					return;
				}
			}

			var pageAndTokens = await _pages.GetPage(context, request.Host.Value, path, searchQuery, true);

			bool pullFromCache = (
				!_config.DisablePageCache &&
				_config.CacheMaxAge > 0 &&
				_config.CacheAnonymousPages &&
				!isAdmin &&
				context.UserId == 0 &&
				context.RoleId == 6
			);

			if (pullFromCache)
			{
				pullFromCache = await Events.Context.CanUseCache.Dispatch(context, pullFromCache);
			}

			response.ContentType = "text/html";
			response.Headers["Content-Encoding"] = "gzip";

			List<DocumentNode> flatNodes = null;

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
					flatNodes = GenerateBlockPage();
					response.Headers["Cache-Control"] = "no-store";
					response.Headers["Pragma"] = "no-cache";
					await WriteCompressed(context, flatNodes, response.Body, pageAndTokens);
					return;
				}
				else
				{
					// Block wall is active but cannot cache this state:
					pullFromCache = false;
				}
			}

			if (pullFromCache)
			{
				response.Headers["Cache-Control"] = _cacheControlHeader;
			}
			else
			{
				response.Headers["Cache-Control"] = "no-store";
				response.Headers["Pragma"] = "no-cache";
			}

			if (pageAndTokens.RedirectTo != null)
			{
				// Redirecting to the given url, as a 302:
				response.Headers["Location"] = pageAndTokens.RedirectTo;
				response.StatusCode = 302;
				return;
			}

			response.StatusCode = pageAndTokens.StatusCode;

			await Events.Page.BeforeNavigate.Dispatch(context, pageAndTokens.Page, path);

			if (updateContext && context.UserId != 0)
			{
				// Update the token:
				context.SendToken(response);
			}

			if (pullFromCache)
			{
				ConcurrentDictionary<string, CachedPageData> localeCache;

				lock (cacheLock)
				{
					if (cache == null)
					{
						cache = new ConcurrentDictionary<string, CachedPageData>[context.LocaleId];
					}
					else if (cache.Length < context.LocaleId)
					{
						Array.Resize(ref cache, (int)context.LocaleId);
					}

					localeCache = cache[context.LocaleId - 1];

					if (localeCache == null)
					{
						cache[context.LocaleId - 1] = localeCache = new ConcurrentDictionary<string, CachedPageData>();
					}
				}

				CachedPageData cpd;

				var page = pageAndTokens.Page;

				// basing anonPageUrl on pageAndTokens.UrlInfo.AllocateString() strips /en-gb from the URL;
				// this then causes issues when the cached non-region version of the page is served for an /en-gb request
				// (as the compressed page includes the wrong URL for the canonical tag)
				//var anonPageUrl = (pageAndTokens.StatusCode == 404) ? (page == null ? "/404" : page.Url) : pageAndTokens.UrlInfo.AllocateString();
				var anonPageUrl = (pageAndTokens.StatusCode == 404) ? (page == null ? "/404" : page.Url) : path;

				if (!localeCache.TryGetValue(anonPageUrl, out cpd) || cpd.AnonymousCompressedPage == null)
				{
					// Generate the page now and ensure it is stored in the cache.
					flatNodes = await RenderPage(context, pageAndTokens, anonPageUrl, true);

					using var ms = new MemoryStream();
					var hash = await WriteCompressedAndHash(context, flatNodes, ms, pageAndTokens);

					cpd = new CachedPageData(flatNodes, ms.ToArray(), hash, _config.CacheMaxAge);

					localeCache[anonPageUrl] = cpd;
				}

				if (cpd.Hash != null && cpd.Hash.Length > 0)
				{
					response.Headers.ETag = cpd.Hash;
					response.Headers["last-modified"] = cpd.LastModifiedHeader;
					response.Headers["expires"] = cpd.ExpiresHeader;
				}

				// Copy direct to target stream.
				await response.Body.WriteAsync(cpd.AnonymousCompressedPage);
				return;
			}

			if (flatNodes == null)
			{
				flatNodes = await RenderPage(context, pageAndTokens, path);
			}

			await WriteCompressed(context, flatNodes, response.Body, pageAndTokens);
		}

		/// <summary>
		/// Generates the base HTML for native mobile apps.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="responseStream"></param>
		/// <returns></returns>
		public async ValueTask BuildHeaderOnly(Context context, Stream responseStream)
		{
			// Does the locale exist? (intentionally using a blank context here - it must only vary by localeId)
			var locale = await context.GetLocale();

			if (locale == null)
			{
				// Dodgy locale - quit:
				return;
			}

			List<DocumentNode> flatNodes = await RenderHeaderOnly(context, locale);

			// Build the final output.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is RawBytesNode node1)
				{
					await responseStream.WriteAsync(node1.Bytes);
				}
			}

			await responseStream.FlushAsync();
		}

		/// <summary>
		/// Generates the base HTML for native mobile apps.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="responseStream"></param>
		/// <param name="mobileMeta"></param>
		/// <returns></returns>
		public async ValueTask BuildMobileHomePage(Context context, Stream responseStream, MobilePageMeta mobileMeta)
		{

			// Does the locale exist? (intentionally using a blank context here - it must only vary by localeId)
			var locale = await _localeService.Get(new Context(), mobileMeta.LocaleId, DataOptions.IgnorePermissions);

			if (locale == null)
			{
				// Dodgy locale - quit:
				return;
			}

			List<DocumentNode> flatNodes = await RenderNativeAppPage(context, locale, mobileMeta);

			// Build the final output.
			for (var i = 0; i < flatNodes.Count; i++)
			{
				var node = flatNodes[i];

				if (node is RawBytesNode node1)
				{
					await responseStream.WriteAsync(node1.Bytes);
				}
			}

			await responseStream.FlushAsync();
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
