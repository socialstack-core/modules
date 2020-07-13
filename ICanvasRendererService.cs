using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;


namespace Api.CanvasRenderer
{
	/// <summary>
	/// Handles rendering canvases server side. Particularly useful for e.g. sending emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ICanvasRendererService
    {
		/// <summary>
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="context">The context to use whilst rendering the canvas.
		/// This acts like POSTed page data.</param>
		/// <returns></returns>
		Task<RenderedCanvas> Render(string bodyJson, CanvasContext context);

		/// <summary>
		/// Renders a complete set of 
		/// </summary>
		/// <param name="set"></param>
		/// <returns></returns>
		Task<List<RenderedCanvas>> Render(CanvasAndContextSet set);
    }
}
