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
using Api.ColourConsole;
using System.Diagnostics;

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
                    _engines[context.LocaleId - 1] = null;
                }

                // A particular locale now needs reconstructing:
                if (_enginesSearch != null && context.LocaleId > 0 && context.LocaleId <= _enginesSearch.Length)
                {
                    // Nullify it:
                    _enginesSearch[context.LocaleId - 1] = null;
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
                _engines = null;
                _enginesSearch = null;

                return new ValueTask<long>(buildtimestampMs);
            });
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
        /// <param name="pageState">Url tokens and the primary object's JSON.</param>
        /// <param name="url">An optional URL of the page being rendered.</param>
        /// <param name="trackDataRequests">Set this to true if you'd like a JS object representing the complete state that was ultimately loaded or used by the renderer.
        /// This is vital for accurate rehydration in web clients, but a waste of cycles when it won't be used like in an email.</param>
        /// <param name="mode">Html only, text only, both or search.</param>
        /// <param name="absoluteUrls">Whether to prefix URLs into an absolute path.</param>
        /// <returns></returns>
        public async ValueTask<RenderedCanvas> Render(Contexts.Context context, string bodyJson, PageState pageState, string url = null, bool trackDataRequests = false, RenderMode mode = RenderMode.Html, bool absoluteUrls = true)
        {
            if (context == null)
            {
                throw new ArgumentException("Context is required when rendering a canvas. It must be the context of the user that will be looking at it.", "context");
            }

            // Context locale is reliable in that it will always return something, 
            // including if the user intentionally sets a locale that doesn't exist:
            var locale = await context.GetLocale();

            // Get a JS engine for the locale:
            V8ScriptEngine engine;

            if (mode == RenderMode.Search)
            {
                // get an engine configured for rendering pages for search
                engine = await GetEngineSearch(locale);
                mode = RenderMode.Html;
            }
            else
            {
                // get the default rendering engine
                engine = await GetEngine(locale);
            }

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
                    (int)mode,
                    absoluteUrls
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
        /// Engines per locale.
        /// </summary>
        private V8.CanvasRendererEngine[] _engines;
        private V8.CanvasRendererEngine[] _enginesSearch;

        private Uri _bundleUri = new Uri("file://bundle.js");
        private Uri _rendererUri = new Uri("file://renderer.js");

        /// <summary>
        /// Gets the script engine for the given locale by its locale.
        /// Also see GetEngineSearch
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

            jsDoc.location.origin = _frontendService.GetPublicUrl(locale.Id);

            engine.AddHostObject("document", new V8.Document());
            engine.AddHostObject("__console", new V8.Console(_config.DebugToConsole));
            engine.Execute("window.addEventListener=document.addEventListener;console={};console.info=console.log=console.warn=console.error=(...args)=>__console.log(...args);");
            engine.AddHostObject("navigator", new V8.Navigator());
            engine.AddHostObject("location", jsDoc.location);

            // ignore setTimeout when running serverside
            engine.Execute("window.setTimeout=(a,b,c)=>{console.log('Ignoring SetTimeout',a,b,c)}");

            // Need to get the location of /content
            var contentUrl = _frontendService.GetContentUrl(locale.Id);
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
            var jsFileData = await _frontendService.GetAdminMainJs(locale.Id);
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
            else if (_engines.Length < locale.Id)
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

        /// <summary>
        /// Gets the search script engine for the given locale by its locale.
        /// Also see GetEngine
        /// </summary>
        /// <param name="locale">The locale in use.</param>
        /// <returns></returns>
        private async ValueTask<V8ScriptEngine> GetEngineSearch(Locale locale)
        {
            if (_enginesSearch != null && _enginesSearch.Length >= locale.Id && _enginesSearch[locale.Id - 1] != null)
            {
                // Use search render engine for this locale:
                var cachedEngine = _enginesSearch[locale.Id - 1];
                return cachedEngine.V8Engine;
            }

            var engine = new V8ScriptEngine("Socialstack API Search Renderer", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);

            //set flag to identify we are being indexed for search
            engine.Execute("SERVER=true;SEARCHINDEXING=true;window=this;");

            var jsDoc = new V8.Document
            {
                location = new V8.Location()
            };

            jsDoc.location.origin = _frontendService.GetPublicUrl(locale.Id);

			engine.AddHostObject("document", new V8.Document());
            engine.AddHostObject("__console", new V8.Console(_config.DebugToConsole));
            engine.Execute("window.addEventListener=document.addEventListener;console={};console.info=console.log=console.warn=console.error=(...args)=>__console.log(...args);");
            engine.AddHostObject("navigator", new V8.Navigator());
            engine.AddHostObject("location", jsDoc.location);

            // ignore setTimeout when running serverside
            engine.Execute("window.setTimeout=(a,b,c)=>{console.log('Ignoring SetTimeout',a,b,c)}");

            // Need to get the location of /content 
            var contentUrl = _frontendService.GetContentUrl(locale.Id);
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
            var jsFileData = await _frontendService.GetAdminMainJs(locale.Id);
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
            if (_enginesSearch == null)
            {
                _enginesSearch = new V8.CanvasRendererEngine[locale.Id];
            }
            else if (_enginesSearch.Length < locale.Id)
            {
                Array.Resize(ref _enginesSearch, (int)locale.Id);
            }

            var cre = new V8.CanvasRendererEngine()
            {
                V8Engine = engine
            };

            _enginesSearch[locale.Id - 1] = cre;

            return engine;
        }
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
                WriteColourLine.Error("ERROR " + JsonConvert.SerializeObject(msgs[i]));
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
        public void dispatchEvent(object evt) { }

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
