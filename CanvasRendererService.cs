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
using Api.Translate;

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
		public CanvasRendererService(LocaleService locales)
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
			if (context == null)
			{
				throw new ArgumentException("Context is required when rendering a canvas. It must be the context of the user that will be looking at it.", "context");
			}

			// Context locale is reliable in that it will always return something, 
			// including if the user intentionally sets a locale that doesn't exist:
			var locale = await context.GetLocale();

			// Get a JS engine for the locale:
			var engine = GetEngine(locale);

			if (engine == null)
			{
				return new RenderedCanvas()
				{
					Failed = true,
					Body = ""
				};
			}

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

		/// <summary>
		/// Engines per locale.
		/// </summary>
		private V8.CanvasRendererEngine[] _engines;

		private Uri _bundleUri = new Uri("file://bundle.js");
		private Uri _rendererUri = new Uri("file://renderer.js");

		/// <summary>
		/// Gets the script engine for the given locale by its locale.
		/// </summary>
		/// <param name="locale">The locale in use.</param>
		/// <returns></returns>
		private V8ScriptEngine GetEngine(Locale locale)
		{
			if (_engines != null && _engines.Length >= locale.Id && _engines[locale.Id - 1] != null)
			{
				// Use engine for this locale:
				var cachedEngine = _engines[locale.Id - 1];
				return cachedEngine.V8Engine;
			}

			var engine = new V8ScriptEngine("Socialstack API Renderer", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);
			engine.Execute("SERVER=true;window=this;");
			var jsDoc = new V8.Document();
			jsDoc.location = new V8.Location();
			engine.AddHostObject("document", new V8.Document());
            engine.AddHostObject("console", new V8.Console());
			engine.AddHostObject("navigator", new V8.Navigator());
			engine.AddHostObject("location", jsDoc.location);
			
			/* engine.AddHostObject("host", new ExtendedHostFunctions());
				engine.AddHostObject("lib", HostItemFlags.GlobalMembers, 
				new HostTypeCollection("mscorlib", "System", "System.Core", "System.Numerics", "ClearScript.Core", "ClearScript.V8"));
			*/
			engine.SuppressExtensionMethodEnumeration = true;
			engine.AllowReflection = true;

			var dllPath = AppDomain.CurrentDomain.BaseDirectory;


			// Module set to use:
			var modules = _cfg.Modules;
			var jsFilePath = modules == "Admin" ?
					"Admin/public/en-admin/pack/" + (locale.Id == 1 ? "main" : locale.Code) + ".generated.js" : 
					modules + "/public/pack/" + (locale.Id == 1 ? "main" : locale.Code) + ".generated.js";

			string sourceContent;

			try
			{
				// If instancing a new engine, always read the file.
				sourceContent = File.ReadAllText(
					jsFilePath
				);
			}
			catch
			{
				// File doesn't exist! This will most often happen when somebody runs the API 
				// for the first time and is waiting for the initial build.
				return null;
			}

			engine.Execute(new DocumentInfo(_bundleUri), sourceContent);

			sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/renderer.js");
			engine.Execute(new DocumentInfo(_rendererUri), sourceContent);

			// Add engine to locale lookup. This happens last to avoid 2 simultaneous 
			// requests trying to use a potentially not initted engine.
			if (_engines == null)
			{
				_engines = new V8.CanvasRendererEngine[locale.Id];
			}
			else if(_engines.Length < locale.Id)
			{
				Array.Resize(ref _engines, locale.Id);
			}

			var cre = new V8.CanvasRendererEngine()
			{
				V8Engine = engine
			};

			_engines[locale.Id - 1] = cre;

			var watcher = new FileSystemWatcher();
			watcher.Path = Path.GetDirectoryName(jsFilePath); 
			watcher.Filter = Path.GetFileName(jsFilePath);
			watcher.NotifyFilter = NotifyFilters.LastWrite;

			watcher.Changed += (object source, FileSystemEventArgs e) => {
				// The js file that this engine has in memory has been changed.
				// Drop the engine from the cache now:
				_engines[locale.Id - 1] = null;

				// Dispose of watcher:
				watcher.EnableRaisingEvents = false;
				watcher.Dispose();
			};

			watcher.EnableRaisingEvents = true;
			cre.Watcher = watcher;

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
	/// A canvas renderer engine.
	/// </summary>
	public class CanvasRendererEngine
	{
		/// <summary>
		/// The V8 JS engine.
		/// </summary>
		public V8ScriptEngine V8Engine;

		/// <summary>
		/// A fs watcher which is looking for changes to the UI .js file that the engine has loaded.
		/// </summary>
		public FileSystemWatcher Watcher;
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
		/// The location of the document.
		/// </summary>
		public Location location;

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

		/// <summary>
		/// Get content serverside by type and ID.
		/// </summary>
		public async Task getContentsByFilter(Contexts.Context context, int contentTypeId, string filterJson, ScriptObject cb)
		{
			// Get the content object, and when it's done, invoke the callback.
			var content = await Content.List(context, contentTypeId, filterJson);

			// Must serialize to JSON to avoid field case sensitivity problems.
			var jsonResult = JsonConvert.SerializeObject(content, jsonFormatter);

			cb.Invoke(false, jsonResult);
		}

	}
}
