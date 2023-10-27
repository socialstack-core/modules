using Api.Contexts;
using Api.SocketServerLibrary;
using Newtonsoft.Json.Linq;
using System;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A graph found in the page json
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// All nodes in the graph.
        /// </summary>
        public Executor[] Nodes;
        /// <summary>
        /// Root of the graph
        /// </summary>
        public Executor Root;

		/// <summary>
		/// Create graph from json node
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="nodeLoader"></param>
		public Graph(JToken graph, NodeLoader nodeLoader)
        {
            if (graph == null)
            {
                return;
            }

            JArray jNodes = null;

            if (graph is JArray)
            {
                jNodes = graph as JArray;
            } else if (graph["c"] is JArray)
            {
                jNodes = graph["c"] as JArray;
            }

            if (jNodes != null && jNodes.Count > 0)
            {
                var i=0;
                Nodes = new Executor[jNodes.Count];

                foreach (JObject node in jNodes)
                {
                    Nodes[i] = InstantiateNode(node);
                    i++;
                }

                for (int j=0;j< Nodes.Length;j++)
                {
                    var node = Nodes[j];
                    var nodeJson = jNodes[j];

                    if (nodeJson["r"] != null)
                    {
                        Root = node;
                    }

                    var links = nodeJson["l"] as JObject;

                    if (links != null)
                    {
                        foreach (var kvp in links)
                        {
                            string name = kvp.Key;
                            var link = kvp.Value.ToObject<Link>();

                            if (link.n > 0 && link.n < Nodes.Length)
                            {
                                var srcNode = Nodes[link.n];

                                // If srcNode is a Constant, add to ConstantData.
                                var cnst = srcNode as Constant;

                                if (cnst != null)
                                {
                                    node.ConstantData[name] = cnst.Value;
                                }
                                else
                                {
                                    node.Links[name] = new NodeLink(srcNode, link.f);
                                }
                            }
                        }
                    }
                }

                // Next, dedupe nodes with the node loader.
                // Starting from the root node of the graph, we collect all reachable nodes.
                // Root itself is *not* added to the loader if it is a component node.
                // This is because it will ultimately replace the original canvas node.
                if (Root.GetType() == typeof(Component))
                {
                    Root.AddLinksToLoader(nodeLoader);
                }
                else
                {
                    Root.AddToLoader(nodeLoader);
                }
            }
        }

        /// <summary>
        /// Converts graph data to JSON.
        /// </summary>
        /// <param name="writer"></param>
        public void ToJson(Writer writer)
        {
            throw new NotImplementedException("todo!");
        }

		private Executor InstantiateNode(JToken node)
        {
            Executor newNode = null;
            var type = node.Value<string>("t");

            if (type != null)
            {
                switch (type) 
                {
                    case "Component":
                        newNode = new Component(node);
                        break;
                    case "Constant":
                        newNode = new Constant(node);
                        break;
                    case "Content":
                        newNode = new Content(node);
                        break;
                    case "ContentList":
                        newNode = new ContentList(node);
                        break;
                    case "Count":
                        newNode = new Count(node);
                        break;
                    case "Fields":
                        newNode = new Fields(node);
                        break;
                    case "FromList":
                        newNode = new FromList(node);
                        break;
                    case "If":
                        newNode = new If(node);
                        break;
                    case "Loop":
                        newNode = new Loop(node);
                        break;
                    case "Tokens":
                        newNode = new Tokens(node);
                        break;
                    case "ToList":
                        newNode = new ToList(node);
                        break;
                    default:
                        break;
                }
            }

            return newNode;
        }
    }
}
