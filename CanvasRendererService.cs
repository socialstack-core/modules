using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.ObjectPool;
using Microsoft.ClearScript;
using System.IO;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// Handles rendering canvases server side. Particularly useful for e.g. sending emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CanvasRendererService : AutoService
	{
		private readonly CanvasRendererServiceConfig _cfg;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public CanvasRendererService()
		{
			_cfg = GetConfig<CanvasRendererServiceConfig>();
		}

		/// <summary>
		/// Renders a block of contexts using the same canvas.
		/// The set of results is in the exact order of the original contexts.
		/// If one fails for whatever reason, the entry will be null.
		/// </summary>
		/// <param name="set"></param>
		/// <returns></returns>
		public async Task<List<RenderedCanvas>> Render(CanvasAndContextSet set)
		{
			var results = new List<RenderedCanvas>();

			// Get one engine and render repeatedly with it:
			var engine = GetEngine();

			foreach (var ctx in set.Contexts)
			{
				// RenderCanvas actually is a promise.
				var canvas = new RenderedCanvas()
				{
					Body = (string)engine.Invoke("renderCanvas", set.BodyJson)
				};

				results.Add(canvas);
			}

			return results;
		}

		/// <summary>
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="context">The context to use whilst rendering the canvas.
		/// This acts like POSTed page data.</param>
		/// <returns></returns>
		public ValueTask<RenderedCanvas> Render(string bodyJson, CanvasContext context)
		{
			// Module set to use:
			var modules = _cfg.Modules;

			var result = new RenderedCanvas()
			{
				// TODO: This will return a promise as a Task. Establish how it handles the task return type.
				Body = (string)GetEngine().Invoke("renderCanvas", bodyJson)
			};

			return new ValueTask<RenderedCanvas>(result);
		}

		private V8ScriptEngine _engine;

		private V8ScriptEngine GetEngine()
		{
			if (_engine != null)
			{
				return _engine;
			}
			
			// TODO:
			// - Add functionality that fetch can sit on top of to request data. 
			//   It should return a cachable JSON string rather than the actual object.
			// - Rework global state such that it's still easy to get to but is instanced without needing to reinstance the whole global scope.
			// - Check what happens when multiple threads attempt Execute simultaneously.
			// - Handle renderCanvas returning a Promise
			// - Relocate the renderer.js such that it's e.g. compiled as an alt version of the main.generated.js or outputted alongside it.

			var engine = new V8ScriptEngine("Socialstack API Renderer", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);
			engine.Script.server = true;
			engine.Execute("window=this;");
			engine.AddHostObject("document", new V8.Document());
			engine.AddHostObject("navigator", new V8.Navigator());
			engine.AddHostObject("location", new V8.Location());

			/* engine.AddHostObject("host", new ExtendedHostFunctions());
				engine.AddHostObject("lib", HostItemFlags.GlobalMembers, 
				new HostTypeCollection("mscorlib", "System", "System.Core", "System.Numerics", "ClearScript.Core", "ClearScript.V8"));
			*/
			engine.SuppressExtensionMethodEnumeration = true;
			engine.AllowReflection = true;

			var sourceContent = File.ReadAllText("main.generated.js");
			engine.Execute(new DocumentInfo(new Uri("main.generated.js")), sourceContent);

			sourceContent = File.ReadAllText("renderer.js");
			engine.Execute(new DocumentInfo(new Uri("renderer.js")), sourceContent);
			_engine = engine;
			return engine;
		}

	}
}

namespace Api.CanvasRenderer.V8
{
	
	/// <summary>
	/// The window.location object.
	/// </summary>
	public class Location
	{
	}

	/// <summary>
	/// The window.navigator object.
	/// </summary>
	public class Navigator
	{
		/// <summary>
		/// User agent.
		/// </summary>
		public string userAgent = "API";
	}

	/// <summary>
	/// The window.document object.
	/// </summary>
	public class Document
	{
		/// <summary>
		/// Indicates we're serverside.
		/// </summary>
		public bool server = true;

		/// <summary>
		/// Stub for adding an event listener.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		public void addEventListener(object a, object b) { }

		/// <summary>
		/// Stub for removing an event listener.
		/// </summary>
		/// <param name="a"></param>
		public void removeEventListener(object a) { }

		/// <summary>
		/// Stub for getting an element by ID.
		/// </summary>
		/// <param name="id"></param>
		public object getElementById(object id)
		{
			return null;
		}
	}
}
