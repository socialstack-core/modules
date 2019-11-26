using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
    /// <summary>
    /// A rendered canvas.
    /// </summary>
    public class RenderedCanvas
    {
		/// <summary>
		/// The HTML body.
		/// </summary>
        public string Body { get; set; }
		/// <summary>
		/// The page title (used as e.g. email subject).
		/// </summary>
        public string Title { get; set; }
    }

}
