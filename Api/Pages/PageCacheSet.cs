namespace Api.Pages;


/// <summary>
/// The set of caches in the page system.
/// </summary>
public struct PageCacheSet
{
	/// <summary>
	/// The generation cache.
	/// </summary>
	public UrlGenerationCache GenerationCache;
	
	/// <summary>
	/// The lookup caches.
	/// </summary>
	public UrlLookupCache[] LookupCache;
	
}