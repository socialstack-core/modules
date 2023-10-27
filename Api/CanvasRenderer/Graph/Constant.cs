using Api.Contexts;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    public class Constant : Executor
    {
        public JToken Value;


        public Constant(JToken d) : base(d)
        {
			Value = ConstantData["output"];
		}

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
