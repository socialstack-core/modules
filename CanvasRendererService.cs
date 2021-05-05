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
using Api.Eventing;
using Api.Contexts;
using Api.SocketServerLibrary;
using Api.Configuration;

namespace Api.CanvasRenderer
{
	/// <summary>
	/// Handles rendering canvases server side. Particularly useful for e.g. sending emails.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CanvasRendererService : AutoService
	{
		private readonly FrontendCodeService _frontendService;
		private readonly ContextService _contextService;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public CanvasRendererService(FrontendCodeService frontend, ContextService contexts)
		{
			_frontendService = frontend;
			_contextService = contexts;

			publicOrigin = AppSettings.Configuration["PublicUrl"];

			Events.Translation.AfterUpdate.AddEventListener((Context context, Translation translation) => {

				if (translation == null)
				{
					return new ValueTask<Translation>(translation);
				}

				// A particular locale now needs reconstructing:
				if (_engines != null && context.LocaleId > 0 && context.LocaleId <= _engines.Length)
				{
					// Nullify it:
					_engines[context.LocaleId - 1] = null;
				}

				return new ValueTask<Translation>(translation);
			});

			Events.FrontendjsAfterUpdate.AddEventListener((Context context, long buildtimestampMs) => {

				// JS file changed - drop all engines:
				_engines = null;

				return new ValueTask<long>(buildtimestampMs);
			});
		}

		/// <summary>
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="context">The context that the json will be rendered as. Any data requests are made as this user.</param>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="mode">Html only, text only, or both.</param>
		/// <param name="url">An optional URL of the page being rendered.</param>
		/// <param name="trackDataRequests">Set this to true if you'd like a JS object representing the complete state that was ultimately loaded or used by the renderer.
		/// This is vital for accurate rehydration in web clients, but a waste of cycles when it won't be used like in an email.</param>
		/// <param name="pageState">Url tokens and the primary object's JSON.</param>
		/// <returns></returns>
		public async ValueTask<RenderedCanvas> Render(Contexts.Context context, string bodyJson, PageState pageState, string url = null, bool trackDataRequests = false, RenderMode mode = RenderMode.Html)
		{
			if (context == null)
			{
				throw new ArgumentException("Context is required when rendering a canvas. It must be the context of the user that will be looking at it.", "context");
			}

			// Context locale is reliable in that it will always return something, 
			// including if the user intentionally sets a locale that doesn't exist:
			var locale = await context.GetLocale();

			// Get a JS engine for the locale:
			var engine = await GetEngine(locale);

			if (engine == null)
			{
				return new RenderedCanvas()
				{
					Failed = true,
					Body = ""
				};
			}

			// Serialise the context:
			var publicContext = await _contextService.ToJsonString(context);

			// Render the canvas now. This renderCanvas function is at the bottom of renderer.js (in the same directory as this file):
			var result = (await (engine.Invoke(
					"renderCanvas",
					bodyJson,
					context,
					// Must do this such that all of the hidden fields are correctly considered, and any modifications the JS makes don't affect our actual cached objects.
					publicContext,
					url,
					pageState,
					trackDataRequests,
					(int)mode
				) as Task<object>)) as dynamic;

			// Get body and data:
			var body = result.body as string;
			var data = result.data as string;
			var text = result.text as string;

			// The result is..
			var canvas = new RenderedCanvas()
			{
				Body = body,
				Text = text,
				Data = data
			};

			return canvas;
		}

		private readonly JsonSerializerSettings jsonFormatter = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// The origin to use in the JS context.
		/// </summary>
		private string publicOrigin;

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
		private async ValueTask<V8ScriptEngine> GetEngine(Locale locale)
		{
			if (_engines != null && _engines.Length >= locale.Id && _engines[locale.Id - 1] != null)
			{
				// Use engine for this locale:
				var cachedEngine = _engines[locale.Id - 1];
				return cachedEngine.V8Engine;
			}

			var engine = new V8ScriptEngine("Socialstack API Renderer", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);
			engine.Execute("SERVER=true;window=this;");
			var jsDoc = new V8.Document
			{
				location = new V8.Location()
			};

			jsDoc.location.origin = publicOrigin;

			engine.AddHostObject("document", new V8.Document());
			engine.AddHostObject("__console", new V8.Console());
			engine.Execute("console={};console.info=console.log=console.warn=console.error=(...args)=>__console.log(...args);");
			engine.AddHostObject("navigator", new V8.Navigator());
			engine.AddHostObject("location", jsDoc.location);
			
			/* engine.AddHostObject("host", new ExtendedHostFunctions());
				engine.AddHostObject("lib", HostItemFlags.GlobalMembers, 
				new HostTypeCollection("mscorlib", "System", "System.Core", "System.Numerics", "ClearScript.Core", "ClearScript.V8"));
			*/
			engine.SuppressExtensionMethodEnumeration = true;
			engine.AllowReflection = true;

			var dllPath = AppDomain.CurrentDomain.BaseDirectory;
			
			string sourceContent;

			sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/inline_header.js");
			engine.Execute(new DocumentInfo(new Uri("file://inline_header.js")), sourceContent);

			// If instancing a new engine, always read the file.
			var jsFileData = await _frontendService.GetAdminMainJs(locale.Id) ;
			sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
			engine.Execute(new DocumentInfo(new Uri("file://admin/main.js")), sourceContent);

			jsFileData = await _frontendService.GetMainJs(locale.Id);
			sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
			engine.Execute(new DocumentInfo(new Uri("file://ui/main.js")), sourceContent);

			jsFileData = await _frontendService.GetEmailMainJs(locale.Id);
			sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
			engine.Execute(new DocumentInfo(new Uri("file://email/main.js")), sourceContent);

			sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/renderer.js");
			engine.Execute(new DocumentInfo(new Uri("file://renderer.js")), sourceContent);

			// Add engine to locale lookup. This happens last to avoid 2 simultaneous 
			// requests trying to use a potentially not initted engine.
			if (_engines == null)
			{
				_engines = new V8.CanvasRendererEngine[locale.Id];
			}
			else if(_engines.Length < locale.Id)
			{
				Array.Resize(ref _engines, (int)locale.Id);
			}

			var cre = new V8.CanvasRendererEngine()
			{
				V8Engine = engine
			};

			_engines[locale.Id - 1] = cre;

			return engine;
		}

	}
	
	/// <summary>
	/// Representation of page state data.
	/// </summary>
	public struct PageState{
		
		/// <summary>
		/// No page state.
		/// </summary>
		public static readonly PageState None = new PageState(){
			Tokens = null,
			TokenNames = null,
			PoJson = null
		};
		
		/// <summary>
		/// Raw direct list of url tokens.
		/// </summary>
		public List<string> Tokens;
		
		/// <summary>
		/// Raw direct list of url token names.
		/// </summary>
		public List<string> TokenNames;

		/// <summary>
		/// The primary object as JSON. It's passed as JSON because of PascalCase in the API on fields, and camelCase on the frontend. JS is case sensitive.
		/// This also prevents any risk of accidental server cache modification if the actual object is passed. Can set either this or PoJson (PoJson is ideal if you already have a JSON string).
		/// </summary>
		public string PoJson;
	}
}

namespace Api.CanvasRenderer.V8
{
	
	/// <summary>
	/// The window.location object.
	/// </summary>
	public class Location
	{
		/// <summary>
		/// Location href.
		/// </summary>
		public string href = null;

		/// <summary>
		/// Location href.
		/// </summary>
		public string origin = null;
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
#pragma warning disable IDE1006 // Naming Styles
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
		/// Gets a service by the given content type ID.
		/// </summary>
		/// <param name="contentTypeId"></param>
		/// <returns></returns>
		public AutoService getService(int contentTypeId)
		{
			return Api.Startup.Services.GetByContentTypeId(contentTypeId);
		}
	}
#pragma warning restore IDE1006 // Naming Styles
}
