using Api.Contexts;
using Api.Eventing;
using Api.Pages;
using Api.SocketServerLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Api.CanvasRenderer;


/// <summary>
/// Converts one canvas in to another performing just-in-time substitutions which are context aware.
/// These substitutions include handling any templates plus also execution of content nodes in graphs.
/// </summary>
public class CanvasGenerator
{
	/// <summary>
	/// The source canvas.
	/// </summary>
	private string _canvas;
	
	/// <summary>
	/// Execution plan - a list of generation nodes which are order optimised and can sometimes be bundled together.
	/// </summary>
	private CanvasGeneratorNode[] _plan;

	/// <summary>
	/// Canvas wide graph node loader.
	/// </summary>
	private NodeLoader _graphNodeLoader;

	private CanvasNode _rootCanvasNode;

	/// <summary>
	/// The GraphContext state type to instance when generating canvases with this generator.
	/// </summary>
	private Type _stateType;

	/// <summary>
	/// Assigned datamap entries.
	/// </summary>
	public List<CanvasGeneratorMapEntry> DataMap = new List<CanvasGeneratorMapEntry>();

	/// <summary>
	/// Instance (or get from a pool) a graphContext to use when executing this generator.
	/// </summary>
	/// <returns></returns>
	public GraphContext GetState()
	{
		if (_stateType == null)
		{
			return new GraphContext();
		}

		var ctx = (GraphContext)Activator.CreateInstance(_stateType);

		return ctx;
	}

	/// <summary>
	/// Creates a generator with the given input canvas and primary content type.
	/// </summary>
	/// <param name="canvas"></param>
	/// <param name="primaryContentType"></param>
	public CanvasGenerator(string canvas, Type primaryContentType)
	{
		_canvas = canvas;
		_graphNodeLoader = new NodeLoader(primaryContentType);
	}

	/// <summary>
	/// True if the execution plan has a constant output.
	/// </summary>
	/// <returns></returns>
	public async ValueTask<bool> IsConstant()
	{
		if (_plan == null)
		{
			await CreateExecutionPlan();
		}

		for (var i = 0; i < _plan.Length; i++)
		{
			if (!(_plan[i] is CanvasGeneratorBytes))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Establishes an "execution plan" of sorts - this is where it figures out e.g. which content it can load in parallel, plus their dependent content bundles.
	/// If multiple nodes in a graph are for the same thing then they will be loaded once by the plan.
	/// </summary>
	private async ValueTask CreateExecutionPlan()
	{
		// This occurs off the hot path - i.e. a plan is created once and then executed repeatedly
		// so it's good to spend more time here to make it as effective as possible.

		Task pc = _planCreate;

		if (pc == null)
		{
			lock (genLocker)
			{
				if (_planCreate == null)
				{
					// Task.Run used here to get _planCreate set to an awaitable as quickly as possible
					// without blocking up this thread specifically.
					_planCreate = Task.Run(async () => {
						await CreatePlanInternal(new Context(1, 0, 1));
					});
				}
			}

			await _planCreate;
		}
		else
		{
			await pc;
		}
	}

	/// <summary>
	/// Loads a canvas node from the given newtonsoft token.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="node"></param>
	/// <returns></returns>
	public async ValueTask<CanvasNode> LoadCanvasNode(Context context, JToken node)
	{
		return await CanvasNode.LoadCanvasNode(context, node, new CanvasDetails() {
			DataMap = DataMap,
			GraphNodeLoader = _graphNodeLoader
		});
	}

	private object genLocker = new object();
	private Task _planCreate;

	private async Task CreatePlanInternal(Context context)
	{
		// In a nutshell then, the technique will be:
		// - Discover all graphs
		// - Combine identical nodes
		// - Group nodes in to tranches so they can load in parallel whenever possible
		// - Each tranche of nodes to execute is its own CanvasGeneratorNode.

		if (string.IsNullOrEmpty(_canvas))
		{
			// Empty plan
			_plan = new CanvasGeneratorNode[0];
			return;
		}

		var wipPlan = new List<CanvasGeneratorNode>();
		
		try
		{
			// Load the JSON.
			var json = Newtonsoft.Json.JsonConvert.DeserializeObject(_canvas) as JToken;

			// Load the canvas nodes and simultaneously locate and consolidate all graph nodes in this json.
			// This process combines identical nodes from anywhere in the canvas.
			_rootCanvasNode = await LoadCanvasNode(context, json);

			// Next, organise the graph nodes in to tranches:
			var tranches = _graphNodeLoader.CreateTranches();

			if (tranches == null || tranches.Length == 0)
			{
				// The whole canvas but reconstructed to ensure it is optimal with things like IDs stripped out:
				var canvasBytes = _rootCanvasNode.ToJsonBytes(false);

				// Add the canvas:
				wipPlan.Add(new CanvasGeneratorBytes(canvasBytes));
			}
			else
			{
				// The vast majority of the canvas:
				var writer = Writer.GetPooled();
				writer.Start(null);
				_rootCanvasNode.ToJson(writer, true);

				// Add the datamap opening:
				writer.WriteASCII(",\"m\":[");

				var canvasBytes = writer.AllocatedResult();
				writer.Release();

				// Add the bulk of the canvas:
				wipPlan.Add(new CanvasGeneratorBytes(canvasBytes));

				// Next, compile the tranches and add them to the plan.
				await _graphNodeLoader.CompileTranches(tranches);
				_stateType = _graphNodeLoader.BakeCompiledTypes();

				for (var i = 0; i < tranches.Length; i++)
				{
					wipPlan.Add(tranches[i].BakeCompiledType());
				}

				wipPlan.Add(new CanvasGeneratorBytes(Encoding.ASCII.GetBytes("]}"))); // The closure after the datamap and the root node itself.

			}

			_plan = wipPlan.ToArray();

		}
		catch (Exception e)
		{
			// Unable to create execution plan.
			// In this scenario, the output is the same as the input.
			Log.Error("canvasgen", e, "Unable to create execution plan");
			Fallback();
		}
	}

	private void Fallback()
	{
		var bytes = Encoding.UTF8.GetBytes(_canvas);
		_plan = new CanvasGeneratorNode[] {
			new CanvasGeneratorBytes(bytes)
		};
	}

	/// <summary>
	/// Generate the target canvas. Puts the result in to the given writer.
	/// </summary>
	public async ValueTask Generate(Context context, Writer writer, PageWithTokens pageWithTokens)
	{
		if(_plan == null)
		{
			await CreateExecutionPlan();
		}

		// Get canvas state object:
		var state = GetState();
		state.PageWithTokens = pageWithTokens;
		state.Context = context;
		state.Writer = writer;

		// The plan is a list of steps which occur. A step may load some content and then emit it in to the writer.
		// Content loaders will frequently group together and then later emit a content reference which canvasExpand then handles.
		// note that if some content loading can occur in parallel, the plan will bundle them in to a 
		// singular node, thus we only need to step through the plan linearly here.
		for(var i=0;i<_plan.Length;i++)
		{
			await _plan[i].Generate(state);
		}

		// Clear any writers in the state:
		state.ReleaseBuffers();
	}
	
}