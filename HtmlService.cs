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

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public HtmlService(PageService pages, CanvasRendererService canvasRendererService, FrontendCodeService frontend)
		{
			_pages = pages;
			_frontend = frontend;
			_canvasRendererService = canvasRendererService;

			_config = GetConfig<HtmlServiceConfig>();

			var pathToUIDir = AppSettings.Configuration["UI"];

			if (string.IsNullOrEmpty(pathToUIDir))
			{
				pathToUIDir = "UI/public";
			}
			
			var pathToAdminDir = AppSettings.Configuration["Admin"];

			if (string.IsNullOrEmpty(pathToAdminDir))
			{
				// The en-admin subdir is to make configuring NGINX easy:
				pathToAdminDir = "Admin/public/en-admin";
			}
			
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
		/// PageRouter state data as a js string. This data is always the same until a page is added/ deleted/ a url is changed.
		/// </summary>
		/// <returns></returns>
		private async ValueTask<string> BuildUserGlobalStateJs(Context context)
		{
			var sb = new StringBuilder();
			var publicContext = await context.GetPublicContext();
			sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(publicContext, jsonSettings));
			return sb.ToString();
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
		/// <returns></returns>
		public async ValueTask<string> RenderState(Context context, PageWithTokens pageAndTokens, string path)
		{
			object primaryObject = null;

			if (pageAndTokens.Tokens != null && pageAndTokens.TokenValues != null)
			{
				var countA = pageAndTokens.TokenValues.Count;

				if (countA > 0 && countA == pageAndTokens.Tokens.Count)
				{
					var primaryToken = pageAndTokens.Tokens[countA - 1];

					if (primaryToken.ContentType != null)
					{
						if (primaryToken.IsId)
						{
							if (uint.TryParse(pageAndTokens.TokenValues[countA - 1], out uint primaryObjectId))
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

			var poJson = (primaryObject != null ? Newtonsoft.Json.JsonConvert.SerializeObject(primaryObject, jsonSettings) : "null");
			string state;

			if (_config.PreRender)
			{
				var preRender = await _canvasRendererService.Render(context, pageAndTokens.Page.BodyJson, new PageState() {
					Tokens = pageAndTokens.TokenValues,
					TokenNames = pageAndTokens.TokenNames,
					PoJson = poJson
				}, path, true, RenderMode.None);

				state = preRender.Data;
			}
			else
			{
				state = "null";
			}

			return "{\"page\":" + Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.Page, jsonSettings) +
				", \"tokens\":" + (pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null") +
				", \"tokenNames\":" + (pageAndTokens.TokenNames != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenNames, jsonSettings) : "null") +
				", \"po\":" + poJson +
				",\"data\":" + state + "}";
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
				"I burnt the pastries",
				"I burnt the pizzas",
				"I burnt the cake again :(",
				"I burnt the chips",
				"I burnt the microwaveable dinner somehow",
				"I burnt the carpet",
				"Instructions unclear, fork wedged in ceiling",
				"Your pet ate all the food whilst you were away, but it wasn't my fault I swear",
				"Have you tried turning it off, then off, then back on and off again?",
				"Maybe the internet got deleted?",
				"Blame Mike",
				"Contact your system admin. If you are the system admin, I'm so sorry.",
				"You shall not pass!",
				"Ruh-roh Rorge!"
			};

			var rng = new Random();
			var doc = new Document();
			doc.Title = "Oops! Something has gone very wrong. " + messages[rng.Next(0, messages.Length)] + ".";

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

				var descript = new DocumentNode("pre").AppendChild(new TextNode(error.Description));
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

#if !DEBUG
			if (cache != null && context.LocaleId <= cache.Length)
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
			doc.Html.With("class", isAdmin ? "admin web" : "ui web").With("lang", locale.Code);
			
			var packDir = isAdmin ? "/en-admin/pack/" : "/pack/";

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
							if (uint.TryParse(pageAndTokens.TokenValues[countA - 1], out uint primaryObjectId))
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
			head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", mainCssFile.PublicUrl));

			if (isAdmin)
			{
				var mainAdminCssFile = await _frontend.GetAdminMainCss(context == null ? 1 : context.LocaleId);
				head.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", mainAdminCssFile.PublicUrl));
			}

			head.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"))
				.AppendChild(new DocumentNode("meta", true).With("name", "description").With("content", doc.ReplaceTokens(page.Description)))
				.AppendChild(new DocumentNode("title").AppendChild(new TextNode(doc.ReplaceTokens(page.Title))));

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
					var poJson = (doc.PrimaryObject != null ? Newtonsoft.Json.JsonConvert.SerializeObject(doc.PrimaryObject, jsonSettings) : "null");

					var preRender = await _canvasRendererService.Render(context, page.BodyJson, new PageState() {
						Tokens = pageAndTokens.TokenValues,
						TokenNames = pageAndTokens.TokenNames,
						PoJson = poJson
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
						body.AppendChild(
							new DocumentNode("script")
							.AppendChild(
								new TextNode(
									"window.pgState={\"page\":" + Newtonsoft.Json.JsonConvert.SerializeObject(page, jsonSettings) +
										", \"tokens\":" + (pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null") +
										", \"tokenNames\":" + (pageAndTokens.TokenNames != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenNames, jsonSettings) : "null") +
										", \"po\":" + poJson +
										",\"data\":" + preRender.Data + "};"
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
				body.AppendChild(
					new DocumentNode("script")
					.AppendChild(
						new TextNode(
							"window.pgState={\"page\":" + Newtonsoft.Json.JsonConvert.SerializeObject(page, jsonSettings) +
							", \"tokens\":" + (pageAndTokens.TokenValues != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenValues, jsonSettings) : "null") +
							", \"tokenNames\":" + (pageAndTokens.TokenNames != null ? Newtonsoft.Json.JsonConvert.SerializeObject(pageAndTokens.TokenNames, jsonSettings) : "null") +
							", \"po\":" + (doc.PrimaryObject != null ? Newtonsoft.Json.JsonConvert.SerializeObject(doc.PrimaryObject, jsonSettings) : "null") + 
							"};"
						)
					)
				);
			}
			
			// Handle all start body JS scripts
			HandleCustomScriptList(_config.StartBodyJs, body);
			
			// Still add the global state init substitution node:
			body.AppendChild(
				new DocumentNode("script")
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
				var mainAdminJs = new DocumentNode("script").With("src", mainAdminJsFile.PublicUrl);
				body.AppendChild(mainAdminJs);
				
				// Same also for the email modules:
				var mainEmailJsFile = await _frontend.GetEmailMainJs(context == null ? 1 : context.LocaleId);
				var mainEmailJs = new DocumentNode("script").With("src", mainEmailJsFile.PublicUrl);
				body.AppendChild(mainEmailJs);
			}

			// Get the main JS file. Note that this will (intentionally) delay on dev instances if the first compile hasn't happened yet.
			// That's primarily because we need the hash of the contents in the URL. Note that it has an internal cache which is almost always hit.
			var mainJsFile = await _frontend.GetMainJs(context == null ? 1 : context.LocaleId);
			
			var mainJs = new DocumentNode("script").With("src", mainJsFile.PublicUrl);
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
		/// Adds the primary object event handlers to the given event group.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="evtGroup"></param>
		public void AttachPrimaryObjectEventHandler<T, ID>(EventGroup<T, ID> evtGroup)
			 where T : new()
			 where ID : struct
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
		private void HandleCustomHeadList(List<HeadTag> list, DocumentNode head)
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
						node.With("content", headTag.Href);
					}
				}
				
				head.AppendChild(node);
			}

		}
		
		/// <summary>
		/// Handles adding a custom script list (if there even is one set) into the given node. They'll be appended.
		/// </summary>
		private void HandleCustomScriptList(List<BodyScript> list, DocumentNode body)
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

				body.AppendChild(node);
			}
			
		}
		
		/// <summary>
		/// Generates the base HTML for the given site relative url.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="path"></param>
		/// <param name="responseStream"></param>
		/// <param name="compress"></param>
		/// <returns></returns>
		public async Task BuildPage(Context context, string path, Stream responseStream, bool compress = true)
		{
			var pageAndTokens = await _pages.GetPage(context, path);

			List<DocumentNode> flatNodes = await RenderPage(context, pageAndTokens, path);

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
			// Todo: Preload templates used by the page as well
		}

	}

}
