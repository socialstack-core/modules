using Api.Contexts;
using Api.SocketServerLibrary;
using Api.Startup;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A node of a graph
    /// </summary>
    public abstract class Executor
    {
        /// <summary>
        /// The original graph node.
        /// </summary>
        public JToken GraphNode { get; set;}
        /// <summary>
        /// Inbound links - other nodes that this node is pulling values from.
        /// </summary>
        public Dictionary<string, NodeLink> Links = new Dictionary<string, NodeLink>();
        /// <summary>
        /// Raw constant data.
        /// </summary>
        public Dictionary<string, JToken> ConstantData = new Dictionary<string, JToken>();
        /// <summary>
        /// 
        /// </summary>
        public List<CanvasGeneratorMapEntry> DataMapOutputs;
        /// <summary>
        /// Outbound links - that's other nodes using this executor's output. Excludes any used by DataMapOutputs.
        /// </summary>
        public List<NodeLinkSet> ReverseLinks;

        /// <summary>
        /// The output set.
        /// </summary>
		public JToken Outputs { get; set; }

        /// <summary>
        /// Creates a new graph node.
        /// </summary>
        /// <param name="graphNode"></param>
        public Executor(JToken graphNode)
        {
            LoadData(graphNode);
            Outputs = new JObject();
        }

        /// <summary>
        /// Gets a constant value.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool GetConstString(string fieldName, out string val)
        {
            if (ConstantData.TryGetValue(fieldName, out JToken v))
            {
                // If it's an array (includes), concat into one string.
                var arr = v as JArray;
                if (arr != null)
                {
                    var str = "";
                    for (var i = 0; i < arr.Count; i++)
                    {
                        if (i != 0)
                        {
                            str += ",";
                        }
                        str += arr[i].ToString();
                    }

                    val = str;
                }
                else
                {
                    val = v.ToString();
                }
                return true;
            }

            val = null;
            return false;
        }
        
        /// <summary>
        /// Gets a constant long.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool GetConstNumber(string fieldName, out long val)
        {
            if (ConstantData.TryGetValue(fieldName, out JToken v))
            {
                val = v.Value<long>();
                return true;
            }

            val = 0;
            return false;
        }

		/// <summary>
		/// Adds a datamap output that this node must populate when it executes.
		/// </summary>
		/// <param name="cgm"></param>
		public void AddDataMapOutput(CanvasGeneratorMapEntry cgm)
        {
            if (DataMapOutputs == null)
            {
                DataMapOutputs = new List<CanvasGeneratorMapEntry>();
			}

            DataMapOutputs.Add(cgm);
        }

		/// <summary>
		/// Gets input type of named input field.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public Type GetLinkedInputType(string field)
		{
			if (!Links.TryGetValue(field, out NodeLink link))
			{
				return null;
			}

			// If the link is the value "null", also return null.
			if (link.SourceNode == null)
			{
				return null;
			}

			// Get output type of named field:
			return link.SourceNode.GetOutputType(field);
		}

		/// <summary>
		/// Emits an output read for the given output field in to the given compile engine.
		/// Nodes can use the reverse map to only output fields that they know will be used or alternatively output one thing and use field readers.
		/// </summary>
		/// <param name="compileEngine"></param>
		/// <param name="field"></param>
		public virtual Type EmitOutputRead(NodeLoader compileEngine, string field)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns type of a named output field.
        /// </summary>
        /// <param name="field"></param>
        public virtual Type GetOutputType(string field)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Emits an output read for the given output field in to the given compile engine.
        /// Nodes can use the reverse map to only output fields that they know will be used or alternatively output one thing and use field readers.
        /// </summary>
        /// <param name="compileEngine"></param>
        /// <param name="field"></param>
        public virtual void EmitOutputJson(NodeLoader compileEngine, string field)
        {
            throw new NotImplementedException("Can't output this node as dataloader content.");
        }

        /// <summary>
        /// Compile this node. It must read inputs from and write outputs to the graph state (arg 1).
        /// </summary>
        /// <param name="compileEngine"></param>
        public virtual ValueTask Compile(NodeLoader compileEngine)
        {
            throw new NotImplementedException("Node "+GetType()+" not supported by the serverside graph executor");
        }
        
        /// <summary>
        /// Compile this nodes output. This can only read from the graph state (arg 1) and then write in to the provided writer (arg 1.writer).
        /// </summary>
        /// <param name="compileEngine"></param>
        public void CompileOutput(NodeLoader compileEngine)
        {
            if (DataMapOutputs == null)
            {
                // No DM outputs from this node.
                return;
            }

			for (var i = 0; i < DataMapOutputs.Count; i++)
            {
                var dmo = DataMapOutputs[i];

				if (compileEngine.DmIsFirst)
                {
                    compileEngine.DmIsFirst = false;

					// Do not emit a comma
					compileEngine.EmitWriteASCII("{\"id\":" + dmo.Id + ",\"c\":");
				}
                else
                {
					// Emit the code to write a comma.
					compileEngine.EmitWriteASCII(",{\"id\":" + dmo.Id + ",\"c\":");
				}

				// Emit datamap entry. MUST output something - "null" is also required!
				EmitOutputJson(compileEngine, dmo.Field);

                // Emit the closing bracket
                compileEngine.EmitWriteByte((byte)'}');
			}
		}

		private Executor _addedToLoader;

        /// <summary>
        /// True if this node is the same as the given one.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool IsSame(Executor node)
        {
            if (node.GetType() != GetType())
            {
                return false;
            }

            // Same type - same constant data?
            if (node.ConstantData.Count != ConstantData.Count)
            {
                return false;
            }

            foreach (var kvp in node.ConstantData)
            {
                if (!ConstantData.TryGetValue(kvp.Key, out JToken cns))
                {
                    return false;
                }

                if (cns.ToString() != kvp.Value.ToString())
                {
                    return false;
                }
            }

            // Same type and constant data matches. Do any upstream links match as well?
            if (node.Links.Count != Links.Count)
            {
                return false;
            }

			foreach (var kvp in node.Links)
			{
				if (!Links.TryGetValue(kvp.Key, out NodeLink link))
				{
					return false;
				}

				if (link.Field != kvp.Value.Field)
				{
					return false;
				}

				if (link.SourceNode.AddedAs != kvp.Value.SourceNode.AddedAs)
				{
					return false;
				}

			}

            return true;
		}

        /// <summary>
        /// 
        /// </summary>
        public Executor AddedAs => _addedToLoader;

        /// <summary>
        /// The node order. Indicates how "deep" a node is in the load tree. Set when adding the node to the loader.
        /// </summary>
        public int Order;

        /// <summary>
        /// Add this node to the given loader.
        /// </summary>
        /// <param name="loader"></param>
        /// <returns></returns>
        public Executor AddToLoader(NodeLoader loader)
        {
            if (_addedToLoader != null)
            {
                // No-op
                return _addedToLoader;
            }
            AddLinksToLoader(loader);

            // Adding reverse links only happens here. It must not happen for links to the root node if it is a component.
            foreach (var kvp in Links)
            {
                var src = kvp.Value.SourceNode;

                if (src.ReverseLinks == null)
                {
                    src.ReverseLinks = new List<NodeLinkSet>();
                }

                var reverseLinks = src.ReverseLinks;
                NodeLinkSet linkSet = null;

				// Will often be just a few output nodes (usually actually just one - a field called "output").
                // So a dictionary of these is actually worse for performance in the general sense.
				for (var i = 0; i < reverseLinks.Count; i++)
                {
                    if (reverseLinks[i].Field == kvp.Value.Field)
                    {
                        linkSet = reverseLinks[i];
                        break;
					}
                }

                if (linkSet == null)
                {
                    linkSet = new NodeLinkSet() { Field = kvp.Value.Field };
                    reverseLinks.Add(linkSet);
				}

                linkSet.Links.Add(kvp.Value);
            }

			return _addedToLoader = loader.Add(this);
		}

        /// <summary>
        /// Add linked nodes to the given loader.
        /// </summary>
        /// <param name="loader"></param>
        public void AddLinksToLoader(NodeLoader loader)
        {
            // Add each of our upstream nodes.
            var maxOrder = 0;

            foreach (var kvp in Links)
            {
                kvp.Value.SourceNode = kvp.Value.SourceNode.AddToLoader(loader);
                var linkedOrder = kvp.Value.SourceNode.Order;

                if (linkedOrder > maxOrder)
                {
                    maxOrder = linkedOrder;
                }
            }

            Order = maxOrder + 1;
        }

        /// <summary>
        /// Loads the data from the given graph node token.
        /// </summary>
        /// <param name="graphNode"></param>
		public virtual void LoadData(JToken graphNode)
        {
            if (graphNode == null)
            {
                return;
            }

            GraphNode = graphNode;
            var nodeData = graphNode["d"] as JObject;

            if (nodeData != null)
            {
                foreach (var kvp in nodeData)
                {
                    string name = kvp.Key;
                    ConstantData[name] = kvp.Value;
                }
            }
        }

        /*
        public async Task<dynamic> ReadValue(Context context, PageState pageState, string nodeKey)
        {
            if (Links == null || !Links.ContainsKey(nodeKey))
            {
                return null;
            }

            var sf = Links[nodeKey];

            if (sf == null)
            {
                return null;
            }

            var result = await sf.SourceNode.Run(context, pageState);

            return result;
        }

        public abstract Task<dynamic> Go(Context context, PageState pageState);

        public virtual async Task<dynamic> Run(Context context, PageState pageState)
        {
            var result = await Go(context, pageState);
            
            JToken jResult = null;

            if (result != null && !(result is JToken))
            {
                jResult = JToken.FromObject(result);
            }else if (result != null)
            {
                jResult = result;
            }
                
            Outputs = jResult;

            // Should we return result or always stick in Json format and return jResult?
            return result;
        }
        */
    }
}
