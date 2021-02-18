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
        /// True if it failed to render. This only happens when the .js files containing the components doesn't exist or can't be read by the API process.
        /// </summary>
        public bool Failed { get; set; }
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
