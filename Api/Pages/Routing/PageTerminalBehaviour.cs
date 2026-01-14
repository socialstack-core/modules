using Api.Pages;
using System;
using System.Collections.Generic;

namespace Api.Startup.Routing;


/// <summary>
/// A terminal behaviour which is used to route to a Page.
/// </summary>
public class PageTerminalBehaviour : TerminalBehaviour
{
	/// <summary>
	/// The page itself.
	/// </summary>
	public Page Page;

	/// <summary>
	/// The primary content service for this page.
	/// </summary>
	public AutoService PrimaryService;

	/// <summary>
	/// An ID for a specific piece of primary content. If null and PrimaryService is set, this page is 
	/// the fallback primary content page.
	/// </summary>
	public string SpecificContentId;

	/// <summary>
	/// typeof(RouterPageTerminal,) or custom variant thereof. Drive advanced custom behaviour at the terminal with this if required.
	/// Note that the ctor used must match the default one.
	/// </summary>
	public Type GenericPageTerminalType = typeof(RouterPageTerminal<,>);
	
	/// <summary>
	/// Creates a new terminal method.
	/// </summary>
	/// <param name="page"></param>
	/// <param name="primaryService"></param>
	/// <param name="specificContentId"></param>
	public PageTerminalBehaviour(Page page, AutoService primaryService, string specificContentId)
	{
		Page = page;
		PrimaryService = primaryService;
		SpecificContentId = specificContentId;
	}

	/// <summary>
	/// True if the behaviours are the same type and target.
	/// </summary>
	/// <returns></returns>
	public override bool Equals(TerminalBehaviour behaviour)
	{
		var node = behaviour as PageTerminalBehaviour;

		if (node == null)
		{
			return false;
		}

		var otherId = node.Page?.Id;
		var selfId = Page?.Id;

		if (!otherId.HasValue || !selfId.HasValue)
		{
			return false;
		}

		return otherId.Value == selfId.Value;
	}

	/// <summary>
	/// Clones this behaviour.
	/// </summary>
	/// <returns></returns>
	public override TerminalBehaviour Clone()
	{
		return new PageTerminalBehaviour(Page, PrimaryService, SpecificContentId);
	}

	/// <summary>
	/// Constructs this node in to a readonly tree node.
	/// </summary>
	/// <returns></returns>
	public override TerminalNode Build(BuilderNode node)
	{
		if (node == null)
		{
			// Typically a standalone status page such as 404.
			return new RouterPageTerminal(
				null,
				null,
				null,
				Page,
				null,
				""
			);
		}

		var tokens = node.GetAllTokens();

		if (PrimaryService == null)
		{
			// Normal page
			return new RouterPageTerminal(
				node.BuildChildren(),
				tokens,
				null,
				Page,
				node.IsToken ? null : node.Text,
				node.FullRoute
			);
		}

		int tokenIndex = -1;

		if (SpecificContentId == null)
		{
			// An .id token is mandatory (see also: Permalink.Target)
			var idTokenText = PrimaryService.ServicedType.Name.ToLower() + ".id";
			tokenIndex = GetTokenIndex(tokens, idTokenText);

			if (tokenIndex == -1)
			{
				Log.Warn("page", "Invalid page route for '" + node.FullRoute + "'. It targets a primary content page but does not have a ${" + idTokenText + "} token in the Url. If you are using other non-ID tokens, you need to instead generate permalinks. This is such that historical URLs are preserved if your fields change.");
				return null;
			}
		}

		// Page with primary content
		var rpt = GenericPageTerminalType.MakeGenericType(PrimaryService.ServicedType, PrimaryService.IdType);

		var result = Activator.CreateInstance(rpt, new object[] {
			node.BuildChildren(),
			tokens,
			PrimaryService,
			SpecificContentId,
			tokenIndex,
			Page,
			node.IsToken ? null : node.Text,
			node.FullRoute
		});

		return result as RouterPageTerminal;
	}

	/// <summary>
	/// Case sensitive search for a specific token index. -1 if not found.
	/// </summary>
	/// <param name="tokens"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	private int GetTokenIndex(List<string> tokens, string token)
	{
		if (tokens == null)
		{
			return -1;
		}

		var tokenLower = token.ToLower();

		for (var i = 0; i < tokens.Count; i++)
		{
			if (tokens[i].ToLower() == tokenLower)
			{
				return i;
			}
		}

		return -1;
	}

}
