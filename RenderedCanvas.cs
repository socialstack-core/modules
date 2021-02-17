using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A rendered canvas.
    /// </summary>
    public struct RenderedCanvas
    {
		/// <summary>
		/// The HTML that was generated.
		/// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Javascript string which reconstructs the state used by the renderer. This is an empty string if you turned tracking it off.
        /// </summary>
        public string Data { get; set; }
    }

}
