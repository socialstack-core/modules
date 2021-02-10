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

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public HtmlService(PageService pages, CanvasRendererService canvasRendererService)
		{
			_pages = pages;
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
				// Doesn't matter what the change was - we'll wipe the cache.
				_pageCache.Clear();

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterDelete.AddEventListener((Context context, Page page) =>
			{
				// Doesn't matter what the change was - we'll wipe the cache.
				_pageCache.Clear();

				return new ValueTask<Page>(page);
			});

			Events.Page.AfterCreate.AddEventListener((Context context, Page page) =>
			{
				// Doesn't matter what the change was - we'll wipe the cache.
				_pageCache.Clear();

				return new ValueTask<Page>(page);
			});

			Events.Page.Received.AddEventListener((Context context, Page page, int mode) => {

				// Doesn't matter what the change was - we'll wipe the cache.
				_pageCache.Clear();

				return new ValueTask<Page>(page);
			});
		}

		private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		private readonly Dictionary<int, byte[]> _pageCache = new Dictionary<int, byte[]>();

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
		/// PageRouter state data as a js string. This data is always the same until a page is added/ deleted/ a url is changed.
		/// </summary>
		/// <returns></returns>
		private async ValueTask<string> BuildPageStateJs(Context context, Page page)
		{
			var sb = new StringBuilder();
			sb.Append("window.pageRouterData=[");

			var urlList = await _pages.GetAllPageUrls(context);

			for (var i=0;i<urlList.Count; i++)
			{
				if (i != 0)
				{
					sb.Append(',');
				}

				var pageId = urlList[i].PageId;

				if (pageId == page.Id)
				{
					sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(page, jsonSettings));
				}
				else
				{
					// Compact version:
					sb.Append("{url:\"");
					sb.Append(HttpUtility.HtmlAttributeEncode(urlList[i].Url));
					sb.Append("\",id:");
					sb.Append(pageId);
					sb.Append('}');
				}
			}

			sb.Append("];");

			return sb.ToString();
		}

		/// <summary>
		/// Url -> nodes that are as pre-generated as possible.
		/// </summary>
		private readonly Dictionary<string, List<DocumentNode>> cache = new Dictionary<string, List<DocumentNode>>();

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

			if (cache.TryGetValue(path, out List<DocumentNode> flatNodes))
			{
				return flatNodes;
			}

			var page = pageAndTokens.Page;

			// Generate the document:
			var doc = new Document();
			doc.Path = path;
			doc.SourcePage = page;
			doc.Html.With("class", "ui web");

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

					if (primaryToken.IsId)
					{
						if (int.TryParse(pageAndTokens.TokenValues[countA - 1], out int primaryObjectId))
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

			// Handle all Start Head Tags in the config.
			HandleCustomHeadList(_config.StartHeadTags, head);

			head.AppendChild(new DocumentNode("link", true).With("rel", "apple-touch-icon").With("sizes", "180x180").With("href", "/apple-touch-icon.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "32x32").With("href", "/favicon-32x32.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "icon").With("type", "image/png").With("sizes", "16x16").With("href", "/favicon-16x16.png"))
				.AppendChild(new DocumentNode("link", true).With("rel", "manifest").With("href", "/site.webmanifest"))
				.AppendChild(new DocumentNode("link", true).With("rel", "mask-icon").With("href", "/safari-pinned-tab.svg").With("color", "#ffffff"))
				.AppendChild(new DocumentNode("link", true).With("rel", "stylesheet").With("href", packDir + "styles.css?v=1"))
				.AppendChild(new DocumentNode("meta", true).With("name", "msapplication-TileColor").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("name", "theme-color").With("content", "#ffffff"))
				.AppendChild(new DocumentNode("meta", true).With("charset", "utf-8"))
				.AppendChild(new DocumentNode("meta", true).With("name", "viewport").With("content", "width=device-width, initial-scale=1"))
				.AppendChild(new DocumentNode("title").AppendChild(new TextNode(page.Title)));

			// Handle all End Head tags in the config.
			HandleCustomHeadList(_config.EndHeadTags, head);
			
			var reactRoot = new DocumentNode("div").With("id", "react-root");

			if (_config.PreRender)
			{
				var set = new CanvasAndContextSet()
				{
					BodyJson = page.BodyJson
				};

				var ctx = new Dictionary<string, object>();
				set.Contexts.Add(ctx);

				try
				{
					var preRender = await _canvasRendererService.Render(set);
					var renderedBody = preRender[0].Body;
					reactRoot.AppendChild(new TextNode(renderedBody));
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
			
			var body = doc.Body;

			// Handle all start body JS scripts
			HandleCustomScriptList(_config.StartBodyJs, body);
			
			body.AppendChild(reactRoot)
				.AppendChild(new DocumentNode("script").AppendChild(new TextNode(await BuildPageStateJs(context, page))))
				.AppendChild(
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

			var mainJs = new DocumentNode("script").With("src", packDir + "main.generated.js?v=1");
			doc.MainJs = mainJs;
			body.AppendChild(mainJs);
			
			// Handle all After Main JS scripts
			HandleCustomScriptList(_config.AfterMainJs, body);
			
			// Handle all End Body JS scripts
			HandleCustomScriptList(_config.EndBodyJs, body);
			
			// Trigger an event for things to modify the html however they need:
			doc = await Events.Page.Generated.Dispatch(context, doc);

			// Build the flat HTML for the page:
			cache[path] = flatNodes = doc.Flatten();
			
			// Note: Although gzip does support multiple concatenated gzip blocks, browsers do not implement this part of the gzip spec correctly.
			// Unfortunately that means no part of the stream can be pre-compressed; must compress the whole thing and output that.

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
				
				if (bodyScript.Content != null)
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
