using Api.Configuration;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;
using Api.Translate;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Api.Themes;

namespace Api.CanvasRenderer
{

    /// <summary>
    /// This service manages and generates (for devs) the frontend code.
    /// It does it by using either precompiled (as much as possible) bundles with metadata, or by compiling in-memory for devs using V8.
    /// </summary>
    [HostType("web")]
    public class FrontendCodeService : AutoService
	{
		private TaskCompletionSource uiLoadTask = new TaskCompletionSource();
		private UIBundle UIBuilder;
		private UIBundle EmailBuilder;
		private UIBundle AdminBuilder;

		/// <summary>
		/// The inline header. This should be served inline in the html. It includes preact, preact hooks and the ss module require function, totalling 13kb.
		/// </summary>
		public byte[] InlineJavascriptHeader;

		/// <summary>
		/// True if we're in prebuilt mode.
		/// </summary>
		private bool Prebuilt;

		private FrontendCodeServiceConfig _config;
		private FrontendFile? _cachedTypeMetadata;

		/// <summary>
		/// The site public URL. Never ends with a path - always just the origin and scheme, e.g. https://www.example.com
		/// </summary>
		/// <param name="localeId">The locale you want the public URL for. If no contextual locale is available, use locale #1.</param>
		/// <returns></returns>
		public string GetPublicUrl(uint localeId)
		{
			return AppSettings.GetPublicUrl(localeId);
		}

		/// <summary>
		/// The host of the /content/ and /content-private/ paths.
		/// </summary>
		/// <returns></returns>
		public string GetContentUrl(uint localeId)
		{
			if (_contentUrl != null)
			{
				return _contentUrl;
			}

			return GetPublicUrl(localeId);
		}

		private string _contentUrl;

		/// <summary>
		/// Sets the contentSource. Null is valid.
		/// </summary>
		/// <param name="contentUrl"></param>
		public void SetContentUrl(string contentUrl)
		{
			if (string.IsNullOrEmpty(contentUrl))
			{
				contentUrl = null;
			}
			
			if (_contentUrl == contentUrl)
			{
				return;
			}

			_contentUrl = contentUrl;

			// Clear caches and make sure url is suitable:
			serviceUrlByLocale = null;

		}
		
		/// <summary>
		/// Reads a file from the CanvasRenderer bundled file set.
		/// </summary>
		public string ReadModuleFileText(string filename)
		{
            var dllPath = AppDomain.CurrentDomain.BaseDirectory;
			
			// Try firstparty first:
			var firstpartyFile = dllPath + "/Api/CanvasRenderer/" + filename;
			
			if(File.Exists(firstpartyFile))
			{
				return File.ReadAllText(firstpartyFile);
			}
			
			return File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/" + filename);
		}

		/// <summary>
		/// Reads a file from the CanvasRenderer bundled file set.
		/// </summary>
		public byte[] ReadModuleFileBytes(string filename)
		{
			var dllPath = AppDomain.CurrentDomain.BaseDirectory;

			// Try firstparty first:
			var firstpartyFile = dllPath + "/Api/CanvasRenderer/" + filename;

			if (File.Exists(firstpartyFile))
			{
				return File.ReadAllBytes(firstpartyFile);
			}

			return File.ReadAllBytes(dllPath + "/Api/ThirdParty/CanvasRenderer/" + filename);
		}

		private string[] serviceUrlByLocale;

		private void InitialBuild()
		{
			var themeConfig = _themes.GetAllConfig();
			var cssVariables = _themes.OutputCss(themeConfig);

			var dllPath = AppDomain.CurrentDomain.BaseDirectory;

			// The html inline header. It includes preact, preact hooks and the socialstack module require function.

			var headerFile = _config.React ? "inline_header_react" : "inline_header";

			InlineJavascriptHeader = ReadModuleFileBytes(headerFile + ".js");
			serviceUrlByLocale = null;

			var prebuilt = _config.Prebuilt;

			// If UI/Source doesn't exist, prebuilt = true.
			if (!Directory.Exists(Path.GetFullPath("UI/Source")))
			{
				prebuilt = true;
			}

			Prebuilt = prebuilt;
			
			if (prebuilt)
			{
				Log.Info(LogTag, "Running in prebuilt mode. *Not* watching your files for changes.");
				try
				{
					AddBuilder(UIBuilder = new UIBundle("UI", "/pack/", _translations, _locales, this) { CssPrepend = cssVariables });
					AddBuilder(EmailBuilder = new UIBundle("Email", "/pack/email-static/", _translations, _locales, this) { FilePathOverride = "/pack/" });
					AddBuilder(AdminBuilder = new UIBundle("Admin", "/en-admin/pack/", _translations, _locales, this));
					CodeLoaded();
				}
				catch (Exception e)
				{
					Log.Fatal(LogTag, e, "Unable to load the UI.");
				}
			}
			else
			{
				// Must wait until all services are ready before the UI can be compiled.
				// This is because it has a dependency on knowing what services are available.
				Events.Service.AfterStart.AddEventListener(async (Context context, object src) => {
					await RunBuild();
					CodeLoaded();
					return src;
				}, 5); // Must be before other services, such as the page service
			}
		}

		private void CodeLoaded()
		{
			Log.Info(LogTag, "Done handling UI load.");
			uiLoadTask.SetResult();
			uiLoadTask = null;
		}

		private async ValueTask RunBuild()
		{
			var themeConfig = _themes.GetAllConfig();
			var cssVariables = _themes.OutputCss(themeConfig);
			
			var globalMap = new GlobalSourceFileMap(this);

			// Todo: make this into a config variable. If true, the build from the watcher will be minified.
			var minify = _config.Minified;

			var ctx = new Context(1, 1, 1);

			var bundleCache = _config.CacheBuiltFiles ? new UIBuildCache("bin") : null;

			// Load the cache:
			if (bundleCache != null)
			{
				await bundleCache.StartAsync();
				globalMap.Cache = bundleCache;
			}

			// Create a group of build/watchers for each bundle of files (all in parallel):
			AddBuilder(UIBuilder = new UIBundle("UI", "/pack/", _translations, _locales, this, globalMap, minify) { CssPrepend = cssVariables });
			AddBuilder(EmailBuilder = new UIBundle("Email", "/pack/email-static/", _translations, _locales, this, globalMap, minify));
			AddBuilder(AdminBuilder = new UIBundle("Admin", "/en-admin/pack/", _translations, _locales, this, globalMap, minify));

			var container = new SourceFileContainerSet();
			container.Bundles = SourceBuilders;

			await Events.Compiler.BeforeCompile.Dispatch(ctx, container);

			// Sort global map and build globals:
			globalMap.ConstructScssHeader();

			// Happens in a separate loop to ensure all the global SCSS has loaded first.
			foreach (var sb in SourceBuilders)
			{
				// Compile everything:
				await sb.BuildEverything();
			}

			await Events.Compiler.AfterCompile.Dispatch(ctx, container);

			if (bundleCache != null)
			{
				// Only updating the cache on startups for now
				Log.Info(LogTag, "UI build cache file hits: " + bundleCache.Hit + "/" + (bundleCache.Hit + bundleCache.Miss));
				await bundleCache.SaveAsync(globalMap, SourceBuilders);
			}
		}

		/// <summary>
		/// Gets service URLs, such as the content source and websocket one, as a javascript variable set.
		/// </summary>
		/// <returns></returns>
		public string GetServiceUrls(uint localeId)
		{
			if (serviceUrlByLocale == null)
			{
				// Generate the initial array:
				serviceUrlByLocale = new string[ContentTypes.Locales == null ? localeId : ContentTypes.Locales.Length];
			}

			if (localeId == 0)
			{
				// Invalid request
				return null;
			}

			if (localeId >= serviceUrlByLocale.Length)
			{
				// Occurs when a new locale was added
				Array.Resize(ref serviceUrlByLocale, (int)localeId);
			}

			var urls = serviceUrlByLocale[localeId - 1];

			if (string.IsNullOrEmpty(urls))
			{
				urls = GenerateServiceUrlsForLocale(localeId);
				serviceUrlByLocale[localeId - 1] = urls;
 			}

			return urls;
		}

		/// <summary>
		/// Generates service URLs, such as the content source and websocket one.
		/// </summary>
		/// <returns></returns>
		private string GenerateServiceUrlsForLocale(uint localeId)
		{
			var wsUrl = _config.WebSocketUrl;

			if (string.IsNullOrEmpty(wsUrl))
			{
				// Generate the ws host now, based on the public URL.
				// A dev site will always assume localhost:WSPORT.
				if (Services.IsDevelopment())
				{
					var portNumber = AppSettings.GetInt32("WebsocketPort", AppSettings.GetInt32("Port", 5000) + 1);

					wsUrl = "ws://localhost:" + portNumber + "/live-websocket/";
				}
				else
				{
					var pUrl = GetPublicUrl(localeId).Replace("http", "ws");

					if (pUrl.EndsWith('/'))
					{
						wsUrl = pUrl + "live-websocket/";
					}
					else
					{
						wsUrl = pUrl + "/live-websocket/";
					}
				}
			}

			var servicePaths = _config.DisableWebSocket ? "wsUrl=null;" : "wsUrl='" + wsUrl + "';";

			if (_contentUrl != null)
			{
				servicePaths += "contentSource='" + _contentUrl + "';";
			}

			return servicePaths;
		}

		/// <summary>
		/// Clears the JS caches such that the output js is reconstructed.
		/// Similar to ReloadFromFilesystem except runs on the assumption that the filesystem itself has not changed.
		/// </summary>
		public void ClearCaches()
		{
			if (SourceBuilders != null)
			{
				foreach (var bundle in SourceBuilders)
				{
					bundle.ClearCaches();
				}
			}
		}

		/// <summary>
		/// Reloads a prebuilt UI from the filesystem. Use this for zero downtime UI only deployments.
		/// </summary>
		public long ReloadFromFilesystem()
		{
			if (SourceBuilders != null && Prebuilt)
			{
				foreach (var bundle in SourceBuilders)
				{
					bundle.ReloadPrebuilt();
				}
			}

			return Version;
		}
		
		#if DEBUG
		private readonly string reloadMessage = "{\"host\":1,\"reload\":1}";
#endif

		private readonly LocaleService _locales;
		private readonly TranslationService _translations;
		private readonly ThemeService _themes;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FrontendCodeService(LocaleService locales, TranslationService translations, Themes.ThemeService themeService)
		{
			_locales = locales;
			_translations = translations;
			_themes = themeService;
			var themeConfig = themeService.GetAllConfig();

			themeConfig.OnChange += async () => {

				// A theme was reconfigured (this also includes when the message came via contentsync as well).
				// Reconstruct the CSS now.
				var cssVariables = themeService.OutputCss(themeConfig);
				
				if(UIBuilder != null)
				{
					await UIBuilder.SetCssPrepend(cssVariables);
				}
			};

			_config = GetConfig<FrontendCodeServiceConfig>();

			#if DEBUG
			Eventing.Events.FrontendAfterUpdate.AddEventListener(async (Context context, long buildNumber) => {

				if (_config.AutoReload)
				{
					try
					{
						// Send refresh websocket message to all clients
						var refreshMessage = SocketServerLibrary.Writer.GetPooled();
						refreshMessage.Start(21);
						refreshMessage.Write((uint)reloadMessage.Length);
						refreshMessage.WriteASCII(reloadMessage);

						var wsService = Services.Get<WebSockets.WebSocketService>();
						await wsService.SendToAll(refreshMessage);
					}
					catch (Exception ex)
					{
                        Log.Warn(LogTag, ex, "Failed sending auto reload message to some or all clients.");
					}
				}

				return buildNumber;
			});
#endif

			InitialBuild();

			_config.OnChange += () => {

				serviceUrlByLocale = null;

				return new ValueTask();
			};

			// Handling translation updates:
			Events.Translation.AfterUpdate.AddEventListener((Context context, Translation updated, ChangedFields diff) => {
				ClearCaches();
				return new ValueTask<Translation>(updated);
			});

			Events.Translation.AfterCreate.AddEventListener((Context context, Translation updated) => {
				ClearCaches();
				return new ValueTask<Translation>(updated);
			});
			
			Events.Translation.AfterDelete.AddEventListener((Context context, Translation updated) => {
				ClearCaches();
				return new ValueTask<Translation>(updated);
			});

			// Translation update from another node in the cluster:
			Events.Translation.Invalidate.AddEventListener((Context context, Translation translation, uint id, CacheInvalidationType type) => {
				if (CacheInvalidation.IsSingular(type))
				{
					ClearCaches();
				}
				return new ValueTask<Translation>(translation);
			});

			Events.Translation.AfterBulkInvalidate.AddEventListener((Context context, CacheInvalidationType type) => {
				ClearCaches();
				return new ValueTask<CacheInvalidationType>(type);
			});

			Events.FrontendjsAfterUpdate.AddEventListener((Context context, long buildtimestampMs) =>
			{

				// A build has occurred - clear meta cache.
				_cachedTypeMetadata = null;

				return new ValueTask<long>(buildtimestampMs);
			});
		}

		/// <summary>
		/// Frontend version. This is the same as the version of the main frontend css/js build.
		/// </summary>
		public long Version
		{
			get {
				return UIBuilder == null ? 0 : UIBuilder.BuildTimestamp;
			}
		}
		
		/// <summary>
		/// Frontend version as a string. This is the same as the version of the main frontend css/js build.
		/// </summary>
		public string VersionString
		{
			get {
				return UIBuilder == null ? "" : UIBuilder.BuildTimestampString;
			}
		}

		/// <summary>
		/// Gets the set of static files. Only used during an app build process as it needs to collect all static files.
		/// </summary>
		/// <returns></returns>
#if DEBUG
		public async ValueTask<List<StaticFileInfo>> GetStaticFiles()
		{
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

#else
		public ValueTask<List<StaticFileInfo>> GetStaticFiles()
		{
#endif
			var set = new List<StaticFileInfo>();

			var path = Prebuilt ? "" : UIBuilder.SourcePath;
			
			foreach (var filePath in Directory.EnumerateFiles(path, "*", new EnumerationOptions()
			{
				RecurseSubdirectories = true
			}))
			{
				// What sort of file are we looking at?
				// We're only interested in static files.
				
				var type = SourceFile.GetTypeMeta(UIBuilder.SourcePath, filePath, out string fileName, out string _, out string _, out string relativePath);

				if (type != SourceFileType.None || filePath.EndsWith(".d.ts"))
				{
					// Is a source file, or a directory otherwise.
					continue;
				}

				// Get file info:
				FileInfo fi = new FileInfo(filePath);

				var refString = "s:" + (UIBuilder.RootName + '/' + relativePath.Replace('\\', '/') + '/' + fileName).ToLower();

				set.Add(new StaticFileInfo()
				{
					Size = fi.Length,
					ModifiedUtc = (ulong)(fi.LastWriteTimeUtc.Ticks) / 10000,
					Ref = refString
				});
			}

#if DEBUG
			return set;
#else
			return new ValueTask<List<StaticFileInfo>>(set);
#endif
		}

		/// <summary>
		/// Gets the build errors from the last build of the CSS/ JS that happened. If the initial build run is happening, this waits for it to complete.
		/// </summary>
		/// <returns></returns>
#if DEBUG
		public async ValueTask<List<UIBuildError>> GetLastBuildErrors()
		{
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

			var uiErrors = UIBuilder.GetBuildErrors();
			var adminErrors = AdminBuilder.GetBuildErrors();
			var emailErrors = EmailBuilder.GetBuildErrors();

			if (uiErrors == null && adminErrors == null && emailErrors == null)
			{
				// Happy days!
				return null;
			}

			// Usually only one:
			if (uiErrors != null && adminErrors == null && emailErrors == null)
			{
				return uiErrors;
			}

			if (uiErrors == null && adminErrors != null && emailErrors == null)
			{
				return adminErrors;
			}

			if (uiErrors == null && adminErrors == null && emailErrors != null)
			{
				return emailErrors;
			}

			// >1 is failing. Merge them together:
			var combined = new List<UIBuildError>();

			if (uiErrors != null)
			{
				combined.AddRange(uiErrors);
			}

			if (adminErrors != null)
			{
				combined.AddRange(adminErrors);
			}

			if (emailErrors != null)
			{
				combined.AddRange(emailErrors);
			}

			return combined;
		}
#endif

		/// <summary>
		/// Gets the global scss header.
		/// </summary>
		/// <returns></returns>
		public string GetScssGlobals()
		{
			var builders = SourceBuilders;

			if (builders == null || builders.Count == 0)
			{
				return null;
			}

			var first = builders[0];

			if (first == null || first.GlobalFileMap == null)
			{
				return null;
			}

			return first.GlobalFileMap.GetScssGlobals();
		}

		/// <summary>
		/// Each source builder currently running (if there are any - can be null on production systems).
		/// </summary>
		public List<UIBundle> SourceBuilders;

		/// <summary>
		/// Adds the given builder. This primarily hooks up global file events.
		/// </summary>
		/// <param name="builder"></param>
		private void AddBuilder(UIBundle builder)
		{
			if (SourceBuilders == null)
			{
				SourceBuilders = new List<UIBundle>();
			}

			builder.OnMapChange = () => {

				// Rebuild aliases:
				Task.Run(async () => {
					await Events.Compiler.OnMapChange.Dispatch(new Context(1, 1, 1), SourceBuilders);
				});

			};

			SourceBuilders.Add(builder);

			// Start it now:
			builder.Start();
		}

		/// <summary>
		/// Gets the meta.json representing all components present. It does not have a locale associated with it.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<FrontendFile> GetTypeMeta()
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}
#endif
			if (_cachedTypeMetadata != null)
			{
				return _cachedTypeMetadata.Value;
			}

			var metaFile = new {
				BuildTime = Version,
				CodeModules = new Dictionary<string, MetaCodeModule>()
			};

			if (Prebuilt)
			{
				// Merge the meta.json files together from each sourceBuilder.
				foreach (var builder in SourceBuilders)
				{
					var meta = builder.PrebuiltMeta;

					if (meta == null || meta.CodeModules == null)
					{
						continue;
					}

					foreach (var kvp in meta.CodeModules)
					{
						if (kvp.Value == null || kvp.Value.Types == null)
						{
							continue;
						}

						metaFile.CodeModules[kvp.Key] = kvp.Value;
					}
				}
			}
			else
			{
				// Must construct the same structure as the main compiler does for type-meta.json.
				foreach (var builder in SourceBuilders)
				{
					foreach (var kvp in builder.FileMap)
					{
						var file = kvp.Value;

						if (file.FileType != SourceFileType.Javascript)
						{
							continue;
						}

						var customTypes = file.CustomTypeData;

						metaFile.CodeModules[file.ModulePath] = new MetaCodeModule
						()
						{
							Types = customTypes
						};

					}
				}
			}

			var jsonMeta = Newtonsoft.Json.JsonConvert.SerializeObject(metaFile, jsonSettings);
			var result = new FrontendFile();
			result.FileContent = System.Text.Encoding.UTF8.GetBytes(jsonMeta);
			_cachedTypeMetadata = result;
			return result;
		}

		/// <summary>
		/// Json serialization settings for canvases
		/// </summary>
		private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// Gets the main JS file as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
		/// virtually all requests that land here are responded to from RAM without allocating.
		/// </summary>
		/// <param name="localeId">The locale you want the JS for.</param>
		/// <returns></returns>
		public async ValueTask<FrontendFile> GetMainJs(uint localeId)
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

#endif
			return await UIBuilder.GetJs(localeId);
		}
		
		/// <summary>
		/// Gets the main CSS file as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
		/// virtually all requests that land here are responded to from RAM without allocating.
		/// </summary>
		/// <param name="localeId">The locale you want the JS for.</param>
		/// <returns></returns>
		public async ValueTask<FrontendFile> GetMainCss(uint localeId)
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

#endif
			return await UIBuilder.GetCss(localeId);
		}

		/// <summary>
		/// Gets the main CSS file (for admin bundle) as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
		/// virtually all requests that land here are responded to from RAM without allocating.
		/// </summary>
		/// <param name="localeId">The locale you want the JS for.</param>
		/// <returns></returns>
		public async ValueTask<FrontendFile> GetAdminMainCss(uint localeId)
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

#endif
			return await AdminBuilder.GetCss(localeId);
		}

		/// <summary>
		/// Gets the main JS file (for admin bundle) as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
		/// virtually all requests that land here are responded to from RAM without allocating.
		/// </summary>
		/// <param name="localeId">The locale you want the JS for.</param>
		/// <returns></returns>
		public async ValueTask<FrontendFile> GetAdminMainJs(uint localeId)
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

#endif
			return await AdminBuilder.GetJs(localeId);
		}

		/// <summary>
		/// Gets the main JS file (for admin bundle) as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
		/// virtually all requests that land here are responded to from RAM without allocating.
		/// </summary>
		/// <param name="localeId">The locale you want the JS for.</param>
		/// <returns></returns>
		public async ValueTask<FrontendFile> GetEmailMainJs(uint localeId)
        {
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			var loadTask = uiLoadTask;
			if (loadTask != null)
			{
				await loadTask.Task;
			}

#endif
			return await EmailBuilder.GetJs(localeId);
		}

		/// <summary>
		/// Gets a V8 engine used to host Babel, node-sass and other parts of the build chain. This is used for primarily development instances.
		/// </summary>
		/// <returns></returns>
		internal V8ScriptEngine GetBuildEngine()
		{
			var engine = new V8ScriptEngine("Socialstack API Builder", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);
			engine.Execute("window=this;");
			// engine.AddHostObject("document", new V8.Document());
			engine.AddHostObject("console", new V8.Console());
			engine.AddHostObject("navigator", new V8.Navigator());
			engine.AddHostObject("location", new V8.Location() { href = "-SocialstackCompiler-"});

			var dllPath = AppDomain.CurrentDomain.BaseDirectory;

			var buildHelpers = ReadModuleFileText("compiler.generated.js");
			engine.Execute(new DocumentInfo(new Uri("file://compiler.generated.js")), buildHelpers);

			return engine;
		}

	}

	/// <summary>
	/// A file as a raw byte[] along with a hash of the content.
	/// </summary>
	public struct FrontendFile
	{
		/// <summary>
		/// An empty file.
		/// </summary>
		public static FrontendFile Empty = new FrontendFile() { FileContent = null, Hash = null, PublicUrl = null, FqPublicUrl = null };

		/// <summary>
		/// The file content.
		/// </summary>
		public byte[] FileContent;

		/// <summary>
		/// The file content, gzipped.
		/// </summary>
		public byte[] Precompressed;

		/// <summary>
		/// The file's E-Tag. It is "hash" (the hash in quotes).
		/// </summary>
		public string Etag;

		private string _lastModdedUtcString;
		private DateTime _lastModified;

		/// <summary>
		/// The last modified date.
		/// </summary>
		public DateTime LastModifiedUtc
		{
			get
			{
				return _lastModified;
			}
			set 
			{
				_lastModified = value;
				_lastModdedUtcString = value.ToString("R");
			}
		}
		
		/// <summary>
		/// The last modified date as an RFC1123 string.
		/// </summary>
		public string LastModifiedUtcString => _lastModdedUtcString;

		/// <summary>
		/// The hash of the file.
		/// </summary>
		public string Hash;

		/// <summary>
		/// The public URL of this file.
		/// </summary>
		public string PublicUrl;

		/// <summary>
		/// The fully qualified public URL of this file. It's the PublicUrl prepended to the PublicUrl.
		/// </summary>
		public string FqPublicUrl;
	}

}