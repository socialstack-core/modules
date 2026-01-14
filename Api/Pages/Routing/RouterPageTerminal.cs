using Api.CanvasRenderer;
using Api.Contexts;
using Api.Database;
using Api.Pages;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Startup.Routing;


/// <summary>
/// A router node which represents a page.
/// </summary>
public class RouterPageTerminal : TerminalNode
{
	/// <summary>
	/// The page at this terminal.
	/// </summary>
	public readonly Page Page;

	/// <summary>
	/// True if this is an admin group page. This impacts the head as the page is rendered with e.g. the admin modules.
	/// </summary>
	public bool IsAdmin;

	/// <summary>
	/// Preformatted JSON array of the url token names. ["A", "B", ..]. will be the string "null" if it is null.
	/// </summary>
	public readonly string TokenNamesJson;

	/// <summary>
	/// Token names as a normal list.
	/// </summary>
	public List<string> TokenNames;

	/// <summary>
	/// The HtmlService instance.
	/// </summary>
	protected readonly HtmlService _htmlService;

	/// <summary>
	/// The page generator.
	/// </summary>
	public readonly CanvasGenerator Generator;

	/// <summary>
	/// Creates a new page terminal.
	/// </summary>
	/// <param name="children"></param>
	/// <param name="tokens"></param>
	/// <param name="page"></param>
	/// <param name="primaryType"></param>
	/// <param name="exactMatch"></param>
	/// <param name="fullRoute"></param>
	public RouterPageTerminal(IntermediateNode[] children, List<string> tokens, Type primaryType, Page page, string exactMatch, string fullRoute) 
		: base(children, exactMatch, null, null, fullRoute)
	{
		Page = page;
		_htmlService = Services.Get<HtmlService>();
		Generator = new CanvasGenerator(page.BodyJson.GetFallback().ValueOf(), primaryType);

		if (!string.IsNullOrEmpty(page.Key))
		{
			IsAdmin = page.Key.Contains("admin_") || page.Key == "admin";
		}

		if (tokens == null)
		{
			TokenNamesJson = "null";
		}
		else
		{
			TokenNamesJson = Newtonsoft.Json.JsonConvert.SerializeObject(tokens);
			TokenNames = tokens;
		}
	}

	/// <summary>
	/// Execute this page node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public override ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		// Map to the non web specific page renderer:
		var pageWithTokens = new PageWithTokens()
		{
			PageTerminal = this,
			Host = httpContext.Request.Host
		};

		if (tokenCount > 0)
		{
			var url = httpContext.Request.Path.Value;

			pageWithTokens.TokenValues = Router.ConvertTokens(tokenCount, url, ref tokens);
		}

		return _htmlService.RouteBasicContextRequest(httpContext, basicContext, pageWithTokens);
	}

	/// <summary>
	/// Gets the primary object from this terminal, if it has one.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="pageWithTokens"></param>
	/// <returns></returns>
	public virtual ValueTask<object> GetPrimaryObject(Context context, PageWithTokens pageWithTokens)
	{
		// Load the primary content
		return new ValueTask<object>((object)null);
	}

	/// <summary>
	/// Gets the primary service if there is one.
	/// </summary>
	/// <returns></returns>
	public virtual AutoService GetPrimaryService()
	{
		return null;
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		string name = "Untitled Page";

		if (Page != null)
		{
			var pageTitle = Page.Title.GetFallback();

			if (!string.IsNullOrEmpty(pageTitle))
			{
				name = pageTitle;
			}
		}

		return new RouterNodeMetadata()
		{
			Name = name,
			HasChildren = HasChildren(),
			ChildKey = ExactMatch,
			FullRoute = FullRoute,
			ContentId = (ulong)Page.Id,
			EditUrl = "/en-admin/page/" + Page.Id,
			Type = "Page",
		};
	}

}

/// <summary>
/// A page terminal for a page which has primary content of the specified type.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
public class RouterPageTerminal<T, ID> : RouterPageTerminal
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	private readonly AutoService<T, ID> PrimaryService;

	private readonly int IdTokenIndex;

	private readonly ID? SpecificContentId;

	/// <summary>
	/// Creates a new page terminal.
	/// </summary>
	/// <param name="children"></param>
	/// <param name="tokens"></param>
	/// <param name="page"></param>
	/// <param name="primaryService"></param>
	/// <param name="specificContentId"></param>
	/// <param name="idTokenIndex"></param>
	/// <param name="exactMatch"></param>
	/// <param name="fullRoute"></param>
	public RouterPageTerminal(IntermediateNode[] children, List<string> tokens, AutoService<T, ID> primaryService, string specificContentId, int idTokenIndex, Page page, string exactMatch, string fullRoute)
		: base(children, tokens, primaryService.ServicedType, page, exactMatch, fullRoute)
	{
		PrimaryService = primaryService;
		IdTokenIndex = idTokenIndex;

		if (specificContentId != null)
		{
			// This is the fallback primary content page otherwise.
			SpecificContentId = primaryService.ConvertId(ulong.Parse(specificContentId));
		}
	}

	/// <summary>
	/// Execute this page node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public override ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		// Map to the non web specific page renderer:
		var pageWithTokens = new PageWithTokens()
		{
			PageTerminal = this,
			PrimaryService = PrimaryService,
			Host = httpContext.Request.Host
		};

		if (tokenCount > 0)
		{
			var url = httpContext.Request.Path.Value;
			pageWithTokens.TokenValues = Router.ConvertTokens(tokenCount, url, ref tokens);
		}

		// This call internally sets PrimaryObject as well.
		return RunPrimary(httpContext, basicContext, pageWithTokens);
	}

	/// <summary>
	/// Gets the primary service if there is one.
	/// </summary>
	/// <returns></returns>
	public override AutoService GetPrimaryService()
	{
		return PrimaryService;
	}

	/// <summary>
	/// Gets the primary object from this terminal.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="pageWithTokens"></param>
	/// <returns></returns>
	public override async ValueTask<object> GetPrimaryObject(Context context, PageWithTokens pageWithTokens)
	{
		// The terminal either has a constant ID (for specific content permalinks)
		// or the ID is deriveable from the token values.
		if (SpecificContentId.HasValue)
		{
			return await PrimaryService.Get(context, SpecificContentId.Value);
		}

		// Id token is mandatory but just in case.
		if (IdTokenIndex == -1 || pageWithTokens.TokenValues == null)
		{
			return null;
		}

		var tokenValue = pageWithTokens.TokenValues[IdTokenIndex];

		if (!ulong.TryParse(tokenValue, out ulong uId))
		{
			// The URL contains a bad ID.
			return null;
		}

		return await PrimaryService.Get(context, PrimaryService.ConvertId(uId));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="pageWithTokens"></param>
	/// <returns></returns>
	private async ValueTask<bool> RunPrimary(HttpContext httpContext, Context basicContext, PageWithTokens pageWithTokens)
	{
		// Full context is required:
		var context = await httpContext.Request.GetContext(basicContext);

		// Get the PO:
		pageWithTokens.PrimaryObject = await GetPrimaryObject(context, pageWithTokens);

		// On these routes, the PO is mandatory.
		if (pageWithTokens.PrimaryObject == null)
		{
			// 404
			return false;
		}

		// Route the request, rendering the page itself
		return await _htmlService.RouteRequest(httpContext, context, pageWithTokens);
	}
}