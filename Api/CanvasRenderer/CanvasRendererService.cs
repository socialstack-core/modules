using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.Translate;
using Api.Eventing;
using Api.Contexts;
using Api.Configuration;
using System.Text;
using System.Web;
using Api.Startup;
using Api.AutoForms;
using System.Reflection;

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
        private readonly ConfigurationService _configService;
        private CanvasRendererServiceConfig _config;

        /// <summary>
        /// Instanced automatically.
        /// </summary>
        public CanvasRendererService(FrontendCodeService frontend, ContextService contexts, ConfigurationService config)
        {
            _frontendService = frontend;
            _contextService = contexts;
            _configService = config;
            _config = GetConfig<CanvasRendererServiceConfig>();

            Events.Translation.AfterUpdate.AddEventListener((Context context, Translation translation) =>
            {

                if (translation == null)
                {
                    return new ValueTask<Translation>(translation);
                }

                // A particular locale now needs reconstructing:
                if (_engines != null && context.LocaleId > 0 && context.LocaleId <= _engines.Length)
                {
                    // Nullify it:
                    var engine = _engines[context.LocaleId - 1];
                    _engines[context.LocaleId - 1] = null;
                    ClearEngine(engine);
                }

                return new ValueTask<Translation>(translation);
            });

            Events.Locale.PotFieldValue.AddEventListener((Context context, object result, ContentField localisedField, TranslationServiceConfig translationServiceConfig) =>
            {

                if (result == null)
                {
                    return new ValueTask<object>(result);
                }

                if (translationServiceConfig.ReformatCanvasElements)
                {
                    if (IsCanvasField(localisedField))
                    {
                        result = CanvasToComponentXml((string)result);
                    }
                }

                return new ValueTask<object>(result);
            });

            Events.FrontendjsAfterUpdate.AddEventListener((Context context, long buildtimestampMs) =>
            {

				// JS file changed - drop all engines:
				ClearEngineCaches();

                return new ValueTask<long>(buildtimestampMs);
            });
        }

        /// <summary>
        /// Clears out the engine caches
        /// </summary>
        public void ClearEngineCaches()
        {
            // JS file changed - drop all engines:
            var engines = _engines;
			_engines = null;
            ClearEngineSet(engines);
		}

        private void ClearEngineSet(V8.CanvasRendererEngine[] engines)
        {
            if (engines == null)
            {
                return;
            }

            for (var i = 0; i < engines.Length; i++)
            {
                var engine = engines[i];
                ClearEngine(engine);
            }
        }

        private void ClearEngine(V8.CanvasRendererEngine engine)
        {
			if (engine == null || engine.V8Engine == null)
			{
				return;
			}

			engine.V8Engine.CollectGarbage(true);
			engine.V8Engine.Dispose();
		}

        /// <summary>
        /// True if the given field is a canvas field.
        /// </summary>
        /// <param name="contentField"></param>
        /// <returns></returns>
        public bool IsCanvasField(ContentField contentField)
        {
            var hasJsonSuffix = contentField.Name.EndsWith("Json");

            if (contentField.FieldType == typeof(string))
            {
                foreach (var attrib in contentField.FieldInfo.GetCustomAttributes<DataAttribute>())
                {
                    if (attrib.Name == "type" && (string)attrib.Value == "canvas")
                    {
                        return true;
                    }

                    if (hasJsonSuffix && attrib.Name == "contenttype" && (string)attrib.Value != "application/canvas")
                    {
                        return false;
                    }
                }
            }

            if (hasJsonSuffix)
            {
                return true;
            }

            return false;

        }


        /// <summary>
        /// Converts canvas JSON into a html-like string which will still contain component usage in there.
        /// This may be useful for sending canvas JSON to translators as it will be much more familiar for them.
        /// </summary>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public string CanvasToComponentXml(string canvas)
        {
            if (string.IsNullOrEmpty(canvas))
            {
                return null;
            }

            var rootNode = JsonConvert.DeserializeObject(canvas) as JToken;

            if (rootNode == null)
            {
                return "";
            }

            var sb = new StringBuilder();

            CanvasNodeToXml(rootNode, sb);

            return sb.ToString();
        }

        private void CanvasNodeToXml(JToken node, StringBuilder sb)
        {
            // Note that roots aren't supported as they don't serialise. They are relatively rarely used however.
            if (node is JObject)
            {
                var str = node["s"];

                if (str != null)
                {
                    // As-is:
                    sb.Append(str.ToString());
                    return;
                }

                var stringName = string.Empty;

                var kids = node["c"];
                var data = node["d"] as JObject;
                var typeName = node["t"];

                if (typeName != null)
                {
                    sb.Append('<');
                    stringName = typeName.ToString();
                    stringName = stringName.Replace('/', ':');

                    sb.Append(stringName);
                }

                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        sb.Append(' ');

                        if (kvp.Value is JObject obj)
                        {
                            sb.Append("-x-json-");
                            sb.Append(kvp.Key);
                            sb.Append("=\"");
                            sb.Append(HttpUtility.HtmlAttributeEncode(obj.ToString()));
                            sb.Append('\"');
                        }
                        else if (kvp.Value is JArray arr)
                        {
                            sb.Append("-x-json-");
                            sb.Append(kvp.Key);
                            sb.Append("=\"");
                            sb.Append(HttpUtility.HtmlAttributeEncode(arr.ToString()));
                            sb.Append('\"');
                        }
                        else
                        {
                            sb.Append(kvp.Key);
                            sb.Append("=\"");
                            sb.Append(HttpUtility.HtmlAttributeEncode(kvp.Value.ToString()));
                            sb.Append('\"');
                        }
                    }
                }

                if (typeName != null)
                {
                    if (kids == null)
                    {
                        sb.Append('/');
                    }

                    sb.Append('>');
                }

                if (kids != null)
                {
                    CanvasNodeToXml(kids, sb);

                    if (typeName != null)
                    {
                        sb.Append("</");
                        sb.Append(stringName);
                        sb.Append('>');
                    }

                }

            }
            else if (node is JArray array)
            {
                foreach (var child in array)
                {
                    CanvasNodeToXml(child, sb);
                }
            }
            else if (node is JValue)
            {
                // As-is:
                sb.Append(node.ToString());
            }

        }

        /// <summary>
        /// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
        /// then passes the body JSON and JSON serialized context to it.
        /// </summary>
        /// <param name="context">The context that the json will be rendered as. Any data requests are made as this user.</param>
        /// <param name="bodyJson">The JSON for the canvas.</param>
        /// <param name="pageState">Optional page state, JSON formatted. Is the same as pgState in JS - contains the URL, tokens and so on.</param>
        /// <param name="mode">Html only, text only, both or search.</param>
        /// <param name="absoluteUrls">Whether to prefix URLs into an absolute path.</param>
        /// <returns></returns>
        public async ValueTask<RenderedCanvas> Render(Context context, string bodyJson, string pageState, RenderMode mode = RenderMode.Html, bool absoluteUrls = true)
        {
			// Serialise the context:
			var publicContext = await _contextService.ToJsonString(context);

            return await Render(context.LocaleId, publicContext, bodyJson, pageState, mode, absoluteUrls);
		}

		/// <summary>
		/// Renders the named canvas. This invokes the `socialstack renderui` command if it's not already running
		/// then passes the body JSON and JSON serialized context to it.
		/// </summary>
		/// <param name="localeId">The locale to render in.</param>
		/// <param name="context">The context that the json will be rendered as as a JSON string.</param>
		/// <param name="bodyJson">The JSON for the canvas.</param>
		/// <param name="pageState">Optional page state, JSON formatted. Is the same as pgState in JS - contains the URL, tokens and so on.</param>
		/// <param name="mode">Html only, text only, both or search.</param>
		/// <param name="absoluteUrls">Whether to prefix URLs into an absolute path.</param>
		/// <returns></returns>
		public async ValueTask<RenderedCanvas> Render(uint localeId, string context, string bodyJson, string pageState, RenderMode mode = RenderMode.Html, bool absoluteUrls = true)
        {
            if (context == null)
            {
                throw new ArgumentException("Context is required when rendering a canvas. It must be the context of the user that will be looking at it.", "context");
            }

            // get the default rendering engine
            var engine = await GetEngine(localeId);
            
            if (engine == null)
            {
                return new RenderedCanvas()
                {
                    Failed = true,
                    Body = ""
                };
            }

            // Render the canvas now. This renderCanvas function is at the bottom of renderer.js (in the same directory as this file):
            var result = engine.Invoke(
                    "renderCanvas",
                    bodyJson,
					context,
					pageState,
					(int)mode,
                    absoluteUrls
                ) as string;

            RenderedCanvas canvas;

            if (mode == RenderMode.Both)
            {
                // It's a JSON object which must be parsed. This is rare.
                var jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(result) as JObject;

				canvas = new RenderedCanvas()
				{
					Body = jObject["body"].Value<string>(),
					Text = jObject["text"].Value<string>()
				};
			}
            else if (mode == RenderMode.Text)
            {
				// Text only.
				canvas = new RenderedCanvas()
				{
					Body = null,
					Text = result
				};
			}
            else
            {
                // HTML only.
                canvas = new RenderedCanvas() {
                    Body = result,
                    Text = null
                };
            }

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
        /// Engines per locale.
        /// </summary>
        private V8.CanvasRendererEngine[] _engines;

        private Uri _bundleUri = new Uri("file://bundle.js");
        private Uri _rendererUri = new Uri("file://renderer.js");

        /// <summary>
        /// Gets the script engine for the given locale by its locale.
        /// Also see GetEngineSearch
        /// </summary>
        /// <param name="localeId">The locale in use.</param>
        /// <returns></returns>
        private async ValueTask<V8ScriptEngine> GetEngine(uint localeId)
        {
            if (_engines != null && _engines.Length >= localeId && _engines[localeId - 1] != null)
            {
                // Use engine for this locale:
                var cachedEngine = _engines[localeId - 1];
                return cachedEngine.V8Engine;
            }

            var constraints = new V8RuntimeConstraints() {
                MaxNewSpaceSize = 512,
                MaxOldSpaceSize = 1024,
                MaxArrayBufferAllocation = 64 * 1024 * 1024
            };

            V8ScriptEngine engine;

            if (_config.EnableJsDebugger)
            {
				engine = new V8ScriptEngine(
					"Socialstack API Renderer",
					constraints,
					V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableDebugging | V8ScriptEngineFlags.EnableRemoteDebugging,
                    8118
				);
			}
            else
            {
				engine = new V8ScriptEngine(
					"Socialstack API Renderer",
					constraints,
					V8ScriptEngineFlags.DisableGlobalMembers
				);
			}

			engine.MaxRuntimeHeapSize = (UIntPtr)(1536UL * 1024 * 1024);

			engine.Execute("SERVER=true;window=this;");

            engine.AddHostObject("serviceHelper", new V8.ServiceHelper());
            engine.Execute(@"
                document = {
                    location: {
                        href: null,
                        origin: '" + _frontendService.GetPublicUrl(localeId) + @"'
                    },
                    dispatchEvent: evt => {return null;},
                    addEventListener: (a, b) => {return null;},
                    removeEventListener: a => {return null;},
                    getElementById: id => {return null;}
                };
            ");
             engine.Execute(@"
                location = document.location;
            ");
            engine.AddHostObject("__console", new V8.Console(_config.DebugToConsole));
            engine.Execute("window.addEventListener=document.addEventListener;console={};console.info=console.log=console.warn=console.error=(...args)=>__console.log(...args);");
            engine.Execute(@"
                navigator = {
                   userAgent: 'API'
                };
            ");

            // ignore setTimeout when running serverside
            engine.Execute("window.setTimeout=(a,b,c)=>{console.log('Ignoring SetTimeout',a,b,c)}");

            // Need to get the location of /content
            var contentUrl = _frontendService.GetContentUrl(localeId);
			if (!string.IsNullOrWhiteSpace(contentUrl))
            {
                engine.Execute($"global=this;global.contentSource='{contentUrl}';");
            }

            // Need to load config into its scope as well:
            engine.Execute(_configService.GetLatestFrontendConfigJs());

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
            var jsFileData = await _frontendService.GetAdminMainJs(localeId);
            sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
            engine.Execute(new DocumentInfo(new Uri("file://admin/main.js")), sourceContent);

            jsFileData = await _frontendService.GetMainJs(localeId);
            sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
            engine.Execute(new DocumentInfo(new Uri("file://ui/main.js")), sourceContent);

            jsFileData = await _frontendService.GetEmailMainJs(localeId);
            sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
            engine.Execute(new DocumentInfo(new Uri("file://email/main.js")), sourceContent);

            sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/renderer.js");
            engine.Execute(new DocumentInfo(new Uri("file://renderer.js")), sourceContent);

            // Add engine to locale lookup. This happens last to avoid 2 simultaneous 
            // requests trying to use a potentially not initted engine.
            if (_engines == null)
            {
                _engines = new V8.CanvasRendererEngine[localeId];
            }
            else if (_engines.Length < localeId)
            {
                Array.Resize(ref _engines, (int)localeId);
            }

            var cre = new V8.CanvasRendererEngine()
            {
                V8Engine = engine
            };

            _engines[localeId - 1] = cre;

            return engine;
        }

        /*
		/// <summary>
		/// Gets the search script engine for the given locale by its locale.
		/// Also see GetEngine
		/// </summary>
		/// <param name="localeId">The locale in use.</param>
		/// <returns></returns>
		private async ValueTask<V8ScriptEngine> GetEngineSearch(uint localeId)
        {
            if (_enginesSearch != null && _enginesSearch.Length >= localeId && _enginesSearch[localeId - 1] != null)
            {
                // Use search render engine for this locale:
                var cachedEngine = _enginesSearch[localeId - 1];
                return cachedEngine.V8Engine;
            }

            var engine = new V8ScriptEngine("Socialstack API Search Renderer", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);

            //set flag to identify we are being indexed for search
            engine.Execute("SERVER=true;SEARCHINDEXING=true;window=this;");

            engine.AddHostObject("serviceHelper", new V8.ServiceHelper());
            engine.Execute(@"
                document = {
                    location: {
                        href: null,
                        origin: '" + _frontendService.GetPublicUrl(localeId) + @"'
                    },
                    dispatchEvent: evt => {return null;},
                    addEventListener: (a, b) => {return null;},
                    removeEventListener: a => {return null;},
                    getElementById: id => {return null;}
                };
            ");
            engine.Execute(@"
                location = document.location;
            ");
            engine.AddHostObject("__console", new V8.Console(_config.DebugToConsole));
            engine.Execute("window.addEventListener=document.addEventListener;console={};console.info=console.log=console.warn=console.error=(...args)=>__console.log(...args);");
            engine.Execute(@"
                navigator = {
                   userAgent: 'API'
                };
            ");

            // ignore setTimeout when running serverside
            engine.Execute("window.setTimeout=(a,b,c)=>{console.log('Ignoring SetTimeout',a,b,c)}");

            // Need to get the location of /content 
            var contentUrl = _frontendService.GetContentUrl(localeId);
			if (!string.IsNullOrWhiteSpace(contentUrl))
            {
                engine.Execute($"global=this;global.contentSource='{contentUrl}';");
            }

            // Need to load config into its scope as well:
            engine.Execute(_configService.GetLatestFrontendConfigJs());

            /* engine.AddHostObject("host", new ExtendedHostFunctions());
				engine.AddHostObject("lib", HostItemFlags.GlobalMembers, 
				new HostTypeCollection("mscorlib", "System", "System.Core", "System.Numerics", "ClearScript.Core", "ClearScript.V8"));
			*
            engine.SuppressExtensionMethodEnumeration = true;
            engine.AllowReflection = true;

            var dllPath = AppDomain.CurrentDomain.BaseDirectory;

            string sourceContent;

            sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/inline_header.js");
            engine.Execute(new DocumentInfo(new Uri("file://inline_header.js")), sourceContent);

            // If instancing a new engine, always read the file.
            var jsFileData = await _frontendService.GetAdminMainJs(localeId);
            sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
            engine.Execute(new DocumentInfo(new Uri("file://admin/main.js")), sourceContent);

            jsFileData = await _frontendService.GetMainJs(localeId);
            sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
            engine.Execute(new DocumentInfo(new Uri("file://ui/main.js")), sourceContent);

            jsFileData = await _frontendService.GetEmailMainJs(localeId);
            sourceContent = System.Text.Encoding.UTF8.GetString(jsFileData.FileContent);
            engine.Execute(new DocumentInfo(new Uri("file://email/main.js")), sourceContent);

            sourceContent = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/renderer.js");
            engine.Execute(new DocumentInfo(new Uri("file://renderer.js")), sourceContent);

            // Add engine to locale lookup. This happens last to avoid 2 simultaneous 
            // requests trying to use a potentially not initted engine.
            if (_enginesSearch == null)
            {
                _enginesSearch = new V8.CanvasRendererEngine[localeId];
            }
            else if (_enginesSearch.Length < localeId)
            {
                Array.Resize(ref _enginesSearch, (int)localeId);
            }

            var cre = new V8.CanvasRendererEngine()
            {
                V8Engine = engine
            };

            _enginesSearch[localeId - 1] = cre;

            return engine;
        }
        */
    }

    /// <summary>
    /// Representation of page state data.
    /// </summary>
    public struct PageState
    {

        /// <summary>
        /// No page state.
        /// </summary>
        public static readonly PageState None = new PageState()
        {
            Tokens = null,
            TokenNames = null,
            PrimaryObject = null
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
        /// The primary object. The Id and Type are read from this, if it exists.
        /// </summary>
        public object PrimaryObject;

        /// <summary>
        /// The AutoService that provided the primary object, if there is one.
        /// </summary>
        public AutoService PrimaryObjectService;

        /// <summary>
        /// The type of the primary object, if there is one. Same as PrimaryObject.GetType()
        /// </summary>
        public Type PrimaryObjectType;
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
        /// Do print debug information to the console
        /// </summary>
        public bool DoDebug;

        /// <summary>
        /// Creates a new Console
        /// </summary>
        public Console()
        {
            DoDebug = true;
        }

        /// <summary>
        /// Creates a new Console
        /// </summary>
        /// <param name="doDebug"></param>
        public Console(bool doDebug)
        {
            DoDebug = doDebug;
        }

        /// <summary>
        /// console.log serverside
        /// </summary>
        public void log(params object[] msgs)
        {
            if (!DoDebug)
            {
                return;
            }

            for (var i = 0; i < msgs.Length; i++)
            {
                Log.Info("canvasrendererservice", JsonConvert.SerializeObject(msgs[i]));
            }
        }

        /// <summary>
        /// console.error serverside
        /// </summary>
        public void error(params object[] msgs)
        {
            for (var i = 0; i < msgs.Length; i++)
            {
                Log.Error("canvasrendererservice", null, JsonConvert.SerializeObject(msgs[i]));
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
    public class ServiceHelper
    {
        /// <summary>
        /// Gets a service by the given content type name.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public AutoService getService(string type)
        {
            return Api.Startup.Services.Get(type.ToLower() + "service");
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
