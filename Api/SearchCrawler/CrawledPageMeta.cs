using Api.CanvasRenderer;
using Api.Contexts;
using Api.Pages;

namespace Api.SearchCrawler;

/// <summary>
/// Metadata for a page that just got crawled (and rendered in to HTML).
/// </summary>
public class CrawledPageMeta
{
	/// <summary>
	/// The page that got crawled. This gives you the BodyJson amongst other things.
	/// </summary>
	public Page Page;

	/// <summary>
	/// The rendered pages body html.
	/// </summary>
	public string BodyHtml;

	/// <summary>
	/// The URL that was used when rendering the page. 
	/// Will always be a resolved URL (Url does not necessarily equal Page.Url) meaning any tokens have been given values here.
	/// Use Page.Url if you want the original with ${tokens} still in it.
	/// </summary>
	public string Url;

	/// <summary>
	/// The page state which provides the URL token values (if there are any) and the information about the primary object.
	/// </summary>
	public PageState PageState;
}
