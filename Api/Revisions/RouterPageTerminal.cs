using Api.Contexts;
using Api.Database;
using Api.Pages;
using Api.Revisions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Startup.Routing;

/// <summary>
/// A page terminal for a page which has primary content of the specified type. The ID given is for a revision.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="ID"></typeparam>
public class RouterRevisionPageTerminal<T, ID> : RouterPageTerminal
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{

	private readonly AutoService<T, ID> PrimaryService;
	private readonly RevisionService<T, ID> RevisionService;

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
	public RouterRevisionPageTerminal(IntermediateNode[] children, List<string> tokens, AutoService<T, ID> primaryService, string specificContentId, int idTokenIndex, Page page, string exactMatch, string fullRoute)
		: base(children, tokens, primaryService.ServicedType, page, exactMatch, fullRoute)
	{
		PrimaryService = primaryService;
		RevisionService = primaryService.Revisions;
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
		Revision<T, ID> rev;

		if (SpecificContentId.HasValue)
		{
			rev = await RevisionService.Get(context, SpecificContentId.Value);

			if (rev == null || string.IsNullOrEmpty(rev.ContentJson))
			{
				return null;
			}

			// Load the content from the revision:
			return PrimaryService.FromStoredJson(rev.ContentJson);
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

		rev = await RevisionService.Get(context, PrimaryService.ConvertId(uId));

		if (rev == null || string.IsNullOrEmpty(rev.ContentJson))
		{
			return null;
		}

		// Load the content from the revision:
		return PrimaryService.FromStoredJson(rev.ContentJson);
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