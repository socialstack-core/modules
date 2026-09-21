using Api.Automations;
using Api.CanvasRenderer;
using Api.Eventing;
using Api.Pages;
using Api.Startup;
using Api.Startup.Routing;
using Api.Uploader;
using Api.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Context = Api.Contexts.Context;

namespace Api.DynamicSiteMap
{
	/// <summary>
	/// Service to generate sitemaps from permalink data.
	/// </summary>
	[LoadPriority(101)]
	public partial class DynamicSiteMapService : AutoService
	{
		private DynamicSiteMapConfig _cfg;
		private readonly PermalinkService _permalinkService;
		private readonly FrontendCodeService _frontend;

		private const string _sitemapXmlHeader =
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
			+ "<urlset xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd\" xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n";

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DynamicSiteMapService(PermalinkService permalinkService, FrontendCodeService frontend)
		{
			_permalinkService = permalinkService;
			_frontend = frontend;

			Events.Page.OnGenerateRobotsTxt.AddEventListener((Context context, StringBuilder sb) => {

				if (sb == null)
				{
					return new ValueTask<StringBuilder>(sb);
				}

				sb.Append("Sitemap: ");
				sb.Append(_frontend.GetPublicUrl(1));
				sb.Append("/sitemap.xml\r\n");

				return new ValueTask<StringBuilder>(sb);
			});

			Events.Automation("Dynamic SiteMap", "0 0 * ? * * *", false, "Rebuild the dynamic sitemap.xml files").AddEventListener(async (Context ctx, AutomationRunInfo info) =>
			{
				await Rebuild(ctx);
				return info;
			});
		}

		/// <summary>
		/// Rebuilds all sitemap files from the permalink data.
		/// </summary>
		public async ValueTask Rebuild(Context context)
		{
			if (!IsConfigured())
			{
				return;
			}

			try
			{
				Log.Info(LogTag, "Dynamic Site Map rebuild starting.");

				var router = Router.CurrentRouter;
				var permalinkSet = await _permalinkService.GetSourcesByTarget(context);

				int pageCount = 0;
				int siteMapCount = 0;
				var currentContent = new StringBuilder();
				var masterContent = new StringBuilder();

				// Collect all canonical URLs
				var sitemapEntries = new List<SiteMapEntry>();

				foreach (var kvp in permalinkSet)
				{
					var target = kvp.Key;
					var sources = kvp.Value;

					if (target == null || sources == null || sources.Count == 0)
					{
						continue;
					}

					// Skip admin targets
					if (target.StartsWith("admin_primary:"))
					{
						continue;
					}

					var canonical = sources[0];

					// Skip entries with tokens or admin 
					if (string.IsNullOrWhiteSpace(canonical.Url) || canonical.Url.Contains('$') || canonical.Url.StartsWith("/en-admin/"))
					{
						continue;
					}

					// Skip URLs NOT matching configured inclusions
					if (_cfg.UrlInclusions != null && _cfg.UrlInclusions.Count > 0)
					{
						var included = false;
						foreach (var inclusion in _cfg.UrlInclusions)
						{
							if (canonical.Url.StartsWith(inclusion, StringComparison.OrdinalIgnoreCase))
							{
								included = true;
								break;
							}
						}
						if (!included)
						{
							continue;
						}
					}

					// Skip URLs matching configured exclusions
					if (_cfg.UrlExclusions != null && _cfg.UrlExclusions.Count > 0)
					{
						var excluded = false;
						foreach (var exclusion in _cfg.UrlExclusions)
						{
							if (canonical.Url.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase))
							{
								excluded = true;
								break;
							}
						}
						if (excluded)
						{
							continue;
						}
					}

					object po = null;
					DateTime? lastUpdated = null;

					if (router != null)
					{
						var terminalWithTokens = router.ResolveWithTokens(context, canonical.Url);
						if (terminalWithTokens.HasValue)
						{
							var pageTerminal = terminalWithTokens.Value.TerminalNode as RouterPageTerminal;

							if (pageTerminal != null)
							{
								if (pageTerminal.Page != null)
								{
									if (!pageTerminal.Page.CanIndex)
									{
										continue;
									}

									lastUpdated = pageTerminal.Page.EditedUtc;
								}

								po = await pageTerminal.GetPrimaryObject(context, new PageWithTokens()
								{
									TokenValues = terminalWithTokens.Value.Tokens,
									PageTerminal = pageTerminal,
									// (host, primary service etc not needed)
								});

								if (po != null)
								{
									var timestamps = po as IHaveTimestamps;
									if (timestamps != null)
									{
										if (lastUpdated == null || (timestamps.GetEditedUtc() > lastUpdated.Value))
										{
											lastUpdated = timestamps.GetEditedUtc();
										}
									}
								}
							}
						}
					}

					var siteMapEntry = new SiteMapEntry()
					{
						Link = canonical,
						PrimaryObject = po,
						LastUpdated = lastUpdated
					};

					// Perform any data related validation such as expired/hidden entities 
					siteMapEntry = await Events.SiteMap.ValidateLink.Dispatch(context, siteMapEntry);
					if (siteMapEntry == null)
					{
						continue;
					}

					sitemapEntries.Add(siteMapEntry);
				}

				// Build sitemap entries
				foreach (var entry in sitemapEntries.OrderBy(e => e.Link.Url, StringComparer.OrdinalIgnoreCase))
				{
					var sb = new StringBuilder();
					sb.AppendLine("<url>");
					sb.AppendLine($"<loc>{GetUrl(entry.Link.Url, context.LocaleId)}</loc>");

					if (entry.LastUpdated.HasValue)
					{
						sb.AppendLine($"<lastmod>{entry.LastUpdated.Value.ToString("yyyy-MM-dd")}</lastmod>");
					}

					sb.AppendLine("</url>");

					currentContent.Append(sb.ToString());
					pageCount++;

					// Once we exceed MaxSiteMapPages, flush to a numbered sub-file
					if (_cfg.MaxSiteMapPages > 0 && pageCount >= _cfg.MaxSiteMapPages)
					{
						siteMapCount++;
						await WriteSiteMapFile(context, siteMapCount, currentContent);
						currentContent = new StringBuilder();

						masterContent.AppendLine("<sitemap>");
						masterContent.AppendLine($"<loc>{GetUrl($"sitemap/{siteMapCount}", context.LocaleId)}</loc>");
						masterContent.AppendLine("</sitemap>");

						pageCount = 0;
					}
				}

				// Write the master sitemap.xml
				if (siteMapCount == 0)
				{
					// Everything fits in one file - write sitemap.xml directly as a urlset
					var tempSiteMapFileName = System.IO.Path.GetTempFileName();
					using (var file = new StreamWriter(tempSiteMapFileName, false))
					{
						WrapUrlSet(currentContent);
						file.WriteLine(currentContent.ToString());
					}

					var uploadLocator = new Upload()
					{
						Id = 1,
						IsPrivate = false,
						Subdirectory = "sitemap"
					};

					var upload = await Events.Upload.StoreFile.Dispatch(context, uploadLocator, tempSiteMapFileName, "sitemap.xml");
				}
				else
				{
					// Multiple sub-files exist - write remaining as a final sub-file
					if (currentContent.Length > 0)
					{
						siteMapCount++;
						await WriteSiteMapFile(context, siteMapCount, currentContent);

						masterContent.AppendLine("<sitemap>");
						masterContent.AppendLine($"<loc>{GetUrl($"sitemap/{siteMapCount}", context.LocaleId)}</loc>");
						masterContent.AppendLine("</sitemap>");
					}

					// Write sitemap.xml as a sitemap index
					var tempSiteMapFileName = System.IO.Path.GetTempFileName();
					using (var file = new StreamWriter(tempSiteMapFileName, false))
					{
						file.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
						file.WriteLine("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
						file.Write(masterContent.ToString());
						file.WriteLine("</sitemapindex>");
					}

					var uploadLocator = new Upload()
					{
						Id = 1,
						IsPrivate = false,
						Subdirectory = "sitemap"
					};

					var upload = await Events.Upload.StoreFile.Dispatch(context, uploadLocator, tempSiteMapFileName, "sitemap.xml");
				}

				Log.Ok(LogTag, $"Dynamic Site Map rebuild completed. {sitemapEntries.Count} entries across {Math.Max(siteMapCount, 1)} file(s).");
			}
			catch (Exception ex)
			{
				Log.Error(LogTag, ex, "Dynamic Site Map rebuild failed.");
			}
		}

		private async ValueTask WriteSiteMapFile(Context context, int siteMapCount, StringBuilder content)
		{
			if (content == null || content.Length == 0)
			{
				return;
			}

			var tempSiteMapFileName = System.IO.Path.GetTempFileName();
			using (var file = new StreamWriter(tempSiteMapFileName, false))
			{
				WrapUrlSet(content);
				file.WriteLine(content.ToString());
			}

			var uploadLocator = new Upload()
			{
				Id = 1,
				IsPrivate = false,
				Subdirectory = "sitemap"
			};

			var upload = await Events.Upload.StoreFile.Dispatch(context, uploadLocator, tempSiteMapFileName, $"sitemap-{siteMapCount}.xml");
		}

		private void WrapUrlSet(StringBuilder sb)
		{
			sb.Insert(0, _sitemapXmlHeader);
			sb.AppendLine("</urlset>");
		}

		private string GetUrl(string url, uint localeId)
		{
			url = url.ToLower().Trim();
			if (url.Length > 1 && url.EndsWith("/"))
			{
				url = url.TrimEnd('/');
			}

			if (url.StartsWith("https://") || url.StartsWith("http://"))
			{
				return url;
			}

			return UrlCombine(_frontend.GetPublicUrl(localeId), url);
		}

		private bool IsConfigured()
		{
			_cfg = GetConfig<DynamicSiteMapConfig>();
			return _cfg.MaxSiteMapPages > 0 && !_cfg.Disabled;
		}
		private static string UrlCombine(params string[] items)
		{
			return string.Join("/", items.Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim('/', '\\')));
		}
	}

	/// <summary>
	/// The details needed for a sitemap entry 
	/// </summary>
	public class SiteMapEntry()
	{
		/// <summary>
		/// The canonical entry for the url
		/// </summary>
		public Permalink Link;

		/// <summary>
		///  The primary object of the page, and if not then the page itself
		/// </summary>
		public object PrimaryObject;

		/// <summary>
		/// When was this last updated 
		/// </summary>
		public DateTime? LastUpdated;
	}

}
