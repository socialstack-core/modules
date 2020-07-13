using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Api.CanvasRenderer
{
	/// <summary>
	/// Used when rendering 1 canvas multiple times.
	/// </summary>
	public class CanvasAndContextSet
    {
		/// <summary>
		/// The canvas to render.
		/// </summary>
		public string BodyJson;
		
		/// <summary>
		/// The raw context set.
		/// </summary>
		public List<Dictionary<string, object>> Contexts = new List<Dictionary<string, object>>();
    }
}
