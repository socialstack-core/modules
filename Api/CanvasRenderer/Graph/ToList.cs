using Api.Contexts;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A graph node which converts inputs to a singular list.
    /// </summary>
    public class ToList : Executor
    {
        /// <summary>
        /// Create a new ToList graph node using info in the given JSON token.
        /// </summary>
        /// <param name="d"></param>
        public ToList(JToken d) : base(d)
        {
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="compileEngine"></param>
		public override ValueTask Compile(NodeLoader compileEngine)
		{
			throw new System.NotImplementedException();
		}
        
        /*
		public override async Task<dynamic> Go(Context context, PageState pageState)
        {
            dynamic[] output = new dynamic[Links.Count];
            int i=0;

            foreach (var key in Links.Keys)
            {
                var node = Links[key];
                dynamic nodeResult = null;

                if (node != null)
                {
                    nodeResult = await Links[key].SourceNode.Run(context, pageState);
                }

                output[i] = nodeResult;
                i++;
            }

            return output;
        }
        */
    }
}
