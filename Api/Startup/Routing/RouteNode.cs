using Api.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Api.Startup.Routing;



/// <summary>
/// A node in the URL routing tree.
/// </summary>
public class RouteNode
{
	/// <summary>
	/// True if this is a rewrite node. 
	/// Checked on a very hot path and is almost always false 
	/// so this is marginally faster than a runtime cast.
	/// </summary>
	public readonly bool IsRewrite;

	/// <summary>
	/// Sets a locale in to the context.
	/// </summary>
	public uint LocaleId;

	/// <summary>
	/// A general purpose routing node.
	/// </summary>
	public RouteNode()
	{
		IsRewrite = this is TerminalRewriteNode;
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public virtual RouterNodeMetadata? GetMetadata()
	{
		return null;
	}

	/// <summary>
	/// Locates a child node by its name specified in the given span.
	/// </summary>
	public virtual IntermediateNode FindChildNode(ReadOnlySpan<char> childName)
	{
		return null;
	}

	/// <summary>
	/// True if this node has 1 or more child.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public bool HasChildren()
	{
		var kids = GetChildren();

		return kids != null && kids.Length > 0;
	}
	
	/// <summary>
	/// Gets the raw children of this node, if it has any.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public virtual IntermediateNode[] GetChildren()
	{
		return null;
	}

	/// <summary>
	/// Called after this node was built such that it 
	/// can set up any further connections with other built nodes if needed.
	/// </summary>
	public virtual void PostBuild()
	{
	}

	/// <summary>
	/// For each endpoint in the router, the given callback is executed.
	/// </summary>
	/// <param name="callback"></param>
	public virtual void ForEachEndpoint(Action<TerminalNode> callback)
	{
	}

}

/// <summary>
/// A request handler for a terminal. These are frequently generative.
/// </summary>
/// <param name="node"></param>
/// <param name="httpContext"></param>
/// <param name="basicContext"></param>
/// <param name="boundState"></param>
/// <param name="body"></param>
/// <returns></returns>
public delegate ValueTask<OutputType> TerminalMethod<State, OutputType, BodyType>(
	TerminalNode<State, OutputType, BodyType> node, 
	HttpContext httpContext, 
	Context basicContext, 
	State boundState,
	BodyType body
) where State : struct;

/// <summary>
/// A request handler for loading the body, if there is one.
/// </summary>
/// <param name="httpRequest"></param>
/// <returns></returns>
public delegate ValueTask<BodyType> TerminalBodyLoader<BodyType>(
	HttpRequest httpRequest
);

/// <summary>
/// A request handler for a terminal. These are frequently generative.
/// </summary>
/// <param name="node"></param>
/// <param name="httpContext"></param>
/// <param name="basicContext"></param>
/// <param name="boundState"></param>
/// <param name="body"></param>
/// <returns></returns>
public delegate ValueTask TerminalVoidMethod<State, BodyType>(
	TerminalVoidNode<State, BodyType> node, 
	HttpContext httpContext, 
	Context basicContext, 
	State boundState,
	BodyType body
) where State : struct;

/// <summary>
/// A request serialiser for a terminal. These are frequently generative.
/// </summary>
/// <param name="node"></param>
/// <param name="httpResponse"></param>
/// <param name="context"></param>
/// <param name="toSerialise"></param>
/// <returns></returns>
public delegate ValueTask TerminalSerialiser<State, OutputType, BodyType>(
	TerminalNode<State, OutputType, BodyType> node,
	HttpResponse httpResponse,
	Context context,
	OutputType toSerialise
) where State : struct;

/// <summary>
/// Binds the given terminal state from the given tokens.
/// </summary>
/// <typeparam name="State"></typeparam>
/// <param name="request"></param>
/// <param name="tokens"></param>
/// <returns></returns>
public delegate State TerminalBinderMethod<State>(HttpRequest request, ref Span<TokenMarker> tokens) where State : struct;

/// <summary>
/// A general use terminal node.
/// </summary>
public class TerminalNode : ArrayIntermediateNode
{
	/// <summary>
	/// Common JSON serialiser config.
	/// </summary>
	public static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{
		ContractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		},
		Formatting = Formatting.None
	};

	/// <summary>
	/// Used by the body loader when there is no body present. It usually gets optimised out completely.
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	public static ValueTask<EmptyTerminalState> ReadNothing(HttpRequest request)
	{
		return new ValueTask<EmptyTerminalState>(new EmptyTerminalState());
	}

	/// <summary>
	/// Deserialises the given type from the given http request.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="request"></param>
	/// <returns></returns>
	public static async ValueTask<T> ReadJsonBodyAsync<T>(HttpRequest request)
	{
		using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
		var body = await reader.ReadToEndAsync();
		return JsonConvert.DeserializeObject<T>(body);
	}
	
	/// <summary>
	/// Gets the common includes string from the query string.
	/// </summary>
	/// <param name="response"></param>
	/// <returns></returns>
	public static string GetIncludesString(HttpResponse response)
	{
		var request = response.HttpContext.Request;

		if (!request.Query.TryGetValue("includes", out StringValues sv))
		{
			return null;
		}

		return sv[sv.Count - 1];
	}

	/// <summary>
	/// The instance of the controller if there is one.
	/// </summary>
	public readonly object ControllerInstance;

	/// <summary>
	/// The node which constructed this one.
	/// </summary>
	public BuilderNode BuilderSource;

	/// <summary>
	/// Create a new terminal node.
	/// </summary>
	/// <param name="children"></param>
	/// <param name="exactMatch"></param>
	/// <param name="controllerInstance"></param>
	/// <param name="service"></param>
	/// <param name="fullRoute"></param>
	public TerminalNode(
		IntermediateNode[] children,
		string exactMatch,
		object controllerInstance,
		object service,
		string fullRoute
	) : base(children, fullRoute, exactMatch)
	{
		Service = service;
		ControllerInstance = controllerInstance;
		_ctxService = Services.Get<ContextService>();
	}

	/// <summary>
	/// For each endpoint in the router, the given callback is executed.
	/// </summary>
	/// <param name="callback"></param>
	public override void ForEachEndpoint(Action<TerminalNode> callback)
	{
		callback(this);
		base.ForEachEndpoint(callback);
	}

	/// <summary>
	/// If a content object is serialised, this is the service reference.
	/// </summary>
	public readonly object Service;

	/// <summary>
	/// Context service used
	/// </summary>
	private readonly ContextService _ctxService;

	/// <summary>
	/// Outputs a context.
	/// </summary>
	/// <param name="response"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	public async ValueTask OutputContext(HttpResponse response, Context context)
	{
		// Regenerate the contextual token:
		context.SendToken(response);

		response.ContentType = "application/json";
		await _ctxService.ToJson(context, response.Body);
	}
	
	/// <summary>
	/// Run this route node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public virtual ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		return new ValueTask<bool>(false);
	}
}

/// <summary>
/// A node at the end of a route.
/// </summary>
public class TerminalNode<State, OutputType, BodyType> : TerminalNode
	where State : struct
{
	/// <summary>
	/// The binder method, which binds URL args.
	/// </summary>
	public readonly TerminalBinderMethod<State> Binder;

	/// <summary>
	/// The method to run.
	/// </summary>
	public readonly TerminalMethod<State, OutputType, BodyType> Method;
	
	/// <summary>
	/// The body loader.
	/// </summary>
	public readonly TerminalBodyLoader<BodyType> BodyLoader;
	
	/// <summary>
	/// Serialises the output.
	/// </summary>
	public readonly TerminalSerialiser<State, OutputType, BodyType> Serialise;

	/// <summary>
	/// True if the full context is required.
	/// </summary>
	public readonly bool RequireFullContext;

	/// <summary>
	/// Create a new terminal node.
	/// </summary>
	/// <param name="children"></param>
	/// <param name="exactMatch"></param>
	/// <param name="bodyLoader"></param>
	/// <param name="binder">Binds values from the src.</param>
	/// <param name="method">Runs the actual endpoint using any bound values.</param>
	/// <param name="serialiser">Serialises the output.</param>
	/// <param name="controllerInstance"></param>
	/// <param name="svc"></param>
	/// <param name="requireFullContext"></param>
	/// <param name="fullRoute"></param>
	public TerminalNode(
		IntermediateNode[] children, 
		string exactMatch,
		TerminalBinderMethod<State> binder, 
		TerminalMethod<State, OutputType, BodyType> method,
		TerminalBodyLoader<BodyType> bodyLoader,
		TerminalSerialiser<State, OutputType, BodyType> serialiser, 
		object controllerInstance = null, 
		object svc = null,
		bool requireFullContext = false,
		string fullRoute = null
	) : base(children, exactMatch, controllerInstance, svc, fullRoute)
	{
		Method = method;
		BodyLoader = bodyLoader;
		Binder = binder;
		Serialise = serialiser;
		RequireFullContext = requireFullContext;
	}

	/// <summary>
	/// Run this route node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public override ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		State state = Binder(httpContext.Request, ref tokens);
		return InternalRunMethod(httpContext, basicContext, state);
	}

	private async ValueTask<bool> InternalRunMethod(HttpContext httpContext, Context basicContext, State state)
	{
		if (RequireFullContext)
		{
			basicContext = await httpContext.Request.GetContext(basicContext);
		}

		// Load the body:
		BodyType body = await BodyLoader(httpContext.Request);

		// Run the endpoint:
		var output = await Method(this, httpContext, basicContext, state, body);

		// Serialise the output:
		await Serialise(this, httpContext.Response, basicContext, output);

		return true;
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		return new RouterNodeMetadata()
		{
			Name = "",
			HasChildren = HasChildren(),
			ChildKey = ExactMatch,
			FullRoute = FullRoute,
			Type = "Method",
		};
	}

}

/// <summary>
/// A redirection node in the routing tree.
/// </summary>
public class TerminalRedirectNode : TerminalNode
{
	private readonly string Target;

	/// <summary>
	/// A redirection node in the routing tree, targeting the given target URL.
	/// </summary>
	public TerminalRedirectNode(IntermediateNode[] children,
		string target,
		string exactMatch,
		string fullRoute) : base (children, exactMatch, null, null, fullRoute)
	{
		Target = target;
	}

	/// <summary>
	/// The target of this node.
	/// </summary>
	/// <returns></returns>
	public string GetTarget()
	{
		return Target;
	}

	/// <summary>
	/// Run this route node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public override ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		var response = httpContext.Response;
		response.Headers.Location = Target;
		response.StatusCode = 302;
		return new ValueTask<bool>(true);
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		return new RouterNodeMetadata()
		{
			Name = Target,
			HasChildren = HasChildren(),
			ChildKey = ExactMatch,
			FullRoute = FullRoute,
			Type = "Redirect",
		};
	}

}

/// <summary>
/// A rewrite node in the routing tree.
/// </summary>
public class TerminalRewriteNode : TerminalNode
{
	private BuilderNode BuiltNode;

	/// <summary>
	/// The node that the request will be rewritten to.
	/// </summary>
	public RouteNode GoTo;

	/// <summary>
	/// The token index set.
	/// </summary>
	private readonly int[] TokenIndices;

	/// <summary>
	/// The token count.
	/// </summary>
	private readonly int TokenCount;

	/// <summary>
	/// A rewrite node in the routing tree, targeting the given target node.
	/// </summary>
	public TerminalRewriteNode(
		BuilderResolvedRoute route,
		string exactMatch,
		string fullRoute) : base (Array.Empty<IntermediateNode>(), exactMatch, null, null, fullRoute)
	{
		BuiltNode = route.Node;
		var tokens = route.Tokens;
		TokenCount = tokens == null ? 0 : tokens.Count;
		int[] tokenIndexes = null;

		if (TokenCount > 0)
		{
			tokenIndexes = new int[TokenCount];

			for (var i = 0; i < TokenCount; i++)
			{
				tokenIndexes[i] = RouterTokenLookup.Add(tokens[i]);
			}
		}

		TokenIndices = tokenIndexes;
	}

	/// <summary>
	/// A rewrite has just happened. 
	/// This causes the token set to be replaced with any in the target route.
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public int ReplaceTokens(ref Span<TokenMarker> tokens)
	{
		var count = TokenCount;

		for (var i = 0; i < count; i++)
		{
			// If count was non-zero then Tokens
			// must be not null as they're both readonly.
			tokens[i] = new TokenMarker(0,0, TokenIndices[i]);
		}

		return count;
	}

	/// <summary>
	/// Called after this node was built such that it 
	/// can set up any further connections with other built nodes if needed.
	/// </summary>
	public override void PostBuild()
	{
		// Get the built node from the route:
		GoTo = BuiltNode.GetBuiltNode();
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		return new RouterNodeMetadata()
		{
			Name = BuiltNode.FullRoute,
			HasChildren = HasChildren(),
			ChildKey = ExactMatch,
			FullRoute = FullRoute,
			Type = "Rewrite",
		};
	}

}

/// <summary>
/// A node which emits methods on demand for a much faster startup time.
/// </summary>
public class TerminalDeferedNode : TerminalNode
{
	/// <summary>
	/// True if the full context is required.
	/// </summary>
	public readonly bool RequireFullContext;

	private TerminalNode builtTerminal;

	private BuilderNode _node;
	private MethodInfo _method;

	/// <summary>
	/// Create a new terminal node.
	/// </summary>
	/// <param name="children"></param>
	/// <param name="exactMatch"></param>
	/// <param name="node"></param>
	/// <param name="method"></param>
	/// <param name="controllerInstance"></param>
	/// <param name="svc"></param>
	/// <param name="requireFullContext"></param>
	/// <param name="fullRoute"></param>
	public TerminalDeferedNode(
		IntermediateNode[] children,
		string exactMatch,
		BuilderNode node,
		MethodInfo method,
		object controllerInstance = null,
		object svc = null,
		bool requireFullContext = false,
		string fullRoute = null
	) : base(children, exactMatch, controllerInstance, svc, fullRoute)
	{
		_node = node;
		_method = method;
		RequireFullContext = requireFullContext;
	}

	/// <summary>
	/// Run this route node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public override ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		if (builtTerminal == null)
		{
			lock (this)
			{
				builtTerminal = _node.BuildTerminalMethodNode(_method, GetChildren());
			}
		}

		return builtTerminal.Run(httpContext, basicContext, tokenCount, ref tokens);
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		if (builtTerminal == null)
		{
			lock (this)
			{
				builtTerminal = _node.BuildTerminalMethodNode(_method, GetChildren());
			}
		}

		return builtTerminal.GetMetadata();
	}

}

/// <summary>
/// A node at the end of a route.
/// </summary>
public class TerminalVoidNode<State, BodyType> : TerminalNode
	where State : struct
{
	/// <summary>
	/// The binder method, which binds URL args.
	/// </summary>
	public readonly TerminalBinderMethod<State> Binder;

	/// <summary>
	/// The method to run.
	/// </summary>
	public readonly TerminalVoidMethod<State, BodyType> Method;

	/// <summary>
	/// The body loader.
	/// </summary>
	public readonly TerminalBodyLoader<BodyType> BodyLoader;
	
	/// <summary>
	/// True if the full context is required.
	/// </summary>
	public readonly bool RequireFullContext;

	/// <summary>
	/// Create a new terminal node.
	/// </summary>
	/// <param name="children"></param>
	/// <param name="exactMatch"></param>
	/// <param name="bodyLoader"></param>
	/// <param name="binder">Binds values from the src.</param>
	/// <param name="method">Runs the actual endpoint using any bound values.</param>
	/// <param name="controllerInstance"></param>
	/// <param name="requireFullContext"></param>
	/// <param name="fullRoute"></param>
	public TerminalVoidNode(
		IntermediateNode[] children,
		string exactMatch,
		TerminalBinderMethod<State> binder,
		TerminalVoidMethod<State, BodyType> method,
		TerminalBodyLoader<BodyType> bodyLoader,
		object controllerInstance = null,
		bool requireFullContext = false,
		string fullRoute = null
	) : base(children, exactMatch, controllerInstance, null, fullRoute)
	{
		BodyLoader = bodyLoader;
		Method = method;
		Binder = binder;
		RequireFullContext = requireFullContext;
	}

	/// <summary>
	/// Run this route node.
	/// </summary>
	/// <param name="httpContext"></param>
	/// <param name="basicContext"></param>
	/// <param name="tokenCount"></param>
	/// <param name="tokens"></param>
	/// <returns></returns>
	public override ValueTask<bool> Run(HttpContext httpContext, Context basicContext, int tokenCount, ref Span<TokenMarker> tokens)
	{
		State state = Binder(httpContext.Request, ref tokens);
		return InternalRunMethod(httpContext, basicContext, state);
	}

	private async ValueTask<bool> InternalRunMethod(HttpContext httpContext, Context basicContext, State state)
	{
		if (RequireFullContext)
		{
			basicContext = await httpContext.Request.GetContext(basicContext);
		}

		BodyType body = await BodyLoader(httpContext.Request);
		await Method(this, httpContext, basicContext, state, body);
		return true;
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		return new RouterNodeMetadata()
		{
			Name = "",
			ChildKey = ExactMatch,
			HasChildren = HasChildren(),
			FullRoute = FullRoute,
			Type = "Method",
		};
	}

}

/// <summary>
/// Non-root nodes in the tree are all intermediates.
/// </summary>
public class IntermediateNode : RouteNode
{
	/// <summary>
	/// True if  the value of this node should be captured as a token.
	/// </summary>
	public readonly bool Capture;

	/// <summary>
	/// An exact match string for "this" node.
	/// </summary>
	public readonly string ExactMatch;

	/// <summary>
	/// The complete original route to this node.
	/// </summary>
	public readonly string FullRoute;

	/// <summary>
	/// Create a new intermediate node.
	/// </summary>
	/// <param name="exactMatch"></param>
	/// <param name="fullRoute"></param>
	public IntermediateNode(string fullRoute, string exactMatch)
	{
		FullRoute = fullRoute;

		if (exactMatch == null)
		{
			Capture = true;
		}
		else
		{
			ExactMatch = exactMatch;
		}
	}

	/// <summary>
	/// Gets metadata about this node. Root nodes have none and return null.
	/// </summary>
	/// <returns></returns>
	public override RouterNodeMetadata? GetMetadata()
	{
		return new RouterNodeMetadata()
		{
			Name = "",
			HasChildren = HasChildren(),
			ChildKey = ExactMatch,
			FullRoute = FullRoute,
			Type = "Group",
		};
	}

}

/// <summary>
/// Used when the possible children is a small set.
/// </summary>
public class ArrayIntermediateNode : IntermediateNode
{
	/// <summary>
	/// The children set, sorted alphabetically with wildcards always last.
	/// </summary>
	private readonly IntermediateNode[] Children;

	/// <summary>
	/// </summary>
	/// <param name="childSet"></param>
	/// <param name="fullRoute"></param>
	/// <param name="exactMatch"></param>
	public ArrayIntermediateNode(IntermediateNode[] childSet, string fullRoute, string exactMatch) : base(fullRoute, exactMatch)
	{
		Children = childSet;
	}

	/// <summary>
	/// For each endpoint in the router, the given callback is executed.
	/// </summary>
	/// <param name="callback"></param>
	public override void ForEachEndpoint(Action<TerminalNode> callback)
	{
		for (var i = 0; i < Children.Length; i++)
		{
			Children[i].ForEachEndpoint(callback);
		}
	}

	/// <summary>
	/// Called after this node was built such that it 
	/// can set up any further connections with other built nodes if needed.
	/// </summary>
	public override void PostBuild()
	{
		for (var i = 0; i < Children.Length; i++)
		{
			var child = Children[i];
			child.PostBuild();
		}
	}

	/// <summary>
	/// Locates a child node by its name specified in the given span.
	/// </summary>
	public override IntermediateNode FindChildNode(ReadOnlySpan<char> childName)
	{
		for(var i=0;i<Children.Length;i++)
		{
			var child = Children[i];
			
			if(child.Capture)
			{
				// It's a wildcard node (ExactMatch is null).
				return child;
			}
			
			if(childName.SequenceEqual(child.ExactMatch))
			{
				return child;
			}
		}
		
		// No matches.
		return null;
	}

	/// <summary>
	/// Gets the raw children of this node.
	/// </summary>
	/// <returns></returns>
	public override IntermediateNode[] GetChildren()
	{
		return Children;
	}
}

/// <summary>
/// The very root of the routing tree.
/// </summary>
public class RootNode : RouteNode
{
	/// <summary>
	/// The children set, sorted alphabetically with wildcards always last.
	/// </summary>
	private readonly IntermediateNode[] Children;
	
	/// <summary>
	/// </summary>
	/// <param name="childSet"></param>
	public RootNode(IntermediateNode[] childSet)
	{
		Children = childSet;
	}

	/// <summary>
	/// For each endpoint in the router, the given callback is executed.
	/// </summary>
	/// <param name="callback"></param>
	public override void ForEachEndpoint(Action<TerminalNode> callback)
	{
		for (var i = 0; i < Children.Length; i++)
		{
			Children[i].ForEachEndpoint(callback);
		}
	}

	/// <summary>
	/// Called after this node was built such that it 
	/// can set up any further connections with other built nodes if needed.
	/// </summary>
	public override void PostBuild()
	{
		for (var i = 0; i < Children.Length; i++)
		{
			var child = Children[i];
			child.PostBuild();
		}
	}

	/// <summary>
	/// Locates a child node by its name specified in the given span.
	/// </summary>
	public override IntermediateNode FindChildNode(ReadOnlySpan<char> childName)
	{
		for(var i=0;i<Children.Length;i++)
		{
			var child = Children[i];
			
			if(child.Capture)
			{
				// It's a wildcard node (ExactMatch is null).
				return child;
			}
			
			if(childName.SequenceEqual(child.ExactMatch))
			{
				return child;
			}
		}
		
		// No matches.
		return null;
	}

	/// <summary>
	/// Gets the raw children of this node.
	/// </summary>
	/// <returns></returns>
	public override IntermediateNode[] GetChildren()
	{
		return Children;
	}
}