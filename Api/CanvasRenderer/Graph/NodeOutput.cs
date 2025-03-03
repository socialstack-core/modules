using Api.Contexts;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A link between nodes on a graph.
    /// </summary>
    public class NodeLink
    {
        /// <summary>
        /// The node being linked to
        /// </summary>
        public Executor SourceNode;
        /// <summary>
        /// The outputted field from the linked node to read
        /// </summary>
        public string Field;

        /// <summary>
        /// Creates a new node link
        /// </summary>
        /// <param name="srcNode"></param>
        /// <param name="field"></param>
        public NodeLink(Executor srcNode, string field)
        {
			SourceNode = srcNode;
			Field = field;
        }

        /*
        public override async Task<dynamic> Go(Context context, PageState pageState)
        {
            await SourceNode.Run(context, pageState);

            var outputs = SourceNode.Outputs;

            if (SourceNode.Outputs == null || string.IsNullOrEmpty(Field))
            {
                return null;
            }

            if (Field == "output" || outputs is JValue)
            {
                return outputs;
            }

            return outputs[Field];
        }
        */
    }

    /// <summary>
    /// Node links using the same output field from the same object.
    /// </summary>
    public class NodeLinkSet
    {
        /// <summary>
        /// A common output field.
        /// </summary>
        public string Field;

        /// <summary>
        /// The links.
        /// </summary>
        public List<NodeLink> Links = new List<NodeLink>();
    }
}
