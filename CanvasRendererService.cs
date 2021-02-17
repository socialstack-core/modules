using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.ObjectPool;
using Microsoft.ClearScript;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.Database;

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
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="context">The context that the json will be rendered as. Any data requests are made as this user.</param>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="url">An optional URL of the page being rendered.</param>
		/// <param name="trackDataRequests">Set this to true if you'd like a JS object representing the complete state that was ultimately loaded or used by the renderer.
		/// This is vital for accurate rehydration in web clients, but a waste of cycles when it won't be used like in an email.</param>
		/// <param name="customState">Functions like POSTed data; this is some initial state added to the context as "this.context.postData"</param>
		/// <returns></returns>
		public async ValueTask<RenderedCanvas> Render(Contexts.Context context, string bodyJson, string url = null, bool trackDataRequests = false, object customState = null)
		{
			// Get a JS engine:
			var engine = GetEngine();

			// Get public version of context:
			var publicContext = await context.GetPublicContext();

			// Render the canvas now. This renderCanvas function is at the bottom of renderer.js (in the same directory as this file):
			var result = (await (engine.Invoke(
					"renderCanvas",
					bodyJson,
					context,
					// Must do this such that all of the hidden fields are correctly considered, and any modifications the JS makes don't affect our actual cached objects.
					JsonConvert.SerializeObject(publicContext, jsonFormatter),
					url,
					customState,
					trackDataRequests
				) as Task<object>)) as dynamic;

			// Get body and data:
			var body = result.body as string;
			var data = result.data as string;

			// The result is..
			var canvas = new RenderedCanvas()
			{
				Body = body,
				Data = data
			};

			return canvas;
		}

		private JsonSerializerSettings jsonFormatter = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		private V8ScriptEngine _engine;

		private V8ScriptEngine GetEngine()
		{
			if (_engine != null)
			{
				return _engine;
			}
			
			var engine = new V8ScriptEngine("Socialstack API Renderer", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);
			engine.Execute("SERVER=true;window=this;");
			engine.AddHostObject("document", new V8.Document());
            engine.AddHostObject("console", new V8.Console());
			engine.AddHostObject("navigator", new V8.Navigator());
			engine.AddHostObject("location", new V8.Location());
			
			/* engine.AddHostObject("host", new ExtendedHostFunctions());
				engine.AddHostObject("lib", HostItemFlags.GlobalMembers, 
				new HostTypeCollection("mscorlib", "System", "System.Core", "System.Numerics", "ClearScript.Core", "ClearScript.V8"));
			*/
			engine.SuppressExtensionMethodEnumeration = true;
			engine.AllowReflection = true;

			var dllPath = AppDomain.CurrentDomain.BaseDirectory;

			// Module set to use:
			var modules = "UI"; // _cfg.Modules;

			var sourceContent = File.ReadAllText(
				modules == "Admin" ?
					"Admin/public/en-admin/pack/main.generated.js": modules + "/public/pack/main.generated.js"
			);
			engine.Execute(new DocumentInfo(new Uri("file://main.generated.js")), sourceContent);

			sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/renderer.js");
			engine.Execute(new DocumentInfo(new Uri("file://renderer.js")), sourceContent);
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
	/// js console.* methods.
	/// </summary>
    public class Console
	{

		/// <summary>
		/// console.log serverside
		/// </summary>
        public void log(params object[] msgs)
        {
            for (var i = 0; i < msgs.Length; i++)
            {
                System.Console.WriteLine(JsonConvert.SerializeObject(msgs[i]));
            }
        }
		
		/// <summary>
		/// console.error serverside
		/// </summary>
        public void error(params object[] msgs)
        {
            for (var i = 0; i < msgs.Length; i++)
            {
                System.Console.WriteLine("ERROR " + JsonConvert.SerializeObject(msgs[i]));
            }
        }

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
		/// Stub for dispatchEvent.
		/// </summary>
		public void dispatchEvent(object evt){ }
		
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
		
		private JsonSerializerSettings jsonFormatter = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Get content serverside by type and ID.
		/// </summary>
		public async Task getContentById(Contexts.Context context, int contentTypeId, int contentId, ScriptObject cb)
        {
			// Get the content object, and when it's done, invoke the callback.
			var content = await Content.Get(context, contentTypeId, contentId);

            // Must serialize to JSON to avoid field case sensitivity problems.
            var jsonResult = JsonConvert.SerializeObject(content, jsonFormatter);

            cb.Invoke(false, jsonResult);
        }

	}
}
