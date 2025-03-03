using Api.Contexts;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// A React component.
	/// </summary>
    public class Component : Executor
    {
		/// <summary>
		/// Create a react component entry from the given token.
		/// </summary>
		/// <param name="d"></param>
        public Component(JToken d) : base(d)
        {
        }


		/// <summary>
		/// Compile this node. It must read inputs from and write outputs to the graph state.
		/// </summary>
		/// <param name="compileEngine"></param>
		public override ValueTask Compile(NodeLoader compileEngine)
		{
			// In the future, these output a CanvasNode for all intermediate situ's otherwise.
			throw new NotImplementedException("Component nodes are only permitted as root nodes currently");
		}

		/*
		public override async Task<dynamic> Go(Context context, PageState pageState)
		{
			JObject component = new JObject();

			component["d"] = new JObject();

			foreach (var nodeKey in Links.Keys)
			{
				var value = await Links[nodeKey].SourceNode.Run(context, pageState);
				JToken jValue = null;

				if (value != null && !(value is JToken))
				{
					jValue = JToken.FromObject(value);
				} else if (value is JToken)
				{
					jValue = value;
				}

				if (nodeKey == "componentType")
				{
					component["t"] = jValue;
				} else
				{
					component["d"][nodeKey] = jValue;
				}
			}

			return component;
		}
		*/
		}
}
