using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
namespace Api.Pages;


/// <summary>
/// A virtual field value generator for a field called "primaryUrl".
/// You can include this field on any type and it will provide the URL of the 
/// page where the object is the primary content. See also: Primary Content on the wiki.
/// 
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class PrimaryUrlValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	private PageService _pageService;
	private UrlGenerationMeta _genMeta;
	private UrlGenerationCache _cache;

	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override async ValueTask GetValue(Context context, T forObject, Writer writer)
	{
		if (_pageService == null)
		{
			_pageService = Services.Get<PageService>();
		}

		if (_cache == null || _pageService.IsUrlCacheStale(_cache))
		{
			// Get the current URL generation cache:
			_cache = await _pageService.GetUrlGenerationCache();
			var lookup = _cache.GetLookup(UrlGenerationScope.UI);

			// Obtain URL generation metadata for the current type:
			lookup.TryGetValue(typeof(T), out _genMeta);
		}

		if (_genMeta == null)
		{
			writer.WriteASCII("null");
			return;
		}

		// Generate the URL:
		var pageUrl = _genMeta.Generate(forObject);

		// Write the URL string:
		writer.WriteEscaped(pageUrl);
	}

	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type GetOutputType()
	{
		return typeof(string);
	}

}