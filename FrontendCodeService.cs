using Api.Contexts;
using Api.Permissions;
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

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public FrontendCodeService(LocaleService locales, TranslationService translations, Themes.ThemeService themeService)
		{
			var themeConfig = themeService.GetAllConfig();
			var cssVariables = themeService.OutputCss(themeConfig);

			themeConfig.OnChange += async () => {

				// A theme was reconfigured (this also includes when the message came via contentsync as well).
				// Reconstruct the CSS now.
				cssVariables = themeService.OutputCss(themeConfig);

				await UIBuilder.SetCssPrepend(cssVariables);
			};

			initialBuildTask = Task.Run(async () =>
			{
				var dllPath = AppDomain.CurrentDomain.BaseDirectory;

				var config = GetConfig<FrontendCodeServiceConfig>();

				// The html inline header. It includes preact, preact hooks and the socialstack module require function.

				var headerFile = config.React ? "inline_header_react" : "inline_header";

				InlineJavascriptHeader = File.ReadAllText(dllPath + "/Api/ThirdParty/CanvasRenderer/"+ headerFile + ".js");

				var prebuilt = config.Prebuilt;

				// If UI/Source doesn't exist, prebuilt = true.
				if (!Directory.Exists(Path.GetFullPath("UI/Source")))
				{
					prebuilt = true;
				}

				Prebuilt = prebuilt;

				if (prebuilt)
				{
					Console.WriteLine("Running in prebuilt mode. *Not* watching your files for changes.");
					try{
						AddBuilder(UIBuilder = new UIBundle("UI", "/pack/", translations, locales) { CssPrepend = cssVariables });
						AddBuilder(EmailBuilder = new UIBundle("Email", "/pack/email-static/", translations, locales) { FilePathOverride = "/pack/" });
						AddBuilder(AdminBuilder = new UIBundle("Admin", "/en-admin/pack/", translations, locales));
					}catch(Exception e){
						Console.WriteLine("[SEVERE] " + e.ToString());
					}
				}
				else
				{
					// Get a build engine:
					var engine = GetBuildEngine();

					var globalMap = new GlobalSourceFileMap();

					// Todo: make this into a config variable. If true, the build from the watcher will be minified.
					var minify = config.Minified;

					// Create a group of build/watchers for each bundle of files (all in parallel):
					AddBuilder(UIBuilder = new UIBundle("UI", "/pack/", translations, locales, engine, globalMap, minify) { CssPrepend = cssVariables });
					AddBuilder(EmailBuilder = new UIBundle("Email", "/pack/email-static/", translations, locales, engine, globalMap, minify));
					AddBuilder(AdminBuilder = new UIBundle("Admin", "/en-admin/pack/", translations, locales, engine, globalMap, minify));

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

				Console.WriteLine("Done handling UI load.");
				initialBuildTask = null;
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
		public async ValueTask<List<StaticFileInfo>> GetStaticFiles()
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			if (initialBuildTask != null)
			{
				await initialBuildTask;
			}
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

			return set;
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

			output.Append("{\"compilerOptions\": {\"baseUrl\": \"..\",\"paths\": {");
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
			File.WriteAllText(Path.Combine(tsMeta, "tsconfig.json"), json);

			var globalsPath = Path.Combine(tsMeta, "typings.d.ts");

			if (!File.Exists(globalsPath))
			{
				File.WriteAllText(globalsPath, "import * as react from \"react\";\r\n\r\ndeclare global {\r\n\ttype React = typeof react;\r\n\tvar global: any;\r\n}");
			}
		}
	
		/// <summary>
		/// Gets the build errors from the last build of the CSS/ JS that happened. If the initial build run is happening, this waits for it to complete.
		/// </summary>
		/// <returns></returns>
		public async ValueTask<List<UIBuildError>> GetLastBuildErrors()
		{
#if DEBUG
			// Special case for devs - may need to wait for first build if it hasn't happened yet.
			if (initialBuildTask != null)
			{
				await initialBuildTask;
			}
#endif

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
		public static FrontendFile Empty = new FrontendFile() { FileContent = null, Hash = null, PublicUrl = null };

		/// <summary>
		/// The file content.
		/// </summary>
		public byte[] FileContent;

		/// <summary>
		/// The file content, gzipped.
		/// </summary>
		public byte[] Precompressed;

		/// <summary>
		/// The hash of the file.
		/// </summary>
		public string Hash;

		/// <summary>
		/// The public URL of this file.
		/// </summary>
		public string PublicUrl;
	}

}