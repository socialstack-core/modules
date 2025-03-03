using Api.Contexts;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// A node which loops over an input set.
	/// </summary>
    public class Loop : Executor
    {
		/// <summary>
		/// Create a new loop node for the given JSON token.
		/// </summary>
		/// <param name="d"></param>
        public Loop(JToken d) : base(d)
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
