using Api.Configuration;
using Api.ContentSync;
using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Startup;
using Api.Translate;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{
	
	/// <summary>
	/// This service manages and generates (for devs) the frontend code.
	/// It does it by using either precompiled (as much as possible) bundles with metadata, or by compiling in-memory for devs using V8.
	/// </summary>
	public class FrontendCodeService : AutoService
	{
		private UIBundle UIBuilder;
		private UIBundle EmailBuilder;
		private UIBundle AdminBuilder;
		private Task initialBuildTask;
		/// <summary>
		/// The inline header. This should be served inline in the html. It includes preact, preact hooks and the ss module require function, totalling 13kb.
		/// </summary>
		public string InlineJavascriptHeader;

		/// <summary>
		/// True if we're in prebuilt mode.
		/// </summary>
		private bool Prebuilt;

		private FrontendCodeServiceConfig _config;
		private ContentSyncService _contentSync;

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

		private string[] serviceUrlByLocale;

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
				if (Configuration.Environment.IsDevelopment())
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
			else
			{
				// Attempt server ID substitution:
				wsUrl = wsUrl.Replace("${server.id}", _contentSync.ServerId.ToString()).Replace("${server.id-1}", (_contentSync.ServerId-1).ToString());
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
		public void ReloadFromFilesystem()
		{
			if (SourceBuilders != null && Prebuilt)
			{
				foreach (var bundle in SourceBuilders)
				{
					bundle.ReloadPrebuilt();
				}
			}
		}
		
		#if DEBUG
		private readonly string reloadMessage = "{\"host\":1,\"reload\":1}";
		#endif
		
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FrontendCodeService(LocaleService locales, TranslationService translations, Themes.ThemeService themeService, ContentSyncService contentSync)
		{
			_contentSync = contentSync;
			var themeConfig = themeService.GetAllConfig();
			var cssVariables = themeService.OutputCss(themeConfig);

			themeConfig.OnChange += async () => {

				// A theme was reconfigured (this also includes when the message came via contentsync as well).
				// Reconstruct the CSS now.
				cssVariables = themeService.OutputCss(themeConfig);
				
				if(UIBuilder != null)
				{
					await UIBuilder.SetCssPrepend(cssVariables);
				}
			};

			_config = GetConfig<FrontendCodeServiceConfig>();

			#if DEBUG
			Eventing.Events.FrontendAfterUpdate.AddEventListener((Context context, long buildNumber) => {

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

						if (wsService.AllClients != null)
						{
							foreach (var kvp in wsService.AllClients)
							{
								var client = kvp.Value;
								if (client != null)
								{
									client.Send(refreshMessage);
								}
							}
						}
						else
						{
							Log.Warn(LogTag, "AutoReload is on but you've specifically disabled TrackAllClients on websocket service. Unable to send the reload message.");
						}
					}
					catch (Exception ex)
					{
                        Log.Warn(LogTag, ex, "Failed sending auto reload message to some or all clients.");
					}
				}

				return new ValueTask<long>(buildNumber);
			});
			#endif

			initialBuildTask = Task.Run(async () =>
			{
				var dllPath = AppDomain.CurrentDomain.BaseDirectory;

				// The html inline header. It includes preact, preact hooks and the socialstack module require function.
				
				var headerFile = _config.React ? "inline_header_react" : "inline_header";

				InlineJavascriptHeader = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/"+ headerFile + ".js");
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
					try{
						AddBuilder(UIBuilder = new UIBundle("UI", "/pack/", translations, locales, this) { CssPrepend = cssVariables });
						AddBuilder(EmailBuilder = new UIBundle("Email", "/pack/email-static/", translations, locales, this) { FilePathOverride = "/pack/" });
						AddBuilder(AdminBuilder = new UIBundle("Admin", "/en-admin/pack/", translations, locales, this));
					}catch(Exception e){
						Log.Fatal(LogTag, e, "Unable to load the UI.");
					}
				}
				else
				{
					// Get a build engine:
					var engine = GetBuildEngine();

					var globalMap = new GlobalSourceFileMap();

					// Todo: make this into a config variable. If true, the build from the watcher will be minified.
					var minify = _config.Minified;

					// Create a group of build/watchers for each bundle of files (all in parallel):
					AddBuilder(UIBuilder = new UIBundle("UI", "/pack/", translations, locales, this, engine, globalMap, minify) { CssPrepend = cssVariables });
					AddBuilder(EmailBuilder = new UIBundle("Email", "/pack/email-static/", translations, locales, this, engine, globalMap, minify));
					AddBuilder(AdminBuilder = new UIBundle("Admin", "/en-admin/pack/", translations, locales, this, engine, globalMap, minify));

					// Sort global map:
					globalMap.Sort();

					// Make ts aliases:
					BuildTypescriptAliases();

					// Happens in a separate loop to ensure all the global SCSS has loaded first.
					foreach (var sb in SourceBuilders)
					{
						// Compile everything:
						await sb.BuildEverything();
					}
				}

                Log.Ok(LogTag, "Done handling UI load.");
                initialBuildTask = null;
			});

			_config.OnChange += () => {

				serviceUrlByLocale = null;

				return new ValueTask();
			};

			// Handling translation updates:
			Events.Translation.AfterUpdate.AddEventListener((Context context, Translation updated) => {
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
			Events.Translation.Received.AddEventListener((Context context, Translation translation, int mode) => {
				ClearCaches();
				return new ValueTask<Translation>(translation);
			});
		}

		/// <summary>
		/// Frontend version. This is the same as the version of the main frontend css/js build.
		/// </summary>
		public long Version
		{
			get {
				return UIBuilder.BuildTimestamp;
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
			if (initialBuildTask != null)
			{
				await initialBuildTask;
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
				
				var type = UIBuilder.GetTypeMeta(filePath, out string fileName, out string _, out string _, out string relativePath);

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
		/// Dev watcher mode only. Outputs a tsconfig.json file which lists all available JS/ JSX/ TS/ TSX files.
		/// </summary>
		private void BuildTypescriptAliases()
		{
			// Do any builders have typescript files in them?
			var ts = false;
			foreach (var builder in SourceBuilders)
			{
				if (builder.HasTypeScript)
				{
					ts = true;
					break;
				}
			}

			if (!ts)
			{
				return;
			}

			var output = new StringBuilder();
			
			output.Append("{\r\n\"compilerOptions\": {\"jsx\": \"react-jsx\", \"paths\": {");
			var first = true;
			
			foreach (var builder in SourceBuilders)
			{
				var rootSegment = "\":[\"" + "./" + builder.RootName + "/Source/";

				foreach (var kvp in builder.FileMap)
				{
					var file = kvp.Value;

					if (file.FileType != SourceFileType.Javascript)
					{
						continue;
					}

					if (first)
					{
						first = false;
					}
					else
					{
						output.Append(',');
					}

					var firstDot = file.FileName.IndexOf('.');
					var nameNoType = firstDot == -1 ? file.FileName : file.FileName.Substring(0, firstDot);
					
					var modPath = file.ModulePath;
					var modPathDot = file.ModulePath.LastIndexOf('.');
					
					
					if(modPathDot != -1){
						// It has a filetype - strip it:
						modPath = modPath.Substring(0, modPathDot);
					}
					
					output.Append('"');
					output.Append(modPath);
					output.Append(rootSegment);
					output.Append(file.RelativePath.Replace('\\', '/') + '/' + nameNoType);
					output.Append("\"]");
				}
			}

			output.Append("}}}");

			// tsconfig.json:
			var json = output.ToString();

			var tsMeta = Path.GetFullPath("TypeScript");

			// Create if doesn't exist:
			Directory.CreateDirectory(tsMeta);
			
			// Write tsconfig file out:
			File.WriteAllText(Path.Combine(tsMeta, "tsconfig.generated.json"), json);
			
			
			/*
			var globalsPath = Path.Combine(tsMeta, "typings.d.ts");

			if (!File.Exists(globalsPath))
			{
				File.WriteAllText(globalsPath, "import * as react from \"react\";\r\n\r\ndeclare global {\r\n\ttype React = typeof react;\r\n\tvar global: any;\r\n}");
			}
			*/
		}

		/// <summary>
		/// Gets the build errors from the last build of the CSS/ JS that happened. If the initial build run is happening, this waits for it to complete.
		/// </summary>
		/// <returns></returns>
#if DEBUG
		public async ValueTask<List<UIBuildError>> GetLastBuildErrors()
		{
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			if (initialBuildTask != null)
			{
				await initialBuildTask;
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
		/// Gets the global scss for a named bundle.
		/// </summary>
		/// <param name="bundle"></param>
		/// <returns></returns>
		public string GetGlobalScss(string bundle)
		{
			if (SourceBuilders == null || string.IsNullOrEmpty(bundle))
			{
				return null;
			}

			bundle = bundle.ToLower();

			foreach(var bundler in  SourceBuilders)
			{
				if (bundler == null)
				{
					continue;
				}

				if (bundler.RootName.ToLower() == bundle)
				{
					return bundler.GetScssGlobals();
				}
			}

			return null;
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
				BuildTypescriptAliases();

			};

			SourceBuilders.Add(builder);

			// Start it now:
			builder.Start();
		}

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
			if (initialBuildTask != null)
			{
				await initialBuildTask;
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
			if (initialBuildTask != null)
			{
				await initialBuildTask;
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
			if (initialBuildTask != null)
			{
				await initialBuildTask;
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
			if (initialBuildTask != null)
			{
				await initialBuildTask;
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
			if (initialBuildTask != null)
			{
				await initialBuildTask;
			}
#endif
			return await EmailBuilder.GetJs(localeId);
		}

		/// <summary>
		/// Gets a V8 engine used to host Babel, node-sass and other parts of the build chain. This is used for primarily development instances.
		/// </summary>
		/// <returns></returns>
		private V8ScriptEngine GetBuildEngine()
		{
			var engine = new V8ScriptEngine("Socialstack API Builder", V8ScriptEngineFlags.DisableGlobalMembers | V8ScriptEngineFlags.EnableTaskPromiseConversion);
			engine.Execute("window=this;");
			// engine.AddHostObject("document", new V8.Document());
			engine.AddHostObject("console", new V8.Console());
			engine.AddHostObject("navigator", new V8.Navigator());
			engine.AddHostObject("location", new V8.Location() { href = "-SocialstackCompiler-"});

			var dllPath = AppDomain.CurrentDomain.BaseDirectory;

			var buildHelpers = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/compiler.generated.js");
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
		/// The file's E-Tag.
		/// </summary>
		public Microsoft.Net.Http.Headers.EntityTagHeaderValue Etag;

		/// <summary>
		/// The last modified date.
		/// </summary>
		public DateTime LastModifiedUtc;

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