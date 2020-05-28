using Api.StackTools;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;


namespace Api.CanvasRenderer
{
	/// <summary>
	/// Handles rendering canvases server side. Particularly useful for e.g. sending emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CanvasRendererService : ICanvasRendererService
	{
		private readonly IStackToolsService _stackTools;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CanvasRendererService(IStackToolsService stackTools)
		{
			_stackTools = stackTools;
		}

		/// <summary>
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="context">The context to use whilst rendering the canvas.
		/// This acts like POSTed page data.</param>
		/// <returns></returns>
		public Task<RenderedCanvas> Render(string bodyJson, CanvasContext context)
		{
			// A TCS will let us return when the callback runs:
			var tcs = new TaskCompletionSource<RenderedCanvas>();

			_stackTools.Request(new RenderRequest() {
				canvas = bodyJson,
				context = context
			}, (string error, JObject response) => {

				var result = error != null ? null : new RenderedCanvas()
				{
					Body = response["html"].Value<string>(),
					Title = response["meta"]["title"].Value<string>(),
				};

				tcs.TrySetResult(result);
			});

			return tcs.Task;
		}

	}

	/// <summary>
	/// A request to socialstack tools to render a canvas.
	/// </summary>
	public class RenderRequest : Request
	{
		/// <summary>
		/// Canvas JSON
		/// </summary>
		public string canvas;

		/// <summary>
		/// The context
		/// </summary>
		public CanvasContext context;

		/// <summary>
		/// 
		/// </summary>
		public RenderRequest()
		{
			action = "render";
		}
	}
}
