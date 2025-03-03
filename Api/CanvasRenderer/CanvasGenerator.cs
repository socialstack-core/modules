using Api.Contexts;
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
	/// Get or create a datamap entry for a field in a graph node.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="outputField"></param>
	/// <returns></returns>
	public CanvasGeneratorMapEntry GetDataMapEntry(Executor node, string outputField)
	{
		for (var i = 0; i < DataMap.Count; i++)
		{
			var existing = DataMap[i];
			if (existing.GraphNode == node && existing.Field == outputField)
			{
				return existing;
			}
		}

		var cgm = new CanvasGeneratorMapEntry();
		cgm.GraphNode = node;
		cgm.Field = outputField;
		DataMap.Add(cgm);
		cgm.Id = (uint)DataMap.Count; // ID must be non-zero so we use index+1.
		node.AddDataMapOutput(cgm);
		return cgm;
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
						await CreatePlanInternal();
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

	private object genLocker = new object();
	private Task _planCreate;

	private async Task CreatePlanInternal()
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
			_rootCanvasNode = LoadCanvasNode(json);

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

	private CanvasNode LoadCanvasNode(JToken node)
	{
		if (node == null)
		{
			return null;
		}

		var result = new CanvasNode();

		if (node.Type == JTokenType.String)
		{
			result.StringContent = node.Value<string>();
			return result;
		}

		if (node.Type == JTokenType.Array)
		{
			throw new NotSupportedException("Canvas with arrays are now only supported if the array is set as a content (c) value.");
		}

		// Here we only care about:
		// - t(ype)
		// - d(ata)
		// - s(trings)
		// - g(raphs)
		// - r(oots)
		// - c(ontent)

		// Type
		var type = node["t"];

		if (type != null)
		{
			result.Module = type.Value<string>();
		}

		// Data
		var data = node["d"] as JObject;

		if (data != null)
		{
			foreach (var kvp in data)
			{
				if (result.Data == null)
				{
					result.Data = new Dictionary<string, string>();
				}

				string val;

				if (kvp.Value.Type == JTokenType.Null)
				{
					val = null;
				}
				else if (kvp.Value.Type == JTokenType.Boolean)
				{
					val = kvp.Value.Value<bool>() ? "true" : "false";
				}
				else if (kvp.Value.Type == JTokenType.String)
				{
					val = kvp.Value.Value<string>();

					// Awkwardly convert back to a json token which is not easily available via JToken (fortunately this only happens once!):
					if (val != null)
					{
						val = Newtonsoft.Json.JsonConvert.SerializeObject(val);
					}
				}
				else
				{
					val = kvp.Value.ToString();
				}

				result.Data[kvp.Key] = val;
			}
		}

		// Strings
		var str = node["s"];

		if (str != null)
		{
			result.StringContent = str.Value<string>();
		}

		// Graphs
		var graphData = node["g"];

		if (graphData != null)
		{
			// Found a graph. Load it using the canvas-wide graph node loader.
			var graph = new Graph(graphData, _graphNodeLoader);

			// If the root node is a component then this canvas node morphs in to that component.
			var comp = graph.Root as Component;
			if (comp != null)
			{
				// Inline it now.
				foreach (var kvp in comp.ConstantData)
				{
					if (kvp.Key == "componentType")
					{
						result.Module = kvp.Value.ToString();
					}
					else
					{
						if (result.Data == null)
						{
							result.Data = new Dictionary<string, string>();
						}

						string val;

						if (kvp.Value.Type == JTokenType.Null)
						{
							val = null;
						}
						else if (kvp.Value.Type == JTokenType.Boolean)
						{
							val = kvp.Value.Value<bool>() ? "true" : "false";
						}
						else if (kvp.Value.Type == JTokenType.String)
						{
							val = kvp.Value.Value<string>();

							// Awkwardly convert back to a json token which is not easily available via JToken (fortunately this only happens once!):
							if (val != null)
							{
								val = Newtonsoft.Json.JsonConvert.SerializeObject(val);
							}
						}
						else
						{
							val = kvp.Value.ToString();
						}

						result.Data[kvp.Key] = val;
					}
				}

				// Each link (non-constant data) becomes a datamap pointer.
				foreach (var kvp in comp.Links)
				{
					if (result.Pointers == null)
					{
						result.Pointers = new Dictionary<string, uint>();
					}

					var cdm = GetDataMapEntry(kvp.Value.SourceNode.AddedAs, kvp.Value.Field);
					result.Pointers[kvp.Key] = cdm.Id;
				}
			}
			else
			{
				result.Graph = graph;
			}
		}

		// Roots
		var roots = node["r"] as JObject;

		if (roots != null)
		{
			foreach (var kvp in roots)
			{
				if (result.Roots == null)
				{
					result.Roots = new Dictionary<string, CanvasNode>();
				}

				result.Roots[kvp.Key] = LoadCanvasNode(kvp.Value);
			}
		}

		// Content
		var content = node["c"];

		if (content != null)
		{
			// Content can be: an array an object or a string.
			var array = content as JArray;

			if (array != null)
			{
				for (var i = 0; i < array.Count; i++)
				{
					if (result.Content == null)
					{
						result.Content = new List<CanvasNode>();
					}

					var child = LoadCanvasNode(array[i]);
					result.Content.Add(child);
				}
			}
			else
			{
				// Either a string or object.
				var child = LoadCanvasNode(content);

				if (result.Content == null)
				{
					result.Content = new List<CanvasNode>();
				}

				result.Content.Add(child);
			}
		}

		return result;
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
	public async ValueTask Generate(Context context, Writer writer, object po)
	{
		if(_plan == null)
		{
			await CreateExecutionPlan();
		}

		// Get canvas state object:
		var state = GetState();
		state.PrimaryObject = po;
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