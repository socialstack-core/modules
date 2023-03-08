using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
using Api.Permissions;
using Microsoft.AspNetCore.Http;
using Api.Themes;
using Api.Startup;
using System.Reflection;

namespace Api.Pages
{
	/// <summary>
	/// Handles the main generation of HTML from the index.html base template at UI/public/index.html and Admin/public/index.html
	/// </summary>

	public partial class HtmlService : AutoService
    {
		private readonly PageService _pages;
		private readonly CanvasRendererService _canvasRendererService;
		private readonly HtmlServiceConfig _config;
		private readonly FrontendCodeService _frontend;
		private readonly ContextService _contextService;
		private readonly ThemeService _themeService;
		private readonly LocaleService _localeService;
		private readonly ConfigurationService _configurationService;

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

			_config = GetConfig<HtmlServiceConfig>();

			var pathToUIDir = AppSettings.Configuration["UI"];

			if (string.IsNullOrEmpty(pathToUIDir))
			{
				pathToUIDir = "UI/public";
			}

			_config.OnChange += () => {
				cache = null;
				return new ValueTask();
			};

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

			Events.Page.Received.AddEventListener((Context context, Page page, int mode) => {

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

			Events.Translation.Received.AddEventListener((Context context, Translation tr, int mode) => {

				// Doesn't matter what the change was for now - we'll wipe the whole cache.
				cache = null;

				return new ValueTask<Translation>(tr);
			});
		}

		/// <summary>
		/// The frontend version.
		/// </summary>
		/// <returns></returns>
		public long Version {
			get
			{
				return _frontend.Version;
			}
		}

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
		public byte[] GetRobotsTxt()
		{
			if (_robots == null)
			{
				var sb = new StringBuilder();
				sb.Append("User-agent: *\r\n");
				sb.Append("Disallow: /v1\r\n\r\n");
				// sb.Append("Sitemap: /sitemap.xml");

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
		/// Url -> nodes that are as pre-generated as possible. Locale sensitive; indexed by locale.Id-1.
		/// </summary>
		private Dictionary<string, List<DocumentNode>>[] cache = null;

		/// <summary>
		/// Renders the state only of a page to a JSON string.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pageAndTokens"></param>
		/// <param name="path"></param>
		/// <param name="responseStream"></param>
		/// <returns></returns>
		public async ValueTask RenderState(Context context, PageWithTokens pageAndTokens, string path, Stream responseStream = null)
		{
			object primaryObject = null;
			AutoService primaryService = null;

			if (pageAndTokens.Tokens != null && pageAndTokens.TokenValues != null)
			{
				var countA = pageAndTokens.TokenValues.Count;

				if (countA > 0 && countA == pageAndTokens.Tokens.Count)
				{
					var primaryToken = pageAndTokens.Tokens[countA - 1];

					if (primaryToken.ContentType != null)
					{
						primaryService = primaryToken.Service;

						if (primaryToken.IsId)
						{
							if (ulong.TryParse(pageAndTokens.TokenValues[countA - 1], out ulong primaryObjectId))
							{
								primaryObject = await primaryToken.Service.GetObject(context, primaryObjectId);
							}
						}
						else
						{
							primaryObject = await primaryToken.Service.GetObject(context, primaryToken.FieldName, pageAndTokens.TokenValues[countA - 1]);
						}
					}
				}
			}

			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{\"page\":");
			await _pages.ToJson(context, pageAndTokens.Page, writer);
			writer.WriteASCII(",\"data\":");

			if (_config.PreRender)
			{
				var preRender = await _canvasRendererService.Render(context, pageAndTokens.Page.BodyJson, new PageState() {
					Tokens = pageAndTokens.TokenValues,
					TokenNames = pageAndTokens.TokenNames,
					PrimaryObject = primaryObject
				}, path, true, RenderMode.None);

				writer.WriteASCII(preRender.Data);
			}
			else
			{
				writer.WriteASCII("null");
			}

			var cfgBytes = _configurationService.GetLatestFrontendConfigBytesJson();

			if (cfgBytes != null)
			{
				writer.WriteASCII(",\"config\":");
				writer.WriteNoLength(cfgBytes);
			}

			writer.WriteASCII(",\"tokenNames\":");
			writer.WriteS(pageAndTokens.TokenNamesJson);
			writer.WriteASCII(",\"tokens\":");
			writer.WriteS(pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null");

			if (primaryObject != null)
			{

				writer.WriteASCII(",\"po\":");
				await primaryService.ObjectToTypeAndIdJson(context, primaryObject, writer);
			}

			if (pageAndTokens.Page != null && !string.IsNullOrEmpty(pageAndTokens.Page.Title))
			{
				writer.WriteASCII(",\"title\":");
				var titleStr = pageAndTokens.Page.Title;

				if (primaryObject != null)
				{
					writer.WriteEscaped(ReplaceTokens(titleStr, primaryObject));
				}
				else
				{
					writer.WriteEscaped(titleStr);
				}
			}

			writer.Write((byte)'}');
			await writer.CopyToAsync(responseStream);
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
			doc.Head.AppendChild(new DocumentNode("meta", true).With("viewport", "width=device-width, initial-scale=1"));
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

			var form = new DocumentNode("form").With("action", "/").With("method", "GET").With("id", "content_pwd_form").With("class", "d-flex justify-content-center mt-5");
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
			paragraph.AppendChild(new DocumentNode("br", true));
			paragraph.AppendChild(new TextNode("Please note, this is not the same as a user account password."));
			container.AppendChild(paragraph);

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
				"Ruh-roh Rorge!"
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

			// Generate the document:
			var doc = new Document();
			doc.Path = "/";
			doc.Title = ""; // Todo: permit {token} values in the title which refer to the primary object.
			doc.Html.With("class", "ui head-only").With("lang", locale.Code)
				.With("data-theme", themeConfig.DefaultAdminThemeId);

			if (locale.RightToLeft)
			{
				doc.Html.With("dir", "rtl");
			}

			var head = doc.Head;

			// If there are tokens, get the primary object:

			// Charset must be within first 1kb of the header:
			head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, head, false);

			head.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "32x32").With("href", "/favicon-32x32.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "16x16").With("href", "/favicon-16x16.png"));

			// Get the main CSS files. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainCssFile = await _frontend.GetMainCss(context == null ? 1 : context.LocaleId);
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", _config.FullyQualifyUrls ? mainCssFile.FqPublicUrl : mainCssFile.PublicUrl));
				
			var mainAdminCssFile = await _frontend.GetAdminMainCss(context == null ? 1 : context.LocaleId);
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", _config.FullyQualifyUrls ? mainAdminCssFile.FqPublicUrl : mainAdminCssFile.PublicUrl));
			head.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", "#ffffff"))
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
		private async ValueTask<List<DocumentNode>> RenderMobilePage(Context context, Locale locale, MobilePageMeta pageMeta)
		{
			var themeConfig = _themeService.GetConfig();

			// Generate the document:
			var doc = new Document();
			doc.Path = "/";
			doc.Title = ""; // Todo: permit {token} values in the title which refer to the primary object.
			doc.Html
				.With("class", "ui mobile").With("lang", locale.Code)
				.With("data-theme", themeConfig.DefaultThemeId);

			if (locale.RightToLeft)
			{
				doc.Html.With("dir", "rtl");
			}

			var head = doc.Head;

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
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"));

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
		/// <returns></returns>
		private async ValueTask<List<DocumentNode>> RenderPage(Context context, PageWithTokens pageAndTokens, string path)
		{
			var isAdmin = path.StartsWith("/en-admin");
			List<DocumentNode> flatNodes;

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
			if (cache != null && context.LocaleId <= cache.Length && !pageAndTokens.Multiple)
			{
				var localeCache = cache[context.LocaleId - 1];

				if(localeCache != null && localeCache.TryGetValue(path, out flatNodes))
				{
					return flatNodes;
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
			
			var page = pageAndTokens.Page;
			
			// Generate the document:
			var doc = new Document();
			doc.Path = path;
			doc.Title = page.Title; // Todo: permit {token} values in the title which refer to the primary object.
			doc.SourcePage = page;
			doc.Html
				.With("class", isAdmin ? "admin web" : "ui web")
				.With("lang", locale.Code)
				.With("data-theme", isAdmin ? themeConfig.DefaultAdminThemeId : themeConfig.DefaultThemeId);

			if (locale.RightToLeft)
			{
				doc.Html.With("dir", "rtl");
			}

			var head = doc.Head;

			// If there are tokens, get the primary object:
			if (pageAndTokens.Tokens != null && pageAndTokens.TokenValues != null)
			{
				var countA = pageAndTokens.TokenValues.Count;

				if (countA > 0 && countA == pageAndTokens.Tokens.Count)
				{
					var primaryToken = pageAndTokens.Tokens[countA - 1];
					doc.PrimaryContentTypeId = primaryToken.ContentTypeId;
					doc.PrimaryObjectService = primaryToken.Service;
					doc.PrimaryObjectType = primaryToken.ContentType;

					if (primaryToken.ContentType != null)
                    {
						if (primaryToken.IsId)
						{
							if (ulong.TryParse(pageAndTokens.TokenValues[countA - 1], out ulong primaryObjectId))
							{
								doc.PrimaryObject = await primaryToken.Service.GetObject(context, primaryObjectId);
							}
						}
						else
						{
							doc.PrimaryObject = await primaryToken.Service.GetObject(context, primaryToken.FieldName, pageAndTokens.TokenValues[countA - 1]);
						}
					}
				}
			}

			// Charset must be within first 1kb of the header:
			head.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"));

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, head);

			// Handle all Start Head Scripts in the config.
			HandleCustomScriptList(_config.StartHeadScripts, head);

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

			head.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"))
				.AppendChild(new DocumentNode("meta", true).With("name", "description").With("content", ReplaceTokens(page.Description, doc.PrimaryObject)))
				.AppendChild(new DocumentNode("title").AppendChild(new TextNode(ReplaceTokens(page.Title, doc.PrimaryObject))));

			/*
			 * PWA headers that should only be added if PWA mode is turned on and these files exist
			  .AppendChild(new DocumentNode("link", true).With("rel", "apple-touch-icon").With("sizes", "180x180").With("href", "/apple-touch-icon.png"))
			  .AppendChild(new DocumentNode("link", true).With("rel", "manifest").With("href", "/site.webmanifest"))
			  .AppendChild(new DocumentNode("link", true).With("rel", "mask-icon").With("href", "/safari-pinned-tab.svg").With("color", "#ffffff"))
			 */

			// Handle all End Head tags in the config.
			HandleCustomHeadList(_config.EndHeadTags, head);

			// Handle all End Head Scripts in the config.
			HandleCustomScriptList(_config.EndHeadScripts, head);
			
			var reactRoot = new DocumentNode("div").With("id", "react-root");

			var body = doc.Body;
			body.AppendChild(reactRoot);

			if (_config.PreRender)
			{
				try
				{

					var preRender = await _canvasRendererService.Render(context, page.BodyJson, new PageState() {
						Tokens = pageAndTokens.TokenValues,
						TokenNames = pageAndTokens.TokenNames,
						PrimaryObject = doc.PrimaryObject
					}, path, true);

					if (preRender.Failed)
					{
						// JS not loaded yet or otherwise not reachable by the API process.
						reactRoot.AppendChild(new TextNode(
							"<h1>Hello! This site is not available just yet.</h1>"
							+ "<p>If you're a developer, check the console for a 'Done handling UI changes' " +
							"message - when that pops up, the UI has been compiled and is ready, then refresh this page.</p>" +
							"<p>Otherwise, this happens when the UI and Admin .js files aren't available to the API.</p>"
						));
					}
					else
					{
						reactRoot.AppendChild(new TextNode(preRender.Body));

						// Add the data which populates the initial cache. This is important for a variety of reasons, but it primarily ensures that e.g. site template data doesn't have to be immediately requested.
						// If it did have to be immediately requested, the first render run results in a totally blank output, which conflicts with what the server has already rendered.
						// The end result of the conflict is it gets duplicated in the DOM and server effort is largely wasted.

						var writer = Writer.GetPooled();
						writer.Start(null);

						writer.WriteS("window.pgState={\"page\":");
						await _pages.ToJson(context, pageAndTokens.Page, writer);
						writer.WriteS(",\"tokens\":");
						writer.WriteS(",\"data\":");
						writer.WriteS(preRender.Data);
						writer.WriteS((pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null"));
						writer.WriteS(",\"tokenNames\":");
						writer.WriteS(pageAndTokens.TokenNamesJson);
						writer.WriteS(",\"po\":");

						if (doc.PrimaryObject != null)
						{
							await doc.PrimaryObjectService.ObjectToTypeAndIdJson(context, doc.PrimaryObject, writer);
						}
						else
						{
							writer.WriteS("null");
						}

						writer.WriteS("};");
						var pgState = writer.AllocatedResult();
						writer.Release();

						body.AppendChild(
							new DocumentNode("script")
							.AppendChild(
								new RawBytesNode(
									pgState
								)
							)
						);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
			else
			{
				var writer = Writer.GetPooled();
				writer.Start(null);

				writer.WriteS("window.pgState={\"page\":");
				await _pages.ToJson(context, pageAndTokens.Page, writer);
				writer.WriteS(",\"tokens\":");
				writer.WriteS((pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null"));
				writer.WriteS(",\"tokenNames\":");
				writer.WriteS(pageAndTokens.TokenNamesJson);
				writer.WriteS(",\"po\":");

				if (doc.PrimaryObject != null)
				{
					await doc.PrimaryObjectService.ObjectToTypeAndIdJson(context, doc.PrimaryObject, writer);
				}
				else
				{
					writer.WriteS("null");
				}

				writer.WriteS("};");
				var pgState = writer.AllocatedResult();
				writer.Release();

				body.AppendChild(
					new DocumentNode("script")
					.AppendChild(
						new RawBytesNode(
							 pgState
						)
					)
				);
			}
			
			// Handle all start body JS scripts
			HandleCustomScriptList(_config.StartBodyJs, body);
			
			// Still add the global state init substitution node:
			body.AppendChild(
				new DocumentNode("script")
				.AppendChild(_configJson)
				.AppendChild(new TextNode("window.gsInit="))
				.AppendChild(new SubstituteNode(  // This is where user and page specific global state will be inserted. It gets substituted in.
					async (Context ctx) => {
						return await BuildUserGlobalStateJs(ctx);
					}
				))
				.AppendChild(new TextNode(";"))
			);
			


			// Handle all Before Main JS scripts
			HandleCustomScriptList(_config.BeforeMainJs, body);

			body.AppendChild(
					new DocumentNode("script")
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
			doc.MainJs = mainJs;
			body.AppendChild(mainJs);
			
			// Handle all After Main JS scripts
			HandleCustomScriptList(_config.AfterMainJs, body);
			
			// Handle all End Body JS scripts
			HandleCustomScriptList(_config.EndBodyJs, body);
			
			// Trigger an event for things to modify the html however they need:
			doc = await Events.Page.Generated.Dispatch(context, doc);

			// Build the flat HTML for the page:
			flatNodes = doc.Flatten();

			lock(cacheLock)
			{
				if (cache == null)
				{
					cache = new Dictionary<string, List<DocumentNode>>[context.LocaleId];
				}
				else if (cache.Length < context.LocaleId)
				{
					Array.Resize(ref cache, (int)context.LocaleId);
				}

				var localeCache = cache[context.LocaleId - 1];

				if (localeCache == null)
				{
					cache[context.LocaleId - 1] = localeCache = new Dictionary<string, List<DocumentNode>>();
				}

				// As it's being cached, may need to cache content type as well:
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

				localeCache[path] = flatNodes;
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
		/// Used to replace tokens within a string with Primary object content
		/// </summary>
		/// <param name="pageTitle"></param>
		/// <param name="primaryObject"></param>
		/// <returns></returns>
		public string ReplaceTokens(string pageTitle, object primaryObject)
		{
			if (pageTitle == null)
			{
				return pageTitle;
			}

			// We need to find out if there is a token to be handled.
			if (primaryObject != null)
			{

				var mode = 0; // 0= text, 1 = inside a {token.field}
				List<string> tokens = new List<string>();
				var storedIndex = 0;

				// we have one. Now, do we have a meta file value stored within the title?
				for (var i = 0; i < pageTitle.Length; i++)
				{
					var currentChar = pageTitle[i];
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
							var token = pageTitle.Substring(storedIndex, i - storedIndex + 1);
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
					var systemType = Database.ContentTypes.GetType(content);

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
					pageTitle = pageTitle.Replace(token, value.ToString());
				}
			}
			return pageTitle;
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
							localeCache.Remove(url);
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
							localeCache.Remove(url);
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
			if(list == null)
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
			if(list == null)
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

		/// <summary>
		/// Generates the base HTML for the given site relative url.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="compress"></param>
		/// <param name="updateContext"></param>
		/// <returns></returns>
		public async ValueTask BuildPage(Context context, HttpRequest request, HttpResponse response, bool compress = true, bool updateContext = false)
		{
			string path = request.Path;
			Microsoft.AspNetCore.Http.QueryString searchQuery = request.QueryString;

			response.ContentType = "text/html";
			response.Headers["Cache-Control"] = "no-store";

			if (compress)
			{
				response.Headers["Content-Encoding"] = "gzip";
			}

			var pageAndTokens = await _pages.GetPage(context, request.Host.Value, path, searchQuery, true);

			if (pageAndTokens.RedirectTo != null)
			{
				// Redirecting to the given url, as a 302:
				response.Headers["Location"] = pageAndTokens.RedirectTo;
				response.StatusCode = 302;
				return;
			}

			await Events.Page.BeforeNavigate.Dispatch(context, pageAndTokens.Page, path);

			if (updateContext)
			{
				// Update the token:
				context.SendToken(response);
			}

			var responseStream = response.Body;

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
				}
			}

			if (flatNodes == null)
			{
				flatNodes = await RenderPage(context, pageAndTokens, path);
			}

			var outputStream = compress ? new GZipStream(responseStream, CompressionMode.Compress) : responseStream;

			// Create a string writer for the stream:
			var writer = new StreamWriter(outputStream);

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
					await writer.WriteAsync(await subNode.OnGenerate(context));
					await writer.FlushAsync();
				}
			}

			await outputStream.FlushAsync();

			if (compress)
			{
				await outputStream.DisposeAsync();
			}
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
			
			List<DocumentNode> flatNodes = await RenderMobilePage(context, locale, mobileMeta);

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

	}

}
