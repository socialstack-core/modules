using Api.Database;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Automations;
using System;
using Api.Pages;
using Api.Translate;
using Api.CanvasRenderer;
using System.Reflection;

namespace Api.SearchCrawler
{
    /// <summary>
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class SearchCrawlerService : AutoService
    {
        private PageService _pageService;
        private LocaleService _locales;
        private CanvasRendererService _canvasRenderer;
        private HtmlService _htmlService;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public SearchCrawlerService(PageService pageService, LocaleService locales, CanvasRendererService canvasRenderer, HtmlService htmlService)
        {
            _pageService = pageService;
            _locales = locales;
            _canvasRenderer = canvasRenderer;
            _htmlService = htmlService;

            // The cron expression runs it every hour. It does nothing if nothing is using the crawlers output.
            Events.Automation("crawler", "0 0 * ? * * *").AddEventListener(async (Context context, AutomationRunInfo runInfo) =>
            {

                await CrawlEverything();

                return runInfo;
            });

        }

        /// <summary>
        /// Tells the crawler to crawl every page in the sitemap now. Does nothing if there is nothing to receive the output of the crawler.
        /// This means you turn the crawler on by simply adding an event handler to it.
        /// As it runs, it invokes both the Events.Crawler.PageCrawled event and also the given handler.
        /// </summary>
        /// <returns></returns>
        public async ValueTask CrawlEverything()
        {
            if (!Events.Crawler.PageCrawledNoPrimaryContent.HasListeners())
            {
                // Nothing wants the crawler output so we don't need to run at all.
                return;
            }

            // Anonymous context - Let's not accidentally leak things through the search index.
            var context = new Context();

            // Get all the current locales:
            var locales = await _locales.Where("").ListAll(context);

            await Events.Crawler.CrawlerStatus.Dispatch(context, SearchCrawlerStatus.Started);

            // For each locale..
            foreach (var locale in locales)
            {
                context.LocaleId = locale.Id;

                // Get the raw page tree for the locale (does not iterate every token page internally):
                var sitemap = await _pageService.GetPageTree(context);

                // Iterate through it, expanding any dynamic token-like pages as we go. Non-expandable pages are ignored however.
                await CrawlTreeNode(context, sitemap.Root);
            }

            await Events.Crawler.CrawlerStatus.Dispatch(context, SearchCrawlerStatus.Completed);

        }

        /// <summary>
        /// Crawls a single page. This expects its URL to be static with no tokens in it.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="page"></param>
        /// <param name="onCrawledPage"></param>
        /// <returns></returns>
        public async ValueTask Crawl(Context context, Page page, Func<Context, CrawledPageMeta, ValueTask> onCrawledPage = null)
        {
            // State is always empty here.
            var state = new PageState();

            try
            {
                var renderedCanvas = await _canvasRenderer.Render(context, page.BodyJson, state, page.Url);

                var cpm = new CrawledPageMeta()
                {
                    PageState = state,
                    Url = page.Url,
                    Page = page,
                    BodyHtml = renderedCanvas.Body
                };

                if (onCrawledPage != null)
                {
                    await onCrawledPage(context, cpm);
                }

                await Events.Crawler.PageCrawledNoPrimaryContent.Dispatch(context, cpm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Crawling '" + page.Url + " Failed " + e.Message + e.StackTrace);
            }

        }

        /// <summary>
        /// Crawls a specific page. The optional lookupNode provides metadata about the parsed URL.
        /// If tokenData is provided and the parsed URL contains 1 type token then crawling the 
        /// page will result in iterating through all permutations of the token.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="page"></param>
        /// <param name="tokenData"></param>
        /// <returns></returns>
        public async ValueTask CrawlPermutations(Context context, Page page, UrlLookupNode tokenData = null)
        {
            // Skip admin pages and any flagged as excluded
            if (page == null || page.ExcludeFromSearch || page.Url.StartsWith("/en-admin") || page.Url.StartsWith("en-admin"))
            {
                return;
            }

            if (tokenData != null && tokenData.UrlTokens != null && tokenData.UrlTokens.Count != 0)
            {
                // Only exactly 1 PO token is supported by this crawler.
                if (tokenData.UrlTokens.Count != 1)
                {
                    return;
                }

                var urlToken = tokenData.UrlTokens[0];
                var service = urlToken.Service;

                if (service == null || service.ServicedType == null)
                {
                    // Not a PO token. We have no information for how to crawl this
                    // page as the token(s) in the URL are too generic.
                    return;
                }

                if (_crawlAService == null)
                {
                    _crawlAService = GetType().GetMethod(nameof(CrawlService));
                }

                var crawlAServiceGenericMethod = _crawlAService.MakeGenericMethod(new Type[] {
                    service.ServicedType,
                    service.IdType
                });

                await (ValueTask)(crawlAServiceGenericMethod.Invoke(this, new object[] {
                    context,
                    service,
                    tokenData,
                    page
                }));
            }
            else
            {
                // Singular static page:
                await Crawl(context, page);
            }
        }

        private MethodInfo _crawlAService;

        public async ValueTask CrawlService<T, ID>(Context context, AutoService<T, ID> service, UrlLookupNode tokenData, Page page)
                    where T : Content<ID>, new()
                    where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
        {
            // As with the generic page crawl, do nothing if the service PageCrawl event is not subscribed to.
            if (!service.EventGroup.PageCrawled.HasListeners())
            {
                return;
            }

            // Create a URL generator for this page:
            var urlGenerator = new UrlGenerator(page.Url);

            // For each object in the service, establish its URL and crawl.
            await service.Where(DataOptions.IgnorePermissions).ListAll(context, async (Context c, T po, int index, object a, object b) =>
            {

                if (po == null)
                {
                    return;
                }

                var svc = (AutoService<T, ID>)a;

                // resolve the apparent URL:
                var resolvedUrl = urlGenerator.Generate(po);

                // resolve any meta data in the page title
                page.Title = _htmlService.ReplaceTokens(page.Title, po);

                var state = new PageState()
                {
                    PrimaryObject = po
                };

                try
                {
                    var renderedCanvas = await _canvasRenderer.Render(context, page.BodyJson, state, resolvedUrl);

                    var cpm = new CrawledPageMeta()
                    {
                        PageState = state,
                        Url = resolvedUrl,
                        Page = page,
                        BodyHtml = renderedCanvas.Body
                    };

                    // Tell other things about this page being crawled:
                    await svc.EventGroup.PageCrawled.Dispatch(c, cpm, po);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Crawling '" + page.Url + " Failed " + e.Message + e.StackTrace);
                }

            }, service, null);

        }

        private async ValueTask CrawlTreeNode(Context context, UrlLookupNode urlTreeNode)
        {
            if (urlTreeNode == null)
            {
                return;
            }

            // Crawl pages at this node (if there are any - pass through and redirect nodes exist too).
            var pages = urlTreeNode.Pages;

            if (pages != null)
            {
                foreach (var page in pages)
                {
                    await CrawlPermutations(context, page, urlTreeNode);
                }
            }

            // Crawl the child nodes too:
            var childNodes = urlTreeNode.Children;

            if (childNodes != null)
            {
                foreach (var child in childNodes)
                {
                    await CrawlTreeNode(context, child.Value);
                }
            }
        }
    }

}
