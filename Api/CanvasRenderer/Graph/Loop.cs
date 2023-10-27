using Api.Contexts;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    public class Loop : Executor
    {
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
