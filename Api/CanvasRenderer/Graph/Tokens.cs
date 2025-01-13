using Api.Contexts;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// A token resolver in a graph.
	/// </summary>
    public class Tokens : Executor
    {
		/// <summary>
		/// Create a new token resolver from the info in the given JSON object.
		/// </summary>
		/// <param name="d"></param>
        public Tokens(JToken d) : base(d)
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
        public override Task<dynamic> Go(Context context, PageState pageState)
        {
            throw new System.NotImplementedException();
        }
        */
	}
}
