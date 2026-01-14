using Api.CanvasRenderer;
using Api.Contexts;
using Api.Startup.Routing;
using Api.Translate;
using Microsoft.AspNetCore.Http;
using Stripe;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Pages;


/// <summary>
/// A page and token values from the URL.
/// </summary>
public struct PageWithTokens
{
	/// <summary>
	/// The primary object for this page.
	/// </summary>
	public object PrimaryObject;
	/// <summary>
	/// The service for the primary object for this page.
	/// </summary>
	public AutoService PrimaryService;
	/// <summary>
	/// The host.
	/// </summary>
	public HostString Host;
	/// <summary>
	/// Any token values in the URL.
	/// </summary>
	public List<string> TokenValues;
	/// <summary>
	/// The page terminal in the router. Can be null.
	/// </summary>
	public RouterPageTerminal PageTerminal;

	private static PageService _pageService;

	/// <summary>
	/// See GetMeta for more information. This coerces the result to a string, 
	/// handling localisation as well using the provided context when necessary.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fieldName"></param>
	/// <returns></returns>
	public async ValueTask<string> GetMetaString(Context context, string fieldName)
	{
		var baseValue = await GetMeta(context, fieldName);

		if (baseValue == null)
		{
			return null;
		}

		if (baseValue is string)
		{
			return (string)baseValue;
		}

		if (baseValue is Localized<string>)
		{
			return ((Localized<string>)baseValue).Get(context);
		}

		// Ignores other localized types at the mo.
		return baseValue.ToString();
	}

	/// <summary>
	/// Gets a named meta field from the primary object. You can specify a meta field with [meta("fieldName")] in your entity.
	/// Note that [meta("title")] and [meta("description")] are 'guessed' automatically if you haven't explicitly declared them in your entity.
	/// If the meta field is not set on the primary object, this function will then attempt to read the meta field from the Page object instead.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="fieldName"></param>
	/// <returns></returns>
	public async ValueTask<object> GetMeta(Context context, string fieldName)
	{
		if (PrimaryService != null && PrimaryObject != null)
		{
			var meta = await PrimaryService.GetMetaFieldValue(context, fieldName, PrimaryObject);

			if (meta != null)
			{
				return meta;
			}
		}

		// Otherwise, try the page instead:
		if (PageTerminal == null || PageTerminal.Page == null)
		{
			return null;
		}

		if (_pageService == null)
		{
			_pageService = Api.Startup.Services.Get<PageService>();
		}

		return await _pageService.GetMetaFieldValue(context, fieldName, PageTerminal.Page);
	}

}
