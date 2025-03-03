using Api.Contexts;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A constant value.
    /// </summary>
    public class Constant : Executor
    {
        /// <summary>
        /// The underlying constant.
        /// </summary>
        public JToken Value;

        /// <summary>
        /// Create a new constant from the given JSON token.
        /// </summary>
        /// <param name="d"></param>
        public Constant(JToken d) : base(d)
        {
			Value = ConstantData["output"];
		}

        /// <summary>
        /// Compiles this node with the given engine.
        /// </summary>
        /// <param name="compileEngine"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public override ValueTask Compile(NodeLoader compileEngine)
        {
            throw new InvalidOperationException("Constant nodes must be eliminated - they are not capable of being compiled.");
        }

        /*
        public async override Task<dynamic> Go(Context context, PageState pageState)
        {
            if (GraphNode == null)
            {
                return null;
            }

            // Is this conversion necessary?
            return GraphNode.ToObject<dynamic>();
        }
        */
    }
}
