using Api.StackTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.CanvasRenderer
{
	/// <summary>
	/// Handles rendering canvases server side. Particularly useful for e.g. sending emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CanvasRendererService
	{
		private readonly StackToolsService _stackTools;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CanvasRendererService(StackToolsService stackTools)
		{
			_stackTools = stackTools;
		}

		/// <summary>
		/// Renders a block of contexts using the same canvas.
		/// The set of results is in the exact order of the original contexts.
		/// If one fails for whatever reason, the entry will be null.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="modules"></param>
		/// <returns></returns>
		public async Task<List<RenderedCanvas>> Render(CanvasAndContextSet set, string modules = "Admin")
		{
			var tcs = new TaskCompletionSource<List<RenderedCanvas>>();

			_stackTools.Request(new RenderRequest()
			{
				canvas = set.BodyJson,
				contexts = set.Contexts,
				modules = modules
			}, (string error, JObject response) => {

				if (error != null || response == null)
				{
					Console.WriteLine(error);
					tcs.TrySetResult(null);
					return;
				}

				var results = response["results"];

				if (results == null)
				{
					Console.WriteLine(error);
					tcs.TrySetResult(null);
					return;
				}

				var result = new List<RenderedCanvas>();

				foreach (var jsonResult in results)
				{
					var body = jsonResult["html"];
					var meta = jsonResult["meta"];

					string htmlBody;
					string title;

					if (body == null)
					{
						htmlBody = "";
					}
					else
					{
						htmlBody = body.Value<string>();
					}

					if (meta == null)
					{
						title = null;
					}
					else
					{
						var titleJson = meta["title"];
						if (titleJson == null)
						{
							title = null;
						}
						else
						{
							title = titleJson.Value<string>();
						}
					}

					result.Add(new RenderedCanvas()
					{
						Body = htmlBody,
						Title = title,
					});
				}

				tcs.TrySetResult(result);
			});
			
			return await tcs.Task;
		}

		/// <summary>
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="context">The context to use whilst rendering the canvas.
		/// This acts like POSTed page data.</param>
		/// <param name="modules"></param>
		/// <returns></returns>
		public Task<RenderedCanvas> Render(string bodyJson, CanvasContext context, string modules = "Admin")
		{
			// A TCS will let us return when the callback runs:
			var tcs = new TaskCompletionSource<RenderedCanvas>();

			_stackTools.Request(new RenderRequest() {
				canvas = bodyJson,
				context = context,
				modules = modules
			}, (string error, JObject response) => {

				var result = error != null ? null : new RenderedCanvas()
				{
					Body = response["html"].Value<string>(),
					Title = response["meta"]["title"].Value<string>()
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
		/// Multiple contexts in a multi-render request.
		/// </summary>
		public List<Dictionary<string, object>> contexts;

		/// <summary>
		/// The module set to use. Usually 'Admin', but also 'UI' or 'Email' are acceptable.
		/// </summary>
		public string modules;

		/// <summary>
		/// 
		/// </summary>
		public RenderRequest()
		{
			action = "render";
		}
	}
}
