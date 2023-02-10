using Api.SearchCrawler;


namespace Api.Eventing
{

	/// <summary>
	/// Events are instanced automatically. 
	/// You can however specify a custom type or instance them yourself if you'd like to do so.
	/// </summary>
	public partial class Events
	{

		/// <summary>
		/// Event group for the search crawler.
		/// </summary>
		public static SearchCrawlEventGroup Crawler;
		
	}

}

namespace Api.SearchCrawler
{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class SearchCrawlEventGroup : Eventing.EventGroupCore<CrawledPageMeta, uint>
	{

		/// <summary>
		/// Called when the crawled has just rendered a page and is telling you about it.
		/// </summary>
		public Api.Eventing.EventHandler<CrawledPageMeta> PageCrawled;

	}

}
