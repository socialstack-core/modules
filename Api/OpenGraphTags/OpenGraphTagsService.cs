using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.CanvasRenderer;
using Api.Startup;
using HtmlAgilityPack;
using System;

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
		public OpenGraphTagsService()
        {
			var config = GetConfig<OpenGraphTagsServiceConfig>();

			Events.Page.Generated.AddEventListener(async (Context ctx, Document pageDocument) =>
			{
				var baseUrl = Services.Get<FrontendCodeService>().GetContentUrl(ctx.LocaleId);

				// We need to add a title, type, url and image tag required.	
				//Title
				DocumentNode titleNode = new DocumentNode("meta", true);
				titleNode.With("property", "og:title");
				titleNode.With("content", (string)await pageDocument.GetMeta(ctx, "title"));
				pageDocument.Head.AppendChild(titleNode);

				//Type
				DocumentNode typeNode = new DocumentNode("meta", true);
				typeNode.With("property", "og:type");

				var typeMeta = (string)await pageDocument.GetMeta(ctx, "type");

				if (!string.IsNullOrEmpty(typeMeta))
                {
					typeNode.With("content", typeMeta);
                }
				else
                {
					typeNode.With("content", "website");
				}
				pageDocument.Head.AppendChild(typeNode);

				//Url
				DocumentNode urlNode = new DocumentNode("meta", true);
				urlNode.With("property", "og:url");
				urlNode.With("content", baseUrl + pageDocument.Path);
				pageDocument.Head.AppendChild(urlNode);

				//Image
				DocumentNode imageNode = new DocumentNode("meta", true);
				imageNode.With("property", "og:image");

				var imageMeta = (string)await pageDocument.GetMeta(ctx, "image");

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
						imageNode.With("content", baseUrl + contentDir + name + imageSize + ext);
					}

					else if(imageMeta.StartsWith("private"))
                    {
						contentDir = "/content-private/";
						imageSize = "-512";
						imageNode.With("content", baseUrl + contentDir + name + imageSize + ext);
					}
                    else
                    {
						imageNode.With("content", baseUrl + image);
					}
                }
                else
                {
					imageNode.With("content", baseUrl + "/favicon-32x32.png");
                }
				pageDocument.Head.AppendChild(imageNode);

				// We also have some optional ones that are good to have. 
				//Descritpion 
				DocumentNode descriptionNode = new DocumentNode("meta", true);
				descriptionNode.With("property", "og:description");

				var descriptionMeta = (string)await pageDocument.GetMeta(ctx, "description");
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

				if (!string.IsNullOrEmpty(ogDescription))
				{
					descriptionNode.With("content", ogDescription.Length > 200 ?
						ogDescription.Substring(0, Math.Min(ogDescription.Length, 199)) + (char)0x2026 : ogDescription);

					pageDocument.Head.AppendChild(descriptionNode);
				}

				//Site name
				DocumentNode siteNameNode = new DocumentNode("meta", true);
				siteNameNode.With("property", "og:site_name");
				// Just grab it from the settings.
				siteNameNode.With("content", config.SiteName);
				pageDocument.Head.AppendChild(siteNameNode);

				return pageDocument;
			});
		}

	}
}