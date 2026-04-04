using Api.AvailableEndpoints;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Api.Startup.Routing;

/// <summary>
/// Used to construct a static router instance.
/// </summary>
public class RouterBuilder
{
	/// <summary>
	/// The main router builder. You can ask it about current endpoints and also build new ones.
	/// </summary>
	public static RouterBuilder BuiltInBuilder;

	/// <summary>
	/// The set of nodes by http verb, the same arrangement as the main router.
	/// </summary>
	private BuilderNode[] NodesByVerb;

	private RouterBuilder(RouterBuilder copyFrom)
	{
		// Create the nbv set:
		NodesByVerb = new BuilderNode[5];

		for (var i = 0; i < NodesByVerb.Length; i++)
		{
			if (copyFrom == null)
			{
				NodesByVerb[i] = new BuilderNode();
			}
			else
			{
				NodesByVerb[i] = copyFrom.NodesByVerb[i].Clone();
			}
		}

		NodesByVerb[0].HttpVerb = "GET";
		NodesByVerb[1].HttpVerb = "POST";
		NodesByVerb[2].HttpVerb = "DELETE";
		NodesByVerb[3].HttpVerb = "PUT";
		NodesByVerb[4].HttpVerb = "CONNECT";
	}

	private static bool _rebuildRequested = false;
	private static object _rebuildLock = new object();

	/// <summary>
	/// Ask the router to rebuild. Often requested repeatedly in quick succession, 
	/// so this internally buffers and will at most be a 100ms delay.
	/// </summary>
	public static void RequestRebuild()
	{
		if (BuiltInBuilder == null)
		{
			// First run has not happened yet. Ignore this request.
			return;
		}

		lock (_rebuildLock)
		{
			if (_rebuildRequested)
			{
				// Already one pending.
				return;
			}

			_rebuildRequested = true;
		}

		_ = Task.Run(async () => {

			await Task.Delay(100);

			lock (_rebuildLock)
			{
				_rebuildRequested = false;
			}

			await Rebuild();
			
		});
	}
	
	/// <summary>
	/// Called once to do the initial setup of the router builder.
	/// </summary>
	/// <returns></returns>
	public static async ValueTask Start()
	{
		var builder = new RouterBuilder(null);
		builder.AddBuiltInControllers();
		BuiltInBuilder = builder;

		// Build the base one such that it can reuse the built controllers in all future calls:
		builder.Build();

		await Rebuild();
	}

	/// <summary>
	/// Rebuilds the route set and extends it with any custom extensions.
	/// The new router is then activated.
	/// </summary>
	/// <returns></returns>
	public static async ValueTask Rebuild()
	{
		if (BuiltInBuilder == null)
		{
			// First run has not happened yet. Ignore this request.
			return;
		}

		var builder = new RouterBuilder(BuiltInBuilder);

		builder = await Events.Router.CollectRoutes.Dispatch(new Context(1,0,1), builder);

		if (builder == null)
		{
			// Soft rejection.
			return;
		}

		// Build & replace the router now:
		builder.Build();
	}

	/// <summary>
	/// Gets the index of the given http method in the verb array.
	/// </summary>
	/// <param name="method"></param>
	/// <returns></returns>
	public int GetVerbIndex(string method)
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
			case "CONNECT":
				verbIndex = 4;
				break;
			default:
				verbIndex = -1;
			break;
		}

		return verbIndex;
	}

	/// <summary>
	/// Builds the router and sets it to the CurrentRouter field when it's ready.
	/// </summary>
	public void Build()
	{
		var routeNodeSet = new RouteNode[NodesByVerb.Length];

		for (var i = 0; i < NodesByVerb.Length; i++)
		{
			routeNodeSet[i] = NodesByVerb[i].Build(); 
		}

		// 2nd pass which connects any now built nodes (for the purposes of rewrites typically).
		for (var i = 0; i < NodesByVerb.Length; i++)
		{
			routeNodeSet[i].PostBuild();
		}

		var router = new Router(routeNodeSet);

		if (Status_404 != null)
		{
			router.Status_404 = Status_404.Build(null);
		}

		Router.CurrentRouter = router;
	}

	/// <summary>
	/// The behaviour to use for a 404 response.
	/// </summary>
	public TerminalBehaviour Status_404;

	/// <summary>
	/// Adds all the built in controllers.
	/// </summary>
	public void AddBuiltInControllers()
	{
		// Locate all builtin AutoController objects
		var allTypes = typeof(RouterBuilder).Assembly.DefinedTypes;
		var availableEndpoints = Services.Get<AvailableEndpointService>();

		var routes = availableEndpoints.GetBuiltIn();
		AddRoutes(routes);
	}

	/// <summary>
	/// Convenience method for adding a list of routes.
	/// </summary>
	/// <param name="routes"></param>
	public void AddRoutes(List<HttpMethodInfo> routes)
	{
		foreach (var route in routes)
		{
			AddRoute(
				route.Route,
				route.Method,
				route.Controller.GetInstance(),
				route.Verb
			);
		}
	}

	/// <summary>
	/// Adds the given route to the node tree.
	/// </summary>
	/// <param name="route"></param>
	/// <param name="method"></param>
	/// <param name="controllerInstance"></param>
	/// <param name="httpVerb"></param>
	public void AddRoute(string route, MethodInfo method, object controllerInstance, string httpVerb)
	{
		// NB: The toLower() will lowercase the tokens too.
		// Binding of the parameters thus needs to be case insensitive.
		route = route.Trim().ToLower();
		httpVerb = httpVerb.ToUpper();

		var verbIndex = GetVerbIndex(httpVerb);

		if (verbIndex == -1)
		{
			Console.WriteLine("Ignored a http verb '" + httpVerb + "' used by route '" + route + "'");
			return;
		}

		var tree = NodesByVerb[verbIndex];
		tree.AddRoute(route, method, controllerInstance);
	}

	/// <summary>
	/// Adds a rewrite.
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	public void AddRewrite(string from, string to)
	{
		var route = from.Trim().ToLower();
		GetGetNode().AddRewrite(from, to);
	}

	/// <summary>
	/// Gets a builder node by uppercase verb, e.g. "GET".
	/// </summary>
	/// <param name="verb"></param>
	/// <returns></returns>
	public BuilderNode GetVerbNode(string verb)
	{
		return NodesByVerb[GetVerbIndex(verb)];
	}
	
	/// <summary>
	/// Gets the "GET" builder node.
	/// </summary>
	/// <returns></returns>
	public BuilderNode GetGetNode()
	{
		return NodesByVerb[GetVerbIndex("GET")];
	}

	/// <summary>
	/// Adds a 302 non-permanent redirect.
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	public void AddRedirect(string from, string to)
	{
		var route = from.Trim().ToLower();
		var tree = NodesByVerb[GetVerbIndex("GET")];
		tree.AddRedirect(from, to);
	}

}

/// <summary>
/// The behaviour for a router terminal.
/// </summary>
public class TerminalBehaviour 
{

	/// <summary>
	/// Clones this behaviour.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public virtual TerminalBehaviour Clone()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// True if the behaviours are the same type and target.
	/// </summary>
	/// <returns></returns>
	public virtual bool Equals(TerminalBehaviour behaviour)
	{
		return false;
	}

	/// <summary>
	/// Constructs this node in to a readonly tree node.
	/// </summary>
	/// <returns></returns>
	public virtual TerminalNode Build(BuilderNode node)
	{
		throw new NotImplementedException();
	}

}

/// <summary>
/// Used to create a redirection.
/// </summary>
public class TerminalRedirect  : TerminalBehaviour 
{

	/// <summary>
	/// The redirection target.
	/// </summary>
	public string RedirectTo;

	/// <summary>
	/// Creates a new terminal redirect.
	/// </summary>
	/// <param name="redirectTo"></param>
	public TerminalRedirect(string redirectTo)
	{
		RedirectTo = redirectTo;
	}

	/// <summary>
	/// Clones this behaviour.
	/// </summary>
	/// <returns></returns>
	public override TerminalBehaviour Clone()
	{
		return new TerminalRedirect(RedirectTo);
	}

	/// <summary>
	/// True if the behaviours are the same type and target.
	/// </summary>
	/// <returns></returns>
	public override bool Equals(TerminalBehaviour behaviour)
	{
		var node = behaviour as TerminalRedirect;
		return node != null && node.RedirectTo == RedirectTo;
	}

	/// <summary>
	/// Constructs this node in to a readonly tree node.
	/// </summary>
	/// <returns></returns>
	public override TerminalNode Build(BuilderNode node)
	{
		return new TerminalRedirectNode(
				node.BuildChildren(),
				RedirectTo,
				node.IsToken ? null : node.Text,
				node.FullRoute
			);
	}

}

/// <summary>
/// Used to create an API endpoint.
/// </summary>
public class TerminalMethod  : TerminalBehaviour 
{

	/// <summary>
	/// The redirection target.
	/// </summary>
	public MethodInfo Method;

	/// <summary>
	/// Creates a new terminal method.
	/// </summary>
	/// <param name="method"></param>
	public TerminalMethod(MethodInfo method)
	{
		Method = method;
	}

	/// <summary>
	/// Clones this behaviour.
	/// </summary>
	/// <returns></returns>
	public override TerminalBehaviour Clone()
	{
		return new TerminalMethod(Method);
	}

	/// <summary>
	/// True if the behaviours are the same type and target.
	/// </summary>
	/// <returns></returns>
	public override bool Equals(TerminalBehaviour behaviour)
	{
		var mtd = behaviour as TerminalMethod;
		return mtd != null && mtd.Method == Method;
	}

	/// <summary>
	/// Constructs this node in to a readonly tree node.
	/// </summary>
	/// <returns></returns>
	public override TerminalNode Build(BuilderNode node)
	{
#if DEBUG
		return node.BuildDeferedNode(Method);
#else
		return node.BuildTerminalMethodNode(Method);
#endif
	}

}

/// <summary>
/// A rewrite at this node.
/// </summary>
public class TerminalRewrite  : TerminalBehaviour 
{
	/// <summary>
	/// A node to rewrite this request to. 
	/// Effectively teleports to the targeted node, replacing the 
	/// current token state with the ones in this rewrite metadata.
	/// </summary>
	public BuilderResolvedRoute? Rewrite;

	/// <summary>
	/// Creates a new terminal method.
	/// </summary>
	/// <param name="rewrite"></param>
	public TerminalRewrite(BuilderResolvedRoute? rewrite)
	{
		Rewrite = rewrite;
	}

	/// <summary>
	/// True if the behaviours are the same type and target.
	/// </summary>
	/// <returns></returns>
	public override bool Equals(TerminalBehaviour behaviour)
	{
		var node = behaviour as TerminalRewrite;
		// Not accurate but it achieves the goal for now!
		return node != null && node.Rewrite != null && Rewrite != null;
	}

	/// <summary>
	/// Clones this behaviour.
	/// </summary>
	/// <returns></returns>
	public override TerminalBehaviour Clone()
	{
		return new TerminalRewrite(Rewrite);
	}

}

/// <summary>
/// A node in the builder tree. Unlike the main router, these are flexible.
/// </summary>
public class BuilderNode
{
	/// <summary>
	/// Parent builder node.
	/// </summary>
	public BuilderNode Parent;

	/// <summary>
	/// An instance of the controller from which the TermialMethod came.
	/// This is set on the built terminal node.
	/// </summary>
	public object ControllerInstance;

	/// <summary>
	/// The terminal behaviour, if this node has one.
	/// </summary>
	public TerminalBehaviour Terminal;

	/// <summary>
	/// The http verb in use at this terminal.
	/// </summary>
	public string HttpVerb;

	/// <summary>
	/// A constructed terminal node. These hold controller instances as necessary.
	/// </summary>
	public TerminalNode ConstructedTerminal;

	/// <summary>
	/// Text at this node. Can be a {token}
	/// </summary>
	public string Text;
	
	/// <summary>
	/// The full route to this node.
	/// </summary>
	public string FullRoute;

	/// <summary>
	/// The children of this node, in no particular order.
	/// </summary>
	public List<BuilderNode> Children = new List<BuilderNode>();

	/// <summary>
	/// A generated binding method.
	/// </summary>
	private object _constructedBinder;

	/// <summary>
	/// A generated invoke method.
	/// </summary>
	private object _constructedMethod;
	
	/// <summary>
	/// A generated invoke method.
	/// </summary>
	private object _constructedBodyLoader;

	/// <summary>
	/// A generated invoke method.
	/// </summary>
	private object _constructedSerialiser;

	/// <summary>
	/// If this terminal outputs 1 piece of content, this is the service reference
	/// that will be used for JSON serialisation.
	/// </summary>
	private object SingularContentService;

	/// <summary>
	/// The terminal state type, which is just a permanent empty struct if an endpoint has no bindings.
	/// </summary>
	private Type _terminalStateType = typeof(EmptyTerminalState);

	private Dictionary<string, FieldBuilder> _stateFields;

	/// <summary>
	/// True if the method has a Context in its args.
	/// </summary>
	private bool _requiresFullContext;

	/// <summary>
	/// Set as the name of a token if 1 child is a token.
	/// </summary>
	private string ChildTokenName;

	private ModuleBuilder Builder;
	private TypeBuilder TypeBuilder;
	private TypeBuilder StateTypeBuilder;

	/// <summary>
	/// Sets the given behaviour as this routes terminal.
	/// </summary>
	/// <param name="behaviour"></param>
	public void SetTerminal(TerminalBehaviour behaviour)
	{
		if (behaviour == null)
		{
			Terminal = null;
			return;
		}

		if (Terminal != null)
		{
			// Are they the same? If so, do nothing.
			if (!behaviour.Equals(Terminal))
			{
				throw new Exception("There's already a terminal at this route (" + FullRoute + ")");
			}
		}

		Terminal = behaviour;
	}

	/// <summary>
	/// A deep clone of this node. Retains any cached built information.
	/// </summary>
	/// <returns></returns>
	public BuilderNode Clone()
	{
		var copy = new BuilderNode()
		{
			ChildTokenName = ChildTokenName,
			_requiresFullContext = _requiresFullContext,
			SingularContentService = SingularContentService,
			ConstructedTerminal = ConstructedTerminal, // When building this is the only one used.
			Terminal = Terminal == null ? null : Terminal.Clone(),
			ControllerInstance = ControllerInstance,
			FullRoute = FullRoute,
			HttpVerb = HttpVerb,
			Text = Text
		};

		if (Children != null)
		{
			foreach (var child in Children)
			{
				var childClone = child.Clone();
				childClone.Parent = copy;
				copy.Children.Add(childClone);
			}
		}

		return copy;
	}

	/// <summary>
	/// True if this node is a capture token.
	/// </summary>
	public bool IsToken => Text != null && Text.EndsWith('}') && (Text.StartsWith('{') || Text.StartsWith("${"));

	/// <summary>
	/// Collect all token names in this route. Null if there are none.
	/// </summary>
	/// <returns></returns>
	public List<string> GetAllTokens()
	{
		List<string> result = null;

		// Pushing them in backwards:
		var cur = this;
		while (cur != null)
		{
			if (cur.IsToken)
			{
				var text = cur.Text;
				var tokenName = text.StartsWith('{') ? text.Substring(1, text.Length - 2) : text.Substring(2, text.Length - 3);

				if (result == null)
				{
					result = new List<string>();
				}

				result.Add(tokenName);
			}

			cur = cur.Parent;
		}

		if (result != null)
		{
			result.Reverse();
		}

		return result;
	}

	/// <summary>
	/// Gets the token index of this node.
	/// </summary>
	/// <returns></returns>
	public int GetSelfTokenIndex()
	{
		var current = Parent;
		var index = 0;

		while (current != null)
		{
			if (current.IsToken)
			{
				index++;
			}
			current = current.Parent;
		}

		return index;
	}

	/// <summary>
	/// The name of the token, if it is one.
	/// </summary>
	public string TokenName => Text == null ? null : Text.Substring(1, Text.Length - 2);

	/// <summary>
	/// Gets the terminal state type during compilation.
	/// </summary>
	/// <returns></returns>
	private Type GetTerminalStateType()
	{
		if (StateTypeBuilder != null)
		{
			return StateTypeBuilder;
		}

		return typeof(EmptyTerminalState);
	}

	private RouteNode BuiltNode;

	/// <summary>
	/// The most recent built route node.
	/// </summary>
	/// <returns></returns>
	public RouteNode GetBuiltNode()
	{
		return BuiltNode;
	}

	/// <summary>
	/// Builds this node as a routing node.
	/// </summary>
	/// <returns></returns>
	public RouteNode Build()
	{
		BuiltNode = BuildInternal();
		return BuiltNode;
	}

	private RouteNode BuildInternal()
	{
		if (Text == null)
		{
			// It's a root node.
			return new RootNode(BuildChildren());
		}

		if (ConstructedTerminal != null)
		{
			return ConstructedTerminal;
		}

		if (Terminal != null)
		{
			return Terminal.Build(this);
		}

		return new ArrayIntermediateNode(BuildChildren(), FullRoute, IsToken ? null : Text);
	}

	/// <summary>
	/// Constructs a defered terminal method, which builds on demand.
	/// </summary>
	/// <param name="method"></param>
	/// <returns></returns>
	public TerminalNode BuildDeferedNode(MethodInfo method)
	{
		return new TerminalDeferedNode(
			BuildChildren(),
			IsToken ? null : Text,
			this,
			method,
			ControllerInstance,
			SingularContentService,
			_requiresFullContext,
			FullRoute
		);
	}

	/// <summary>
	/// Constructs a terminal method.
	/// </summary>
	/// <returns></returns>
	public TerminalNode BuildTerminalMethodNode(MethodInfo method, IntermediateNode[] children = null)
	{
		if (method.IsGenericMethod)
		{
			throw new Exception("Can't use generic methods as a controller route as the router does not know what types can possibly be given.");
		}

		var bodyType = GetBodyType(method);

		if (_constructedMethod == null)
		{
			var outputType = GetOutputType(method);

			if (QuitIfServiceDoesntExist(method, outputType))
			{
				return null;
			}

			Validate(method);
			_requiresFullContext = IsFullContextRequired(method);
			InitModule();

			if (IsVoidNode(method))
			{
				BuildBinder(method);
				BuildBodyLoader(bodyType);
				BuildVoidTerminalMethod(bodyType, method);

				// Close the type and set the values now.
				var mainType = TypeBuilder.CreateType();

				_terminalStateType = (StateTypeBuilder == null) ? typeof(EmptyTerminalState) : StateTypeBuilder.CreateType();

				_constructedMethod = mainType.GetMethod("TerminalInvoke")
					.CreateDelegate(
						typeof(TerminalVoidMethod<,>)
							.MakeGenericType(_terminalStateType, bodyType)
					);

				_constructedBinder = mainType.GetMethod("TerminalBinder")
					.CreateDelegate(
						typeof(TerminalBinderMethod<>)
							.MakeGenericType(_terminalStateType)
					);
			}
			else
			{
				BuildBinder(method);
				BuildBodyLoader(bodyType);
				BuildTerminalMethod(bodyType, method, outputType);
				BuildSerialiser(outputType, bodyType);

				// Close the type and set the values now.
				var mainType = TypeBuilder.CreateType();

				_terminalStateType = (StateTypeBuilder == null) ? typeof(EmptyTerminalState) : StateTypeBuilder.CreateType();

				_constructedMethod = mainType.GetMethod("TerminalInvoke")
					.CreateDelegate(
						typeof(TerminalMethod<,,>)
							.MakeGenericType(_terminalStateType, outputType, bodyType)
					);

				_constructedBinder = mainType.GetMethod("TerminalBinder")
					.CreateDelegate(
						typeof(TerminalBinderMethod<>)
							.MakeGenericType(_terminalStateType)
					);

				var serialiserMethod = mainType.GetMethod("TerminalSerialiser");

				_constructedSerialiser = serialiserMethod
					.CreateDelegate(
						typeof(TerminalSerialiser<,,>)
						.MakeGenericType(_terminalStateType, outputType, bodyType)
					);
			}
		}

		var returnType = method.ReturnType;

		if (IsVoidNode(method))
		{
			// It's a void node.
			var voidNodeType = typeof(TerminalVoidNode<,>).MakeGenericType(_terminalStateType, bodyType);

			var voidObj = Activator.CreateInstance(voidNodeType, new object[] {
				children == null ? BuildChildren() : children,
				IsToken ? null : Text,
				_constructedBinder,
				_constructedMethod,
				_constructedBodyLoader,
				ControllerInstance,
				_requiresFullContext,
				FullRoute
			});

			ConstructedTerminal = voidObj as TerminalNode;
			ConstructedTerminal.BuilderSource = this;
			return ConstructedTerminal;
		}

		// It's a full node otherwise. Underlying return type is..
		var nonAsyncReturnType = returnType;

		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			nonAsyncReturnType = returnType.GetGenericArguments()[0];
		}

		// Spawn the type:
		var fullNodeType = typeof(TerminalNode<,,>).MakeGenericType(_terminalStateType, nonAsyncReturnType, bodyType);

		var obj = Activator.CreateInstance(fullNodeType, new object[] {
			children == null ? BuildChildren() : children,
			IsToken ? null : Text,
			_constructedBinder,
			_constructedMethod,
			_constructedBodyLoader,
			_constructedSerialiser,
			ControllerInstance,
			SingularContentService,
			_requiresFullContext,
			FullRoute
		});

		ConstructedTerminal = obj as TerminalNode;
		ConstructedTerminal.BuilderSource = this;
		return ConstructedTerminal;
	}

	/// <summary>
	/// Performs some validation on a controller method.
	/// </summary>
	/// <exception cref="Exception"></exception>
	private void Validate(MethodInfo method)
	{
		var returnType = method.ReturnType;

		// Some common validations
		if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
		{
			throw new Exception("Please use ValueTask in all controller return types. " +
				"The following controller method used Task instead: " + method.ToString() + " in " + method.DeclaringType.Name);
		}

		if (
		(returnType.IsArray && IsContentType(returnType.GetElementType())) ||
		(returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(List<>) && IsContentType(returnType.GetGenericArguments()[0]))
		)
		{
			throw new Exception("Avoid returning lists or arrays of content types from your endpoints. " +
			"Instead, return a ContentStream. You can create a stream from a list, " +
				"although you should generally aim to use the ContentStream API to its full performance potential. " +
				"The endpoint was: " + method.Name + " in " + method.DeclaringType.Name);
		}
	}

	private bool IsVoidNode(MethodInfo method)
	{
		var returnType = method.ReturnType;
		return (returnType == typeof(void) || returnType == typeof(ValueTask));
	}

	/// <summary>
	/// Gets the index of a token.
	/// </summary>
	/// <param name="tokenWithBrackets"></param>
	/// <returns></returns>
	private int GetTokenIndex(string tokenWithBrackets)
	{
		var current = this;

		while (current != null)
		{
			if (current.Text == tokenWithBrackets)
			{
				return current.GetSelfTokenIndex();
			}
			current = current.Parent;
		}

		// Token not found
		return -1;
	}

	private static FieldInfo _controllerInstanceFld;
	private static FieldInfo _serialiseConfig;
	private static MethodInfo _newtonsoftDeserialise;
	private static MethodInfo _newtonsoftSerialise;

	private bool IsFullContextRequired(MethodInfo method)
	{
		var parameters = method.GetParameters();

		for (var i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].ParameterType == typeof(Context))
			{
				return true;
			}
		}

		return false;
	}
	
	private Type GetBodyType(MethodInfo method)
	{
		if (HttpVerb == "GET" || HttpVerb == "DELETE" || HttpVerb == "CONNECT")
		{
			return typeof(EmptyTerminalState);
		}

		// POST or PUT

		var parameters = method.GetParameters();

		for (var i = 0; i < parameters.Length; i++)
		{
			var p = parameters[i];
			var fromBody = p.GetCustomAttribute<FromBodyAttribute>();

			if (fromBody != null)
			{
				return p.ParameterType;
			}
		}

		return typeof(EmptyTerminalState);
	}

	private static int ModuleCounter = 1;

	private void InitModule()
	{
		var assemblyName = new AssemblyName("$Route_" + ModuleCounter);

		// Create the assembly builder. If we're AOT compiling, it's saveable.
		AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
		Builder = assemblyBuilder.DefineDynamicModule("$Route_Module");
		TypeBuilder = Builder.DefineType("RouteMethods", System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);
		ModuleCounter++;
	}

	private void BuildBodyLoader(Type bodyType)
	{
		if (bodyType == typeof(EmptyTerminalState))
		{
			_constructedBodyLoader = typeof(TerminalNode)
				.GetMethod(nameof(TerminalNode.ReadNothing))
				.CreateDelegate(
					typeof(TerminalBodyLoader<EmptyTerminalState>)
				);
		}
		else
		{
			_constructedBodyLoader = typeof(TerminalNode).GetMethod("ReadJsonBodyAsync").MakeGenericMethod(new Type[] {
				bodyType
			})
			.CreateDelegate(
				typeof(TerminalBodyLoader<>)
					.MakeGenericType(bodyType)
			);
		}

	}

	private void BuildBinder(MethodInfo method)
	{
		if (_newtonsoftDeserialise == null)
		{
			_newtonsoftDeserialise = typeof(Newtonsoft.Json.JsonConvert)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(m =>
					m.Name == "DeserializeObject" &&
					m.IsGenericMethodDefinition &&
					m.GetParameters().Length == 1 &&
					m.GetParameters()[0].ParameterType == typeof(string)
				);

			_newtonsoftSerialise = typeof(Newtonsoft.Json.JsonConvert)
				.GetMethod("SerializeObject", BindingFlags.Public | BindingFlags.Static, new Type[] {
					typeof(object),
					typeof(JsonSerializerSettings)
				});

			// Static cfg field:
			_serialiseConfig = typeof(TerminalNode).GetField(nameof(TerminalNode.jsonSettings));
		}

		// Set _constructedBinder
		// If there are no tokens, the binder simply returns null.
		var parameters = method.GetParameters();
		var hasBindable = false;

		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];

			if (
				parameter.ParameterType == typeof(Context) || 
				parameter.ParameterType == typeof(HttpContext) || 
				parameter.ParameterType == typeof(HttpRequest) ||  
				parameter.ParameterType == typeof(HttpResponse)
			)
			{
				// Binder not needed fo these
				continue;
			}

			var fromRoute = parameter.GetCustomAttribute<FromRouteAttribute>();

			if (fromRoute == null)
			{
				// Not from the route.
				continue;
			}

			hasBindable = true;
			break;
		}

		ILGenerator il;

		if (!hasBindable)
		{
			// Bind method just returns null, and the type is typeof(EmptyTerminalState).
			StateTypeBuilder = null;
			_terminalStateType = typeof(EmptyTerminalState);

			var emptyMethodBuilder = TypeBuilder.DefineMethod(
				"TerminalBinder",
				MethodAttributes.Static | MethodAttributes.Public,
				CallingConventions.Standard,
				typeof(EmptyTerminalState),
				new Type[] {
					typeof(HttpRequest),
					typeof(Span<TokenMarker>).MakeByRefType()
				}
			);

			il = emptyMethodBuilder.GetILGenerator();
			var loc = il.DeclareLocal(typeof(ValueTask));
			il.Emit(OpCodes.Ldloca, loc);
			il.Emit(OpCodes.Initobj, typeof(EmptyTerminalState));
			il.Emit(OpCodes.Ldloc, loc);
			il.Emit(OpCodes.Ret);
			return;
		}

		// Start defining the state type (a struct)
		StateTypeBuilder = Builder.DefineType(
			"TerminalState",
			TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit,
			typeof(System.ValueType)
		);

		_stateFields = new Dictionary<string, FieldBuilder>();

		var methodBuilder = TypeBuilder.DefineMethod(
			"TerminalBinder",
			MethodAttributes.Static | MethodAttributes.Public,
			CallingConventions.Standard,
			StateTypeBuilder,
			new Type[] {
				typeof(HttpRequest),
				typeof(Span<TokenMarker>).MakeByRefType()
			}
		);

		il = methodBuilder.GetILGenerator();
		var stateLoc = il.DeclareLocal(StateTypeBuilder);
		il.Emit(OpCodes.Ldloca, stateLoc);
		il.Emit(OpCodes.Initobj, StateTypeBuilder);

		LocalBuilder pathLocal = null;

		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];

			if (
				parameter.ParameterType == typeof(Context) ||
				parameter.ParameterType == typeof(HttpContext) ||
				parameter.ParameterType == typeof(HttpRequest) ||
				parameter.ParameterType == typeof(HttpResponse)
			)
			{
				// Binder not needed for these
				continue;
			}

			var fromRoute = parameter.GetCustomAttribute<FromRouteAttribute>();

			if (fromRoute == null)
			{
				// Not from the route.
				continue;
			}

			if (pathLocal == null)
			{
				// var path = request.Path.Value.AsSpan();
				il.Emit(OpCodes.Ldarg_0); // request
				il.Emit(OpCodes.Callvirt, typeof(HttpRequest).GetProperty("Path").GetGetMethod()); // Path

				// Addressify it
				var pathStrLoc = il.DeclareLocal(typeof(PathString));
				il.Emit(OpCodes.Stloc, pathStrLoc);
				il.Emit(OpCodes.Ldloca, pathStrLoc);
				il.Emit(OpCodes.Callvirt, typeof(PathString).GetProperty("Value").GetGetMethod()); // Value

				var toSpan = typeof(System.MemoryExtensions).GetMethod("AsSpan", new[] { typeof(string) }); // .AsSpan()
				il.Emit(OpCodes.Call, toSpan);
				pathLocal = il.DeclareLocal(typeof(ReadOnlySpan<char>));
				il.Emit(OpCodes.Stloc, pathLocal);
			}

			// Define a field in the StateTypeBuilder to hold this value:
			var fldName = "BindArg_" + i;
			var bindField = StateTypeBuilder.DefineField(fldName, parameter.ParameterType, FieldAttributes.Public);
			_stateFields[fldName] = bindField;

			// Establish which Nth route arg it is.
			var offset = GetTokenIndex("{" + parameter.Name.ToLower() + "}");

			if (offset == -1)
			{
				throw new Exception("Token could not be found: " + parameter.Name + " (in " + method.Name + ")");
			}

			EmitReadFromSpan(parameter.ParameterType, offset, il, bindField, pathLocal, stateLoc);
		}

		il.Emit(OpCodes.Ldloc, stateLoc);
		il.Emit(OpCodes.Ret);
	}

	private void EmitReadFromSpan(Type valueType, int spanIndex, ILGenerator il, FieldInfo targetField, LocalBuilder pathLocal, LocalBuilder stateStruct)
	{
		var afterValueReady = il.DefineLabel();

		// this.field = {everything else};
		// The Stfld at the end does the field part.
		il.Emit(OpCodes.Ldloca, stateStruct);

		// Read the token info from the span at arg 1
		il.Emit(OpCodes.Ldarg, 1); // it's already an address
		il.Emit(OpCodes.Ldc_I4, spanIndex);
		il.Emit(OpCodes.Callvirt, typeof(Span<TokenMarker>).GetMethod("get_Item"));

		var tokenMarkerLoc = il.DeclareLocal(typeof(TokenMarker).MakeByRefType());
		il.Emit(OpCodes.Stloc, tokenMarkerLoc);

		// PathLocal holds a ReadOnlySpan<char> with the path in it.
		// TokenMarker (in tokenMarkerLoc) tells us which region of this path to select via .Slice
		// but only if it is not in the constant lookup.
		var nonLookupToken = il.DefineLabel();
		var afterTokenReady = il.DefineLabel();
		il.Emit(OpCodes.Ldloc, tokenMarkerLoc);
		il.Emit(OpCodes.Ldfld, typeof(TokenMarker).GetField(nameof(TokenMarker.LookupIndex)));
		il.Emit(OpCodes.Dup);
		il.Emit(OpCodes.Ldc_I4, -1);
		il.Emit(OpCodes.Ceq);
		il.Emit(OpCodes.Brtrue, nonLookupToken);

		// There is a lookup index on the stack at the mo.
		// Look it up in the constant table and then spanify it, unless the target is a string anyway.
		il.Emit(OpCodes.Call, typeof(RouterTokenLookup).GetMethod(nameof(RouterTokenLookup.Get)));

		if (valueType == typeof(string))
		{
			il.Emit(OpCodes.Br, afterValueReady);
		}
		else
		{
			// string -> span.
			var toSpan = typeof(System.MemoryExtensions)
									.GetMethod("AsSpan", new[] { typeof(string) });
			il.Emit(OpCodes.Call, toSpan);
			il.Emit(OpCodes.Br, afterTokenReady);
		}

		il.Emit(OpCodes.Pop);
		il.MarkLabel(nonLookupToken);

		// Pop the duplicated lookup index.
		il.Emit(OpCodes.Pop);

		// path.
		il.Emit(OpCodes.Ldloca, pathLocal);

		// Start:
		il.Emit(OpCodes.Ldloc, tokenMarkerLoc);
		il.Emit(OpCodes.Ldfld, typeof(TokenMarker).GetField(nameof(TokenMarker.Start)));

		// Length:
		il.Emit(OpCodes.Ldloc, tokenMarkerLoc);
		il.Emit(OpCodes.Ldfld, typeof(TokenMarker).GetField(nameof(TokenMarker.Length)));

		// path.Slice(start, length);
		il.Emit(OpCodes.Callvirt,
			typeof(ReadOnlySpan<char>)
			.GetMethod(
				nameof(ReadOnlySpan<char>.Slice),
				BindingFlags.Public | BindingFlags.Instance,
				new Type[] {
						typeof(int),
						typeof(int)
				}
			)
		);

		il.MarkLabel(afterTokenReady);

		if (valueType == typeof(string))
		{
			var ctor = typeof(string).GetConstructor(new Type[] {
				typeof(ReadOnlySpan<char>)
			});

			il.Emit(OpCodes.Newobj, ctor);
		}
		else
		{
			// Emit the parse.
			EmitParseValueFromSpan(valueType, il);
		}

		il.MarkLabel(afterValueReady);

		il.Emit(OpCodes.Stfld, targetField);
	}

	private void BuildVoidTerminalMethod(Type bodyType, MethodInfo method)
	{
		var stateType = GetTerminalStateType();

		var methodBuilder = TypeBuilder.DefineMethod(
			"TerminalInvoke",
			MethodAttributes.Static | MethodAttributes.Public,
			CallingConventions.Standard,
			typeof(ValueTask),
			new Type[] {
				typeof(TerminalVoidNode<,>).MakeGenericType(stateType, bodyType),
				typeof(HttpContext),
				typeof(Context),
				stateType,
				bodyType
			}
		);

		var il = methodBuilder.GetILGenerator();
		EmitInvokeTerminalMethod(il, bodyType, method);
	}

	private Type GetOutputType(MethodInfo method)
	{
		var outputType = method.ReturnType;

		if (outputType.IsGenericType && outputType.GetGenericTypeDefinition() == typeof(ValueTask<>))
		{
			outputType = outputType.GetGenericArguments()[0];
		}

		return outputType;
	}

	/// <summary>
	/// Returns true if we can quit building a terminal in the event that the associated service for the given output type does not exist.
	/// Otherwise, if the condition is found further down the line, then it will fail.
	/// This specifically happens with generic-typed content type endpoints which only exist on particular types - such as revision services only existing 
	/// on types that are actually revisionable, but endpoints using Revision exist on all AutoControllers.
	/// </summary>
	/// <param name="method"></param>
	/// <param name="outputType"></param>
	/// <returns></returns>
	private bool QuitIfServiceDoesntExist(MethodInfo method, Type outputType)
	{
		var nullable = Nullable.GetUnderlyingType(outputType);

		if (nullable != null)
		{
			return QuitIfServiceDoesntExist(method, nullable);
		}

		if (!IsContentType(outputType))
		{
			return false;
		}

		// It's a content type - get the svc:
		var svc = Services.GetByContentType(outputType);

		if (svc != null)
		{
			return false;
		}

		// We'll quietly quit if ignoring this condition is made possible by the attribute:
		var attr = method.GetCustomAttribute<OmitIfNoServiceAttribute>();
		return attr != null;
	}

	private void BuildTerminalMethod(Type bodyType, MethodInfo method, Type outputType)
	{
		var stateType = GetTerminalStateType();

		var methodBuilder = TypeBuilder.DefineMethod(
			"TerminalInvoke",
			MethodAttributes.Static | MethodAttributes.Public,
			CallingConventions.Standard,
			typeof(ValueTask<>).MakeGenericType(outputType),
			new Type[] {
				typeof(TerminalNode<,,>).MakeGenericType(stateType, outputType, bodyType),
				typeof(HttpContext),
				typeof(Context),
				stateType,
				bodyType
			}
		);

		var il = methodBuilder.GetILGenerator();
		EmitInvokeTerminalMethod(il, bodyType, method);
	}

	private void BuildSerialiser(Type outputType, Type bodyType)
	{
		var stateType = GetTerminalStateType();
		var terminalNodeType = typeof(TerminalNode<,,>).MakeGenericType(stateType, outputType, bodyType);

		var methodBuilder = TypeBuilder.DefineMethod(
			"TerminalSerialiser",
			MethodAttributes.Static | MethodAttributes.Public,
			CallingConventions.Standard,
			typeof(ValueTask),
			new Type[] {
				terminalNodeType,
				typeof(HttpResponse),
				typeof(Context),
				outputType
			}
		);

		var il = methodBuilder.GetILGenerator();

		EmitAsyncSerialise(terminalNodeType, outputType, il, (ILGenerator ilGen) => {
			il.Emit(OpCodes.Ldarg_3);
		}, false);

		il.Emit(OpCodes.Ret);
	}

	private void EmitEmptyValueTask(ILGenerator il)
	{
		var loc = il.DeclareLocal(typeof(ValueTask));
		il.Emit(OpCodes.Ldloca, loc);
		il.Emit(OpCodes.Initobj, typeof(ValueTask));
		il.Emit(OpCodes.Ldloc, loc);
	}

	/// <summary>
	/// Emits a serialise of the given return type.
	/// All routes leave a ValueTask on the stack.
	/// </summary>
	/// <param name="terminalNodeType"></param>
	/// <param name="outputType"></param>
	/// <param name="il"></param>
	/// <param name="emitValue"></param>
	/// <param name="notNull"></param>
	private void EmitAsyncSerialise(Type terminalNodeType, Type outputType, ILGenerator il, Action<ILGenerator> emitValue, bool notNull = false)
	{
		var setContentType = typeof(HttpResponse).GetProperty("ContentType").GetSetMethod();
		var setStatusCode = typeof(HttpResponse).GetProperty("StatusCode").GetSetMethod();
		var getBody = typeof(HttpResponse).GetProperty("Body").GetGetMethod();

		var baseNullable = Nullable.GetUnderlyingType(outputType);
		if (baseNullable != null)
		{
			// If value is null, 404. Otherwise, serialise it.
			emitValue(il);
			var nullableLoc = il.DeclareLocal(outputType);
			il.Emit(OpCodes.Stloc, nullableLoc);

			// .HasValue
			il.Emit(OpCodes.Ldloca, nullableLoc);
			var hasValueGetter = outputType.GetProperty("HasValue").GetGetMethod();
			il.Emit(OpCodes.Callvirt, hasValueGetter);
			var hasValueLabel = il.DefineLabel();
			var afterLabel = il.DefineLabel();

			// If it is not null, branch:
			il.Emit(OpCodes.Brtrue, hasValueLabel);

			{
				// It was null - 404 here.
				il.Emit(OpCodes.Ldarg_1); // response
				il.Emit(OpCodes.Ldc_I4, 404);
				il.Emit(OpCodes.Callvirt, setStatusCode);
				EmitEmptyValueTask(il);
				il.Emit(OpCodes.Br, afterLabel);
			}

			il.MarkLabel(hasValueLabel);

			{
				// Not null route
				EmitAsyncSerialise(terminalNodeType, baseNullable, il, (ILGenerator ilGen) => {
					il.Emit(OpCodes.Ldloca, nullableLoc);
					var valueGetter = outputType.GetProperty("Value").GetGetMethod();
					il.Emit(OpCodes.Callvirt, valueGetter);
				}, false);
			}

			il.MarkLabel(afterLabel);
			return;
		}
		
		if (!outputType.IsValueType && !notNull)
		{
			// It can be null. Check for it and potentially 404.
			emitValue(il);
			var valueLoc = il.DeclareLocal(outputType);
			il.Emit(OpCodes.Stloc, valueLoc);
			var hasValueLabel = il.DefineLabel();
			var afterLabel = il.DefineLabel();

			// == null
			il.Emit(OpCodes.Ldloc, valueLoc);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ceq);

			// If it is not null, branch:
			il.Emit(OpCodes.Brfalse, hasValueLabel);

			{
				// It was null - 404 here.
				il.Emit(OpCodes.Ldarg_1); // response
				il.Emit(OpCodes.Ldc_I4, 404);
				il.Emit(OpCodes.Callvirt, setStatusCode);
				EmitEmptyValueTask(il);
				il.Emit(OpCodes.Br, afterLabel);
			}

			il.MarkLabel(hasValueLabel);

			{
				// Not null route
				EmitAsyncSerialise(terminalNodeType, outputType, il, (ILGenerator ilGen) => {
					il.Emit(OpCodes.Ldloc, valueLoc);
				}, true);
			}

			il.MarkLabel(afterLabel);
			return;
		}

		if (outputType == typeof(FileContent))
		{
			var loc = il.DeclareLocal(outputType);
			emitValue(il);
			il.Emit(OpCodes.Stloc, loc);
			
			// Read the mime type
			il.Emit(OpCodes.Ldloca, loc);
			il.Emit(OpCodes.Ldarg_1); // HttpResponse
			il.Emit(OpCodes.Callvirt, typeof(FileContent).GetMethod(nameof(FileContent.SendAsResponse)));
			return;
		}

		// JSON mime type - not for nullable though.
		// This only occurs if we're actually emitting a serialisation.
		// context.Response.ContentType = "application/json";
		il.Emit(OpCodes.Ldarg_1); // response
		il.Emit(OpCodes.Ldstr, "application/json");
		il.Emit(OpCodes.Callvirt, setContentType);

		if (outputType == typeof(Context))
		{
			var outputContextMethod = typeof(TerminalNode).GetMethod(nameof(TerminalNode.OutputContext));

			// Ld terminal node, http response, api context:
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			emitValue(il);
			il.Emit(OpCodes.Callvirt, outputContextMethod);
			return;
		}

		if (outputType.IsGenericType && outputType.GetGenericTypeDefinition() == typeof(ContentStream<,>))
		{
			var writeJson = outputType.GetMethod("WriteJson");

			emitValue(il);
			
			// "this" is now on the stack.
			// In order to call it though, we need to push it in to a local & then 
			// load its address.
			var loc = il.DeclareLocal(outputType);
			il.Emit(OpCodes.Stloc, loc);

			// Load the 'this' addr now:
			il.Emit(OpCodes.Ldloca, loc);

			// Args of WriteJson next.
			il.Emit(OpCodes.Ldarg_2); // Context

			il.Emit(OpCodes.Ldarg_1); // HttpResponse.Body
			il.Emit(OpCodes.Callvirt, getBody);

			il.Emit(OpCodes.Ldarg_1); // A *HttpResponse*.
			il.Emit(OpCodes.Call, typeof(TerminalNode).GetMethod(nameof(TerminalNode.GetIncludesString)));

			il.Emit(OpCodes.Callvirt, writeJson); // write now
			
			return;
		}
		
		if (IsContentType(outputType))
		{
			// Note that it has already been established that the object is not null.

			// Get the service reference:
			SingularContentService = Services.GetByContentType(outputType);

			if (SingularContentService == null)
			{
				throw new Exception(outputType.Name + " was a content type being returned by an endpoint, " +
					"but the service for it could not be found. The service is used to safely and efficiently serialise the object.");
			}

			var toJsonMethod = SingularContentService.GetType().GetMethod("ToJson", BindingFlags.Public | BindingFlags.Instance, new Type[] {
				typeof(Context),
				outputType,
				typeof(System.IO.Stream),
				typeof(string)
			});

			if (toJsonMethod == null)
			{
				throw new Exception(outputType.Name + " was a content type being returned by an endpoint, " +
					"but the services ToJson method could not be found.");
			}

			// "this" (the service)
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, typeof(TerminalNode).GetField(nameof(TerminalNode.Service)));

			// Api context
			il.Emit(OpCodes.Ldarg_2);

			// The value
			emitValue(il);

			// Body stream
			il.Emit(OpCodes.Ldarg_1); // Response.Body
			il.Emit(OpCodes.Callvirt, getBody);

			il.Emit(OpCodes.Ldarg_1); // A *HttpResponse*.
			il.Emit(OpCodes.Call, typeof(TerminalNode).GetMethod(nameof(TerminalNode.GetIncludesString)));
			
			// Call ToJson(ctx, theObject, stream, includes)
			il.Emit(OpCodes.Callvirt, toJsonMethod);
			return;
		}

		// Otherwise, newtonsoft serialiser.

		il.Emit(OpCodes.Ldarg_1); // Response

		// Emit the value now:
		emitValue(il);

		if (outputType.IsValueType)
		{
			il.Emit(OpCodes.Box, outputType);
		}

		// Serialiser config arg:
		il.Emit(OpCodes.Ldsfld, _serialiseConfig);
		il.Emit(OpCodes.Call, _newtonsoftSerialise);
			
		// - At this point, a HttpRequest and string are on the stack -
		
		// CancellationToken:
		var noneProp = typeof(CancellationToken).GetProperty(nameof(CancellationToken.None));
		var getNone = noneProp.GetGetMethod();
		il.Emit(OpCodes.Call, getNone);

		MethodInfo writeAsync = typeof(HttpResponseWritingExtensions)
		.GetMethod("WriteAsync", new[] { typeof(HttpResponse), typeof(string), typeof(System.Threading.CancellationToken) });

		// Write the string to the response body stream.
		il.Emit(OpCodes.Call, writeAsync);

		// Wrap as valuetask:
		var valueTaskCtor = typeof(ValueTask).GetConstructor(new[] { typeof(Task) });
		il.Emit(OpCodes.Newobj, valueTaskCtor);
	}

	private void EmitInvokeTerminalMethod(ILGenerator il, Type bodyType, MethodInfo method)
	{
		var parameters = method.GetParameters();

		if (!method.IsStatic)
		{
			if (_controllerInstanceFld == null)
			{
				_controllerInstanceFld = typeof(TerminalNode).GetField("ControllerInstance");
			}

			// get controller ref first, which is via the terminal node.
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, _controllerInstanceFld);
		}

		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];

			if (parameter.ParameterType == typeof(Context))
			{
				il.Emit(OpCodes.Ldarg_2);
			}
			else if (parameter.ParameterType == typeof(HttpContext))
			{
				il.Emit(OpCodes.Ldarg_1);
			}
			else if (parameter.ParameterType == typeof(HttpRequest))
			{
				var getRequest = typeof(HttpContext).GetProperty("Request").GetGetMethod();

				// The httpContext:
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Callvirt, getRequest);
			}
			else if (parameter.ParameterType == typeof(HttpResponse))
			{
				var getResponse = typeof(HttpContext).GetProperty("Response").GetGetMethod();

				// The httpContext:
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Callvirt, getResponse);
			}
			else
			{
				var fromRoute = parameter.GetCustomAttribute<FromRouteAttribute>();

				if (fromRoute == null)
				{
					// Is it from the query
					var fromQuery = parameter.GetCustomAttribute<FromQueryAttribute>();

					if (fromQuery != null)
					{
						var getRequest = typeof(HttpContext).GetProperty("Request").GetGetMethod();
						var getQuery = typeof(HttpRequest).GetProperty("Query").GetGetMethod();
						var tryGetValue = typeof(IQueryCollection).GetMethod("TryGetValue");

						// context.Request.Query.TryGetValue(name, out stringValues queryStr);

						// The httpContext:
						il.Emit(OpCodes.Ldarg_1);
						il.Emit(OpCodes.Callvirt, getRequest);
						il.Emit(OpCodes.Callvirt, getQuery);

						// The name:
						il.Emit(OpCodes.Ldstr, parameter.Name);

						// The out addr:
						var svTarget = il.DeclareLocal(typeof(Microsoft.Extensions.Primitives.StringValues));

						il.Emit(OpCodes.Ldloca, svTarget);
						il.Emit(OpCodes.Callvirt, tryGetValue);

						var isNullRoute = il.DefineLabel();
						var afterBoth = il.DefineLabel();

						// If it was false..
						il.Emit(OpCodes.Brfalse, isNullRoute);

						{
							// Succesfully got stringValue route.
							// Parse to target from the stringValue
							var getItemMethod = typeof(Microsoft.Extensions.Primitives.StringValues).GetMethod("get_Item");
							var countProp = typeof(Microsoft.Extensions.Primitives.StringValues).GetProperty("Count").GetGetMethod();

							// string str = sv[sv.Count - 1];
							il.Emit(OpCodes.Ldloca, svTarget);

							// sv.Count - 1
							il.Emit(OpCodes.Ldloca, svTarget);
							il.Emit(OpCodes.Callvirt, countProp);
							il.Emit(OpCodes.Ldc_I4_1);
							il.Emit(OpCodes.Sub);

							il.Emit(OpCodes.Callvirt, getItemMethod);

							// A string is now on the stack. 

							if (parameter.ParameterType == typeof(string))
							{
								// Nothing else to do here.
							}
							else if (parameter.ParameterType.IsValueType)
							{
								// Get it as a ReadOnlySpan<char> 
								// such that it is then compatible with our other conversion methods.
								var toSpan = typeof(System.MemoryExtensions)
									.GetMethod("AsSpan", new[] { typeof(string) });

								il.Emit(OpCodes.Call, toSpan);

								// A ReadOnlySpan is now on the stack.
								EmitParseValueFromSpan(parameter.ParameterType, il);
							}
							else
							{
								throw new Exception("Can't load a non-value type from the query string.");
							}

							il.Emit(OpCodes.Br, afterBoth);
						}

						il.MarkLabel(isNullRoute);

						{
							// Did not get a stringValue here.
							// Emit default for the parameter.
							EmitObjectAsDefault(parameter.ParameterType, il, parameter.DefaultValue);
						}

						il.MarkLabel(afterBoth);
					}
					else if (HttpVerb == "GET" || HttpVerb == "DELETE")
					{
						throw new Exception(
							"Unable to bind a body parameter '" + parameter.Name + "' for a get/delete endpoint as it has no body. " +
							"The parameter is on " + method.Name + " in " + method.DeclaringType.Name
						);
					}
					else if (parameter.ParameterType == bodyType)
					{
						il.Emit(OpCodes.Ldarg, 4);
					}
					else
					{
						throw new Exception(
							"Unable to bind parameter '" + parameter.Name + "' - you might be missing a [FromRoute] or [FromQuery]. " +
							"The parameter is on " + method.Name + " in " + method.DeclaringType.Name
						);
					}
				}
				else
				{
					// Load from the state struct.
					var stateType = GetTerminalStateType();

					_stateFields.TryGetValue("BindArg_" + i, out FieldBuilder fld);

					il.Emit(OpCodes.Ldarga, 3);
					il.Emit(OpCodes.Ldfld, fld);
				}
			}
		}

		// The 'this' reference (a class object, instance method) is now on the stack
		// Invoke the method
		il.Emit(OpCodes.Callvirt, method);

		// If the terminal method itself did not return a ValueTask, wrap it now.
		var retType = method.ReturnType;

		if (retType != typeof(void) && retType != typeof(ValueTask) && !(retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
		{
			il.Emit(OpCodes.Newobj, typeof(ValueTask<>).MakeGenericType(retType).GetConstructor(new[] { retType }));
		}

		// Either exits from a void func or returns whatever the method did.
		il.Emit(OpCodes.Ret);
	}

	private void EmitParseValueFromSpan(Type type, ILGenerator il)
	{
		if (type == typeof(bool))
		{
			// Must be the string "true".

			il.Emit(OpCodes.Ldstr, "true");
			var toSpan = typeof(System.MemoryExtensions)
				.GetMethod("AsSpan", new[] { typeof(string) });
			il.Emit(OpCodes.Call, toSpan);

			// SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
			var sequenceEq = typeof(MemoryExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.First(m => m.Name == "SequenceEqual"
						 && m.IsGenericMethodDefinition
						 && m.GetParameters().Length == 2
						 && m.GetParameters()[0].ParameterType.IsGenericType
						 && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>));

			il.Emit(OpCodes.Call, sequenceEq.MakeGenericMethod(new Type[] { typeof(char) }));

			// A bool is now on the stack
			return;
		}

		var nullableType = Nullable.GetUnderlyingType(type);

		var baseType = nullableType == null ? type : nullableType;

		// A ReadOnlySpan is on the stack.
		var tryParseMethod = baseType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, new Type[] {
			typeof(ReadOnlySpan<char>),
			baseType.MakeByRefType()
		});

		if (tryParseMethod == null)
		{
			throw new Exception("Can't parse a '" + type.Name + "'");
		}

		var loc = il.DeclareLocal(baseType);
		il.Emit(OpCodes.Ldloca, loc);
		il.Emit(OpCodes.Call, tryParseMethod);

		var parseFailed = il.DefineLabel();
		var parseEnd = il.DefineLabel();

		il.Emit(OpCodes.Brfalse, parseFailed);

		{
			// Parse succeeded.
			il.Emit(OpCodes.Ldloc, loc);

			if (nullableType != null)
			{
				// Create the nullable wrapper
				var nullableCtor = type
				.GetConstructor(new[] { nullableType });
				il.Emit(OpCodes.Newobj, nullableCtor);
			}

			il.Emit(OpCodes.Br, parseEnd);
		}

		il.MarkLabel(parseFailed);

		{
			// Parse failed.
			EmitDefault(type, il);
		}

		il.MarkLabel(parseEnd);
	}

	private void EmitObjectAsDefault(Type type, ILGenerator il, object defaultValue)
	{
		if (defaultValue == null || defaultValue.GetType() == typeof(System.DBNull))
		{
			// default(type)
			EmitDefault(type, il);
			return;
		}

		if (!type.IsValueType)
		{
			il.Emit(OpCodes.Ldnull);
			return;
		}

		if (type == typeof(bool))
		{
			if ((bool)defaultValue)
			{
				il.Emit(OpCodes.Ldc_I4_1);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			return;
		}

		if (type == typeof(byte))
		{
			il.Emit(OpCodes.Ldc_I4, (byte)defaultValue);
			return;
		}

		if (type == typeof(sbyte))
		{
			il.Emit(OpCodes.Ldc_I4, (sbyte)defaultValue);
			return;
		}

		if (type == typeof(ushort))
		{
			il.Emit(OpCodes.Ldc_I4, (ushort)defaultValue);
			return;
		}

		if (type == typeof(short))
		{
			il.Emit(OpCodes.Ldc_I4, (short)defaultValue);
			return;
		}

		if (type == typeof(uint))
		{
			il.Emit(OpCodes.Ldc_I4, (uint)defaultValue);
			return;
		}
		
		if (type == typeof(int))
		{
			il.Emit(OpCodes.Ldc_I4, (int)defaultValue);
			return;
		}

		if (type == typeof(float))
		{
			il.Emit(OpCodes.Ldc_R4, (float)defaultValue);
			return;
		}

		if (type == typeof(double))
		{
			il.Emit(OpCodes.Ldc_R8, (double)defaultValue);
			return;
		}

		if (type == typeof(ulong))
		{
			il.Emit(OpCodes.Ldc_I8, (ulong)defaultValue);
			return;
		}
		
		if (type == typeof(long))
		{
			il.Emit(OpCodes.Ldc_I8, (long)defaultValue);
			return;
		}

		// Other structs - call its default ctor:
		var addr = il.DeclareLocal(type);
		il.Emit(OpCodes.Ldloca, addr);
		il.Emit(OpCodes.Initobj, type);
		il.Emit(OpCodes.Ldloc, addr);
	}

	/// <summary>
	/// Emits a default value for the given target type.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="il"></param>
	public void EmitDefault(Type type, ILGenerator il)
	{
		if (!type.IsValueType)
		{
			il.Emit(OpCodes.Ldnull);
			return;
		}

		if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) ||
			type == typeof(ushort) || type == typeof(short) ||
			type == typeof(uint) || type == typeof(int))
		{
			il.Emit(OpCodes.Ldc_I4_0);
			return;
		}

		if (type == typeof(float))
		{
			il.Emit(OpCodes.Ldc_R4, 0f);
			return;
		}

		if (type == typeof(double))
		{
			il.Emit(OpCodes.Ldc_R8, 0d);
			return;
		}

		if (type == typeof(ulong) || type == typeof(long))
		{
			il.Emit(OpCodes.Ldc_I8, 0);
			return;
		}

		// Other structs - call its default ctor:
		var addr = il.DeclareLocal(type);
		il.Emit(OpCodes.Ldloca, addr);
		il.Emit(OpCodes.Initobj, type);
		il.Emit(OpCodes.Ldloc, addr);
	}

	private bool IsContentType(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Content<>))
		{
			return true;
		}

		var baseType = type.BaseType;

		if (baseType == null)
		{
			return false;
		}

		return IsContentType(baseType);
	}

	/// <summary>
	/// Builds the child set.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public IntermediateNode[] BuildChildren()
	{
		var imSet = new List<IntermediateNode>();

		// If there is more than one token node, it's invalid.
		IntermediateNode captureNode = null;

		for (var i=0;i<Children.Count;i++)
		{
			var child = Children[i];
			var built = child.Build();

			if (built == null)
			{
				continue;
			}

			var intNode = built as IntermediateNode;

			if (intNode == null)
			{
				// This implies something is wrong.
				Log.Warn("router", "A URL tree seemingly encountered a root not actually at the root of the tree. Something is likely incorrect!");
				continue;
			}

			if (intNode.Capture)
			{
				if (captureNode != null)
				{
					throw new Exception("Ambiguous routes detected: You have more than one URL route requesting different tokens at the same location, " + Text);
				}

				captureNode = intNode;
			}
			else
			{
				imSet.Add(intNode);
			}
		}

		// Sorted such that the token node is last and everything else is alphabetical.
		imSet.Sort((a, b) => string.Compare(a.ExactMatch, b.ExactMatch, StringComparison.Ordinal));

		if (captureNode != null)
		{
			imSet.Add(captureNode);
		}

		return imSet.ToArray();
	}

	/// <summary>
	/// Resolves the given singular node text relative to this one.
	/// This is only used during route construction: it is not the main route resolver.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="tokenContainer"></param>
	/// <returns></returns>
	public BuilderNode Resolve(string text, ref BuilderResolvedRoute tokenContainer)
	{
		BuilderNode tokenCapturer = null;

		foreach (var child in Children)
		{
			if (child.Text == text)
			{
				return child;
			}

			if (child.IsToken)
			{
				tokenCapturer = child;
			}
		}

		if (tokenCapturer != null)
		{
			// Capture the token now:
			if (tokenContainer.Tokens == null)
			{
				tokenContainer.Tokens = new List<string>();
			}

			tokenContainer.Tokens.Add(text);
			return tokenCapturer;
		}

		return null;
	}

	/// <summary>
	/// Adds or gets the given text as a builder node.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="canAdd"></param>
	/// <returns></returns>
	public BuilderNode AddOrGet(string text, bool canAdd = true)
	{
		foreach (var child in Children)
		{
			if (child.Text == text)
			{
				return child;
			}
		}

		if (!canAdd)
		{
			throw new Exception("URL route does not exist: " + text);
		}

		// Not found - add child:
		var newChild = new BuilderNode() {
			Text = text,
			FullRoute = FullRoute + "/" + text,
			Parent = this,
			HttpVerb = HttpVerb
		};

		if (newChild.IsToken)
		{
			if (ChildTokenName != null)
			{
				// Ambiguous token situation.
				// All routes need to use the same {name} at the same point in the route.
				Console.WriteLine("Colliding token was: " + ChildTokenName);
				return null;
			}
			ChildTokenName = newChild.Text;
		}

		Children.Add(newChild);
		return newChild;
	}

	/// <summary>
	/// Adds a rewrite. The target node must exist.
	/// </summary>
	/// <param name="route"></param>
	/// <param name="to"></param>
	public void AddRewrite(string route, string to)
	{
		var current = GetNode(route, false);

		// - Resolve which node will handle 'to' fully
		// - This will frequently result in some token values which will then, during the actual rewrite itself 
		//   replace the current token state entirely.

		// Builder resolver is permitted to allocate
		var target = Resolve(to);

		if (target.Node == null)
		{
			throw new Exception("A rewrite was added to a node that does not exist. Note that rewrite nodes can only safely target true nodes (not other rewrites).");
		}

		current.SetTerminal(new TerminalRewrite(target));
	}

	/// <summary>
	/// Adds a 302 redirect.
	/// </summary>
	/// <param name="route"></param>
	/// <param name="to"></param>
	public void AddRedirect(string route, string to)
	{
		var current = GetNode(route, false);
		current.SetTerminal(new TerminalRedirect(to));
	}

	/// <summary>
	/// Adds custom behaviour for a route.
	/// </summary>
	/// <param name="route"></param>
	/// <param name="behaviour"></param>
	public void AddCustomBehaviour(string route, TerminalBehaviour behaviour)
	{
		var current = GetNode(route, false);
		current.SetTerminal(behaviour);
	}

	/// <summary>
	/// Adds a complete route.
	/// </summary>
	/// <param name="route"></param>
	/// <param name="controllerMethod"></param>
	/// <param name="controllerInstance"></param>
	public void AddRoute(string route, MethodInfo controllerMethod, object controllerInstance)
	{
		var current = GetNode(route, false);
		current.ControllerInstance = controllerInstance;
		current.SetTerminal(new TerminalMethod(controllerMethod));
	}

	private BuilderResolvedRoute Resolve(string route)
	{
		if (route.StartsWith("/"))
		{
			route = route.Substring(1);
		}

		if (route.EndsWith("/"))
		{
			route = route.Substring(0, route.Length - 1);
		}

		var parts = route.Split('/');

		var current = this;

		BuilderResolvedRoute result = new BuilderResolvedRoute();

		for (var i = 0; i < parts.Length; i++)
		{
			// NB: the homepage, at "", would still be added as a child node.
			// parts is always length 1 or more.
			current = current.Resolve(parts[i], ref result);

			if (current == null)
			{
				throw new Exception("Ambiguous token identified: you have more than one route using tokens with different names at the same location. " +
					"Please make sure they all use the same {name}. A route that was being added was " + route);
			}
		}

		result.Node = current;
		return result;
	}

	private BuilderNode GetNode(string route, bool mustExist)
	{
		if (route.StartsWith("/"))
		{
			route = route.Substring(1);
		}

		if (route.EndsWith("/"))
		{
			route = route.Substring(0, route.Length - 1);
		}

		var parts = route.Split('/');

		var current = this;

		for (var i = 0; i < parts.Length; i++)
		{
			// NB: the homepage, at "", would still be added as a child node.
			// parts is always length 1 or more.
			current = current.AddOrGet(parts[i], !mustExist);

			if (current == null)
			{
				throw new Exception("Ambiguous token identified: you have more than one route using tokens with different names at the same location. " +
					"Please make sure they all use the same {name}. A route that was being added was " + route);
			}
		}

		return current;
	}

}

/// <summary>
/// An empty struct representing empty terminal state.
/// </summary>
public struct EmptyTerminalState
{
}