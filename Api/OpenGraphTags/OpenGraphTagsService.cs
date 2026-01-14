using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.CanvasRenderer;
using Api.Startup;
using HtmlAgilityPack;
using System;
using Api.SocketServerLibrary;

namespace Api.OpenGraphTags
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class OpenGraphTagsService : AutoService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public OpenGraphTagsService(FrontendCodeService feService)
        {
			var config = GetConfig<OpenGraphTagsServiceConfig>();

			Events.Page.OnWriteHeadEnd.AddEventListener(async (Context ctx, Writer writer, PageWithTokens pageWithTokens) =>
			{
				var baseUrl = feService.GetContentUrl(ctx.LocaleId);

				// We need to add a title, type, url and image tag required.	
				//Title
				var titleMeta = await pageWithTokens.GetMetaString(ctx, "title");
				var typeMeta = await pageWithTokens.GetMetaString(ctx, "type");
				var imageMeta = await pageWithTokens.GetMetaString(ctx, "image");
				var descriptionMeta = await pageWithTokens.GetMetaString(ctx, "description");
				var url = baseUrl + (pageWithTokens.PageTerminal == null ? "" : pageWithTokens.PageTerminal.FullRoute);

				// Opening og:title
				writer.WriteASCII("<meta property=\"og:title\" content=");

				if (titleMeta == null)
				{
					writer.WriteASCII("\"\"");
				}
				else
				{
					writer.WriteEscaped(titleMeta);
				}

				// Closing title and opening og:type
				writer.WriteASCII("/><meta property=\"og:type\" content=");

				if (!string.IsNullOrEmpty(typeMeta))
				{
					writer.WriteEscaped(typeMeta);
				}
				else
				{
					writer.WriteASCII("\"website\"");
				}

				// Closing type and opening og:url
				writer.WriteASCII("/><meta property=\"og:url\" content=");
				writer.WriteEscaped(url); // it's never null

				// Closing url and opening og:image
				writer.WriteASCII("/><meta property=\"og:image\" content=");

				string imageUrl;

				// We have three options - either the primary object, the page image ref, or the favicon.
				if (!string.IsNullOrEmpty(imageMeta))
                {
					// Let's handle our imageMeta which will be in the form of public:100.png
					// We need to split by the colon, then the period to append the size.
					var image = imageMeta.Substring(imageMeta.IndexOf(":") + 1);

					var name = image.Split(".")[0];

					// NB: second Split() removes excess attributes if found (other formats, dimension info, blurhash, etc)
					// (e.g. 2942-512.jpg|webp?w=5464&h=3640&b=LbD%2Ciu%25MR*n~t-WYf5s)
					var ext = "." + image.Split(".")[1].Split("|")[0];

					var contentDir = "";
					var imageSize = "";

					// Is it in the content dir?
					if(imageMeta.StartsWith("public"))
                    {
						contentDir = "/content/";
						imageSize = "-512";
						imageUrl = baseUrl + contentDir + name + imageSize + ext;
					}

					else if(imageMeta.StartsWith("private"))
                    {
						contentDir = "/content-private/";
						imageSize = "-512";
						imageUrl = baseUrl + contentDir + name + imageSize + ext;
					}
                    else
                    {
						imageUrl = baseUrl + image;
					}
                }
                else
                {
					imageUrl = baseUrl + "/favicon-32x32.png";
                }

				// It's never null
				writer.WriteEscaped(imageUrl);
				// Closing image
				writer.WriteASCII("/>");

				// We also have some optional ones that are good to have. 

				string ogDescription = "";

				if (!string.IsNullOrEmpty(descriptionMeta))
                {
					var _canvasRendererService = Services.Get<CanvasRendererService>();
					var descriptionHtmlString = _canvasRendererService.CanvasToComponentXml(descriptionMeta);
					descriptionHtmlString = descriptionHtmlString.Replace("</li><li>", "; </li><li>");

					var doc = new HtmlDocument();
					doc.LoadHtml(descriptionHtmlString);
					ogDescription = doc.DocumentNode.InnerText;
                }
                else
                {
					ogDescription = config.DefaultDescription;
				}

				// Optional description - only written if one is present
				if (!string.IsNullOrEmpty(ogDescription))
				{
					writer.WriteASCII("<meta property=\"og:description\" content=");
					writer.WriteEscaped(ogDescription.Length > 200 ?
						ogDescription.Substring(0, Math.Min(ogDescription.Length, 199)) + (char)0x2026 : ogDescription);
					writer.WriteASCII("/>");
				}

				// Site name
				var siteName = config.SiteName;
				if (!string.IsNullOrEmpty(siteName))
				{
					writer.WriteASCII("<meta property=\"og:site_name\" content=");
					writer.WriteEscaped(siteName);
					writer.WriteASCII("/>");
				}

				return writer;
			});
		}

	}
}