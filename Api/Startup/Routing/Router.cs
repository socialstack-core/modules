using Api.CanvasRenderer;
using Api.Contexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Startup.Routing;


/// <summary>
/// The root of the URL router. It has multiple trees - one per http verb.
/// Routers are static structures. To change routes, you need to create a new router.
/// </summary>
public class Router
{
	/// <summary>
	/// The current router.
	/// </summary>
	public static Router CurrentRouter;

	/// <summary>
	/// True if the given router is not the current one.
	/// </summary>
	/// <param name="router"></param>
	/// <returns></returns>
	public static bool IsStale(Router router)
	{
		return router != CurrentRouter;
	}

	/// <summary>
	/// Ask the router to rebuild. Often requested repeatedly in quick succession, 
	/// so this internally buffers and will at most be a 100ms delay.
	/// </summary>
	public static void RequestRebuild()
	{
		RouterBuilder.RequestRebuild();
	}

	/// <summary>
	/// Max number of tokens allowed in a URL.
	/// </summary>
	public static readonly int MaxTokenCount = 4;

	/// <summary>
	/// The terminal to use when a 404 occurs.
	/// </summary>
	public TerminalNode Status_404;
	
	/// <summary>
	/// The set of router trees by HTTP verb.
	/// </summary>
	public readonly RouteNode[] TreesByVerb;

	private readonly PathString Slash = new PathString("/");

	/// <summary>
	/// Create a new router. Usually done via a RouterBuilder.
	/// </summary>
	/// <param name="treesByVerb"></param>
	public Router(RouteNode[] treesByVerb)
	{
		TreesByVerb = treesByVerb;
	}

	/// <summary>
	/// For each endpoint in the router, the given callback is executed.
	/// </summary>
	/// <param name="callback"></param>
	public void ForEachEndpoint(Action<TerminalNode> callback)
	{
		for (var i = 0; i < TreesByVerb.Length; i++)
		{
			TreesByVerb[i].ForEachEndpoint(callback);
		}
	}

	/// <summary>
	/// Resolves the URL to the routing node it directly maps to.
	/// This method is only for exploration of the routing tree, using the route as the key.
	/// This is because it does not handle redirection or rewrites.
	/// Tokens are matched but you won't be given their value.
	/// </summary>
	/// <param name="method"></param>
	/// <param name="url"></param>
	/// <returns></returns>
	public RouteNode AbsoluteResolve(string method, string url)
	{
		var current = GetByVerb(method);

		if (string.IsNullOrEmpty(url))
		{
			return current;
		}

		var path = url.AsSpan();

		// The requested path is..
		int pathStartIndex;
		int pathMax = path.Length;

		if (pathMax > 0)
		{
			// If it starts with a /, ignore it.
			var startsWithSlash = (path[0] == '/');
			var endsWithSlash = (pathMax > 1 && path[pathMax - 1] == '/');

			if (startsWithSlash)
			{
				pathStartIndex = 1;

				if (endsWithSlash)
				{
					pathMax--; // Yes this is correct! The max only moves down 1 as the paths actual true end is only moving 1 place.
					path = path.Slice(1, pathMax - 1); // The subpath however gets 2 chars shorter.
				}
				else
				{
					path = path.Slice(1);
				}
			}
			else if (endsWithSlash)
			{
				pathMax--;
				path = path.Slice(0, pathMax);
				pathStartIndex = 0;
			}
			else
			{
				pathStartIndex = 0;
			}
		}
		else
		{
			pathStartIndex = 0;
		}

		while (true)
		{
			int index = path.IndexOf('/');

			if (index == -1)
			{
				// It's the whole remaining segment.
				return current.FindChildNode(path);
			}

			ReadOnlySpan<char> childSegment;

			if (index == 0)
			{
				childSegment = ReadOnlySpan<char>.Empty;
			}
			else
			{
				childSegment = path.Slice(0, index);
			}

			var next = current.FindChildNode(childSegment);

			if (next == null)
			{
				// 404
				return null;
			}

			// Move along the path:
			path = path.Slice(index + 1);
			pathStartIndex += index + 1;
			current = next;
		}
	}

	/// <summary>
	/// Handle the given request.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <returns></returns>
	public ValueTask<bool> HandleRequest(HttpContext httpContext, Context basicContext)
	{
		// Max of N tokens (4 is the default).
		var tokenCount = 0;
		Span<TokenMarker> tokenSet = stackalloc TokenMarker[MaxTokenCount];
		var req = httpContext.Request;

		ReadOnlySpan<char> path;

		if (req.Path.HasValue)
		{
			path = req.Path.Value.AsSpan();
		}
		else
		{
			path = ReadOnlySpan<char>.Empty;
		}

		var node = Resolve(req.Method, path, basicContext, ref tokenCount, ref tokenSet);

		if (node == null)
		{
			return Run404(httpContext, basicContext);
		}

		return node.Run(httpContext, basicContext, tokenCount, ref tokenSet);
	}

	/// <summary>
	/// Displays a 404 page.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <returns></returns>
	public ValueTask<bool> Run404(HttpContext httpContext, Context basicContext)
	{
		httpContext.Response.StatusCode = 404;
		if (Status_404 != null)
		{
			Span<TokenMarker> tokenSet = stackalloc TokenMarker[MaxTokenCount];
			return Status_404.Run(httpContext, basicContext, 0, ref tokenSet);
		}

		return new ValueTask<bool>(true);
	}

	/// <summary>
	/// Converts a set of tokens to a heap List.
	/// </summary>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <param name="url"></param>
	/// <returns></returns>
	public static List<string> ConvertTokens(int tokenCount, string url, ref Span<TokenMarker> tokens)
	{
		var result = new List<string>();

		for (var i = 0; i < tokenCount; i++)
		{
			var token = tokens[i];

			if (token.LookupIndex == -1)
			{
				result.Add(url.Substring(token.Start, token.Length));
			}
			else
			{
				result.Add(RouterTokenLookup.Get(token.LookupIndex));
			}
		}

		return result;
	}

	/// <summary>
	/// Resolves the given URL to a terminal, allocating the token set as a 
	/// List on the heap making it more portable through async code. 
	/// The majority of routing generally avoids using this.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="url"></param>
	/// <returns></returns>
	public TerminalWithTokens? ResolveWithTokens(Context context, string url)
	{
		var tokenCount = 0;
		Span<TokenMarker> tokenSet = stackalloc TokenMarker[MaxTokenCount];

		var pathSpan = url.AsSpan();
		var queryStart = pathSpan.IndexOf('?');
		if (queryStart != -1)
		{
			pathSpan = pathSpan.Slice(0, queryStart);
		}

		var terminal = Resolve("GET", pathSpan, context, ref tokenCount, ref tokenSet);

		if (terminal == null)
		{
			return null;
		}

		return new TerminalWithTokens
		{
			TerminalNode = terminal,
			Tokens = tokenCount == 0 ? null : ConvertTokens(tokenCount, url, ref tokenSet)
		};
	}

	/// <summary>
	/// Gets the root route node for a given HTTP verb.
	/// </summary>
	/// <param name="method"></param>
	/// <returns></returns>
	public RouteNode GetByVerb(string method)
	{
		int verbIndex;

		switch (method)
		{
			case "GET":
				verbIndex = 0;
				break;
			case "POST":
				verbIndex = 1;
				break;
			case "DELETE":
				verbIndex = 2;
				break;
			case "PUT":
				verbIndex = 3;
				break;
			default:
				return null;
		}

		return TreesByVerb[verbIndex];
	}

	/// <summary>
	/// Resolves the given request in to a specific terminal node.
	/// </summary>
	/// <param name="method">GET, POST etc</param>
	/// <param name="path">The URL</param>
	/// <param name="context">Either a basic context or a regular context.</param>
	/// <param name="tokenCount"></param>
	/// <param name="tokenSet"></param>
	/// <returns>Null if the route did not resolve (it was a 404).</returns>
	public TerminalNode Resolve(string method, ReadOnlySpan<char> path, Context context, ref int tokenCount, ref Span<TokenMarker> tokenSet)
	{
		int verbIndex;

		switch (method)
		{
			case "GET":
				verbIndex = 0;
				break;
			case "POST":
				verbIndex = 1;
				break;
			case "DELETE":
				verbIndex = 2;
				break;
			case "PUT":
				verbIndex = 3;
				break;
			default:
				return null;
		}

		// The requested path is..
		int pathStartIndex;
		int pathMax = path.Length;

		if (pathMax > 0)
		{
			// If it starts with a /, ignore it.
			var startsWithSlash = (path[0] == '/');
			var endsWithSlash = (pathMax > 1 && path[pathMax - 1] == '/');

			if (startsWithSlash)
			{
				pathStartIndex = 1;

				if (endsWithSlash)
				{
					pathMax--; // Yes this is correct! The max only moves down 1 as the paths actual true end is only moving 1 place.
					path = path.Slice(1, pathMax - 1); // The subpath however gets 2 chars shorter.
				}
				else
				{
					path = path.Slice(1);
				}
			}
			else if (endsWithSlash)
			{
				pathMax--;
				path = path.Slice(0, pathMax);
				pathStartIndex = 0;
			}
			else
			{
				pathStartIndex = 0;
			}
		}
		else
		{
			pathStartIndex = 0;
		}

		// Initial node is..
		var current = TreesByVerb[verbIndex];

		while (true)
		{
			int index = path.IndexOf('/');

			if (index == -1)
			{
				// It's the whole remaining segment.
				var finalTerminal = current.FindChildNode(path);

				// Is current a capture token?
				if (finalTerminal != null)
				{
					if (finalTerminal.LocaleId != 0)
					{
						context.LocaleId = finalTerminal.LocaleId;
					}

					if (finalTerminal.IsRewrite)
					{
						// It's a rewrite. Tokens are reset to the contents of the rewrite node.
						var rewrite = (TerminalRewriteNode)finalTerminal;
						finalTerminal = rewrite.GoTo as IntermediateNode;
						tokenCount = rewrite.ReplaceTokens(ref tokenSet);
					}
					else if (finalTerminal.Capture)
					{
						// Yes - store the child segment as a token.
						// It is assumed that URLs that have too many tokens are never added to the tree
						// to minimise logic on the hot path as much as possible.
						tokenSet[tokenCount] = new TokenMarker(pathStartIndex, pathMax - pathStartIndex);
						tokenCount++;
					}
				}

				var terminal = finalTerminal as TerminalNode;
				return terminal;
			}

			ReadOnlySpan<char> childSegment;

			if (index == 0)
			{
				childSegment = ReadOnlySpan<char>.Empty;
			}
			else
			{
				childSegment = path.Slice(0, index);
			}

			var next = current.FindChildNode(childSegment);

			if (next == null)
			{
				// 404
				return null;
			}

			if (next.LocaleId != 0)
			{
				context.LocaleId = next.LocaleId;
			}
			
			if (next.IsRewrite)
			{
				// It's a rewrite. Tokens are reset to the contents of the rewrite node.
				var rewrite = (TerminalRewriteNode)next;
				next = rewrite.GoTo as IntermediateNode;
				tokenCount = rewrite.ReplaceTokens(ref tokenSet);
			}
			else if (next.Capture) 
			{
				// It's a capture token - store the child segment as a token.
				// It is assumed that URLs that have too many tokens are never added to the tree
				// to minimise logic on the hot path as much as possible.
				tokenSet[tokenCount] = new TokenMarker(pathStartIndex, index);
				tokenCount++;
			}

			// Move along the path:
			path = path.Slice(index + 1);
			pathStartIndex += index + 1;
			current = next;
		}
	}

}

/// <summary>
/// A marked region in a path where a token is located.
/// </summary>
public struct TokenMarker
{
	/// <summary>
	/// Index in the lookup, if this token is a static one.
	/// </summary>
	public readonly int LookupIndex;
	/// <summary>
	/// The start of the token region
	/// </summary>
	public readonly int Start;
	/// <summary>
	/// The end of the token region
	/// </summary>
	public readonly int Length;


	/// <summary>
	/// Creates a new token marker.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="length"></param>
	/// <param name="lookupIndex"></param>
	public TokenMarker(int start, int length, int lookupIndex = -1)
	{
		Start = start;
		Length = length;
		LookupIndex = lookupIndex;
	}
}

/// <summary>
/// A terminal node with converted token values.
/// </summary>
public struct TerminalWithTokens
{
	/// <summary>
	/// The terminal.
	/// </summary>
	public TerminalNode TerminalNode;

	/// <summary>
	/// Converted token set.
	/// </summary>
	public List<string> Tokens;
}
