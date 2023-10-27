using System.Collections.Generic;

namespace Api.Pages;


/// <summary>
/// status of the html cache.
/// </summary>
public class HtmlCacheStatus
{

	/// <summary>
	/// Locales in the cache currently. Null if cache is empty.
	/// </summary>
	public List<HtmlCachedLocaleStatus> Locales;

}

/// <summary>
/// Particular cached locale in the html cache.
/// </summary>
public class HtmlCachedLocaleStatus
{
	/// <summary>
	/// Id of the locale.
	/// </summary>
	public int LocaleId;

	/// <summary>
	/// The list of cached pages.
	/// </summary>
	public List<HtmlCachedPageStatus> CachedPages;
	
}

/// <summary>
/// Info about a particular cached page.
/// </summary>
public class HtmlCachedPageStatus
{
	
	/// <summary>
	/// The cached URL.
	/// </summary>
	public string Url;
	
	/// <summary>
	/// Html node count. Null if nodes not in cache.
	/// </summary>
	public int? NodeCount;

	/// <summary>
	/// Compressed size of anon state. Null if state is not anon cached.
	/// </summary>
	public int? AnonymousStateSize;

	/// <summary>
	/// Compressed data size. Null if not anon cached.
	/// </summary>
	public int? AnonymousDataSize;
	
}