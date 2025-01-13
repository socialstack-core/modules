using Api.Contexts;
using Api.Eventing;
using Api.Translate;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Api.CanvasRenderer
{

    /// <summary>
    /// A UI bundle such as admin UI or the frontend one. Includes source watcher on dev which watches for changes in the source code in a particular directory.
    /// Source watchers for each directory are not quite independent - they are independent for everything except for "global" SCSS, such as theme variables and mixins.
    /// </summary>
    public class UIBundle
    {
        /// <summary>
        /// True if builds from the watcher should be minified.
        /// Generally recommended to leave it off, but try at least once as there are a collection of known bugs in the Babel minifier.
        /// </summary>
        public bool Minified;

        /// <summary>
        /// Filesystem path. e.g. "C:\\Projects\\UI".
        /// </summary>
        public string RootPath;

        /// <summary>
        /// Filesystem root name. e.g. "UI".
        /// </summary>
        public string RootName;

        /// <summary>
        /// Filesystem path. e.g. "C:\\Projects\\UI\\Source".
        /// </summary>
        public string SourcePath;

        /// <summary>
        /// The public pack directory.
        /// </summary>
        public string PackDir;

        /// <summary>
        /// Overrides PackDir for the actual file during prebuilt load.
        /// </summary>
        public string FilePathOverride;

        /// <summary>
        /// Map of path (relative to Path) -> a particular source file.
        /// </summary>
        public ConcurrentDictionary<string, SourceFile> FileMap = new ConcurrentDictionary<string, SourceFile>();

        /// <summary>
        /// Map of path (relative to Path) -> a particular *global* source file.
        /// </summary>
        public GlobalSourceFileMap GlobalFileMap;

        /// <summary>
        /// The build engine which does compilation for us.
        /// </summary>
        public V8ScriptEngine BuildEngine;

        private readonly TranslationService _translationService;
        private readonly LocaleService _localeService;
        private readonly TranslationServiceConfig _translationServiceConfig;

        /// <summary>
        /// UTC timestamp in milliseconds of last build. This regularly changes on a dev instance, but is constant on prod as it comes from a file.
        /// </summary>
        private long BuildTimestampMs = 1;

        /// <summary>
        /// UTC timestamp in milliseconds of last build. 
        /// </summary>
        public long BuildTimestamp
        {
            get
            {
                return BuildTimestampMs;
            }
        }

        private List<UIBuildError> JsBuildErrors = new List<UIBuildError>();
        private List<UIBuildError> CssBuildErrors = new List<UIBuildError>();

        /// <summary>
        /// Invoked when the map changes.
        /// </summary>
        public Action OnMapChange;

        /// <summary>
        /// True if using prebuilt UI.
        /// </summary>
        private bool Prebuilt;

        /// <summary>
        /// (Dev mode only). True if there is at least one .ts or .tsx file that is not a .d. defs file.
        /// </summary>
        public bool HasTypeScript;

        /// <summary>
        /// Options when transforming the JS.
        /// </summary>
        private TransformOptions TransformOptions;

        /// <summary>
        /// A list of UI build errors. Only exists on dev mode.
        /// </summary>
        public List<UIBuildError> GetBuildErrors()
        {
            if (JsBuildErrors.Count == 0 && CssBuildErrors.Count == 0)
            {
                return null;
            }

            var combined = new List<UIBuildError>();
            combined.AddRange(JsBuildErrors);
            combined.AddRange(CssBuildErrors);
            return combined;
        }

        /// <summary>
        /// Creates a new bundle for the given filesystem path.
        /// </summary>
        public UIBundle(
            string rootPath, string packDir, TranslationService translations, LocaleService locales, FrontendCodeService frontend, V8ScriptEngine buildEngine, GlobalSourceFileMap globalFileMap, bool minify)
        {
            RootName = rootPath;
            RootPath = Path.GetFullPath(rootPath);
            SourcePath = Path.GetFullPath(rootPath + "/Source");
            BuildEngine = buildEngine;
            GlobalFileMap = globalFileMap;
            _translationService = translations;
            _localeService = locales;
            Minified = minify;
            PackDir = packDir;
            _frontend = frontend;

            TransformOptions = new TransformOptions()
            {
                minified = minify
            };

            _translationServiceConfig = translations.GetConfig<TranslationServiceConfig>();

        }

        /// <summary>
        /// Creates a new bundle for the given filesystem path.
        /// </summary>
        public UIBundle(string rootPath, string packDir, TranslationService translations, LocaleService locales, FrontendCodeService frontend)
        {
            RootName = rootPath;
            RootPath = Path.GetFullPath(rootPath);
            SourcePath = Path.GetFullPath(rootPath + "/Source");
            _translationService = translations;
            _localeService = locales;
            _frontend = frontend;
            Prebuilt = true;
            PackDir = packDir;

            _translationServiceConfig = translations.GetConfig<TranslationServiceConfig>();
        }

        /// <summary>
        /// Clears the JS locale caches.
        /// </summary>
        public void ClearCaches()
        {
            _localeToMainJs = null;
        }

        /// <summary>
        /// Reloads a prebuilt UI bundle from the filesystem.
        /// </summary>
        public void ReloadPrebuilt()
        {
            if (Prebuilt)
            {
                // Load it now:
                LoadPrebuiltUI();
            }
        }

        /// <summary>
        /// Prepend text to add to the CSS. Use SetCssPrepend except during construction.
        /// </summary>
        public string CssPrepend;

        /// <summary>
        /// Sets the given CSS to be prepended to any CSS this outputs.
        /// </summary>
        /// <param name="css"></param>
        public async ValueTask SetCssPrepend(string css)
        {
            CssPrepend = css;

            if (BuiltCss == null)
            {
                // Not loaded yet.
                return;
            }

            if (Prebuilt)
            {
                // Load main.css (as-is):
                var cssFilePath = RootPath + "/public" + PackDir + "main.prebuilt.css";
                if (File.Exists(cssFilePath))
                {
                    var mainCss = File.ReadAllText(cssFilePath);
                    BuiltCss = (CssPrepend == null ? "" : CssPrepend) + mainCss;
                    CssFile.FileContent = null;
                }
            }
            else
            {
                // Reconstruct the CSS:
                await ConstructCss();
            }
        }

        /// <summary>
        /// Determines the given file name + type as a particular useful source file type. "None" if it didn't.
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public SourceFileType DetermineFileType(string fileType, string fileName)
        {
            if (fileType == "js" || fileType == "jsx")
            {
                return SourceFileType.Javascript;
            }
            else if ((fileType == "ts" || fileType == "tsx") && !fileName.EndsWith(".d.ts") && !fileName.EndsWith(".d.tsx"))
            {
                HasTypeScript = true;
                return SourceFileType.Javascript;
            }
            else if (fileType == "css" || fileType == "scss")
            {
                return SourceFileType.Scss;
            }
            else if (fileName == "module.json")
            {
                return SourceFileType.ModuleMeta;
            }
            else if (fileType == "json" && Regex.Match(fileName, "locale\\.[a-z]+\\.json").Success)
            {
                return SourceFileType.Locale;
            }

            return SourceFileType.None;
        }

        /// <summary>
        /// True if this bundle contains UI/Start.
        /// </summary>
        public bool ContainsStarterModule;

        /// <summary>
        /// Cached localeId -> JS file bytes. Locale IDs are always intended to be low range (as all locale lookups work this way, rather than introducing a dictionary).
        /// </summary>
        private FrontendFile[] _localeToMainJs = null;


        /// <summary>
        /// Gets an md5 lowercase hash for the given content.
        /// </summary>
        /// <param name="fileContent"></param>
        /// <returns></returns>
        private string GetHash(byte[] fileContent)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(fileContent);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// The built frontend file. This is a byte[] in memory for fast return speeds.
        /// </summary>
        private FrontendFile CssFile = FrontendFile.Empty;

        /// <summary>
        /// Gets the main CSS file for this bundle as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
        /// virtually all requests that land here are responded to from RAM without allocating.
        /// </summary>
        /// <param name="localeId">The locale you want the JS for.</param>
        /// <returns></returns>
        public ValueTask<FrontendFile> GetCss(uint localeId)
        {
            // CSS files do not currently do any localisation specific things, but will likely do so in the future, so this is an async method.
            // Images containing text reffed by them for example.

            if (CssFile.FileContent == null)
            {
                var bytes = Encoding.UTF8.GetBytes(BuiltCss);
                CssFile.FileContent = bytes;
                CssFile.Precompressed = Compress(bytes);
                var hash = GetHash(CssFile.FileContent);
                CssFile.Hash = hash;
                CssFile.LastModifiedUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(BuildTimestampMs);
                CssFile.Etag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"" + hash + "\"");

				SetCssPath(localeId);
            }

            return new ValueTask<FrontendFile>(CssFile);
        }

        /// <summary>
        /// Gets the main JS file for this bundle as a raw, always from memory file. Note that although the initial generation of the response is dynamic, 
        /// virtually all requests that land here are responded to from RAM without allocating.
        /// </summary>
        /// <param name="localeId">The locale you want the JS for.</param>
        /// <returns></returns>
        public async ValueTask<FrontendFile> GetJs(uint localeId)
        {
            if (localeId <= 0)
            {
                return FrontendFile.Empty;
            }

            // The vast majority of these hits will come from the following cache.
            if (_localeToMainJs != null && localeId <= _localeToMainJs.Length)
            {
                // It's in range already. Does the cached version exist?
                var cached = _localeToMainJs[localeId - 1];

                if (cached.FileContent != null)
                {
                    return cached;
                }
            }

            // Not cached - construct the localised JS now. 
            // On prod, this happens using a precompiled file and some meta about where the template literals are.
            // It supports the precompiled file being minified as well.

            // Does the locale exist? (intentionally using a blank context here - it must only vary by localeId)
            var locale = await _localeService.Get(new Context(), localeId, DataOptions.IgnorePermissions);

            if (locale == null)
            {
                // Dodgy locale - quit:
                return FrontendFile.Empty;
            }

            // Load all the local database translations:
            var translationList = await _translationService.Where(DataOptions.IgnorePermissions).ListAll(new Context()
            {
                LocaleId = localeId
            });

            // Load any codebase translations
            var baseTranslations = LoadModuleTranslations(locale);

            // Create a lookup:
            var translationLookup = new Dictionary<string, Translation>();

            // Start with the baseline entries             
            if (baseTranslations != null)
            {
                foreach (var translation in baseTranslations)
                {
                    translationLookup[translation.Module + "/" + translation.Original] = translation;
                }
            }

            // Add/replace with local values from the db 
            if (translationList != null)
            {
                foreach (var translation in translationList)
                {
                    if (translationLookup.ContainsKey(translation.Module + "/" + translation.Original))
                    {
                        // only replace with local if has been translated 
                        if (!string.IsNullOrWhiteSpace(translation.Translated) && translation.Original != translation.Translated)
                        {
                            translationLookup[translation.Module + "/" + translation.Original] = translation;
                        }
                    }
                    else
                    {
                        translationLookup[translation.Module + "/" + translation.Original] = translation;
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (var segment in BuiltJavascriptSegments)
            {
                if (segment.Source != null)
                {
                    sb.Append(segment.Source);
                }
                else
                {
                    // Perform substitution
                    if (translationLookup.TryGetValue(segment.Module + "/" + segment.TemplateLiteralToSearch, out Translation translation))
                    {
                        // Got a hit! The source is likely minified (on prod it pretty much always is) 
                        // so we'll need to consider mapping variables from the translated form.

                        // Do we even have a translated value?
                        if (string.IsNullOrEmpty(translation.Translated))
                        {
                            // Retain original as-is:
                            sb.Append(segment.TemplateLiteralSource);
                        }
                        else
                        {
                            if (segment.VariableMap != null)
                            {
                                // It has a variable map so we'll need to map the variables in the translation.
                                sb.Append(RemapTemplateLiteralVariables(translation.Translated, segment.VariableMap));
                            }
                            else
                            {
                                // It's not minified so use as-is.
                                sb.Append(translation.Translated);
                            }
                        }
                    }
                    else
                    {
                        // The text has no localised substitution - keep as-is.
                        sb.Append(segment.TemplateLiteralSource);
                    }
                }
            }

            var resultJs = sb.ToString();

            // Get the bytes of the result:
            var bytes = Encoding.UTF8.GetBytes(resultJs);

            // Min array length is just localeId (as we use localeId-1 as the index):
            if (_localeToMainJs == null)
            {
                _localeToMainJs = new FrontendFile[localeId];
            }
            else if (_localeToMainJs.Length < localeId)
            {
                Array.Resize(ref _localeToMainJs, (int)localeId);
            }

            // Create a hash of the file bytes.
            // This is important because the build timestamp doesn't change when somebody edits locale strings in the admin area, and users would continue to see the cached strings.
            // Because multiple servers may be hashing the contents too, it's very important that they're in the same order.
            // (On prod, this order is covered just by the pregenerated js file - it ultimately only gets lightly edited).
            var hash = GetHash(bytes);

            var pubUrl = PackDir + "main.js?v=" + BuildTimestampMs + "&h=" + hash + "&lid=" + localeId;

            // Add to cache:
            var file = new FrontendFile()
            {
                FileContent = bytes,
                Precompressed = Compress(bytes),
                Hash = hash,
                Etag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"" + hash + "\""),
                LastModifiedUtc = new DateTime(1970, 1, 1, 0, 0,0,DateTimeKind.Utc).AddMilliseconds(BuildTimestampMs),
				PublicUrl = pubUrl,
                FqPublicUrl = FullyQualify(pubUrl, localeId)
            };

            _localeToMainJs[localeId - 1] = file;

            return file;
        }

        /// <summary>
        /// Extracts any module text and ensures that we have entries in the translation tables 
        /// </summary>
        /// <param name="localeId">The locale you want to process translations for</param>
        /// <returns></returns>
        public async ValueTask<bool> PopulateDefaultTranslations(uint localeId)
        {
            if (localeId <= 0)
            {
                return false;
            }

            // Does the locale exist? (intentionally using a blank context here - it must only vary by localeId)
            var locale = await _localeService.Get(new Context(), localeId, DataOptions.IgnorePermissions);

            if (locale == null)
            {
                // Dodgy locale - quit:
                return false;
            }

            // Load all the local database translations:
            var translationList = await _translationService.Where(DataOptions.IgnorePermissions).ListAll(new Context()
            {
                LocaleId = localeId
            });

            // Load any codebase translations
            var baseTranslations = LoadModuleTranslations(locale);

            // Create a lookup:
            var translationLookup = new Dictionary<string, Translation>();

            // Start with the baseline entries             
            if (baseTranslations != null)
            {
                foreach (var translation in baseTranslations)
                {
                    translationLookup[translation.Module + "/" + translation.Original] = translation;
                }
            }

            // Add/replace with local values from the db 
            if (translationList != null)
            {
                foreach (var translation in translationList)
                {
                    if (translationLookup.ContainsKey(translation.Module + "/" + translation.Original))
                    {
                        // only replace with local if has been translated 
                        if (!string.IsNullOrWhiteSpace(translation.Translated) && translation.Original != translation.Translated)
                        {
                            translationLookup[translation.Module + "/" + translation.Original] = translation;
                        }
                    }
                    else
                    {
                        translationLookup[translation.Module + "/" + translation.Original] = translation;
                    }
                }
            }

            // add any baseline entries into the backend ?
            if (_translationServiceConfig.AutoAddBaseLineTranslations && baseTranslations != null)
            {
                foreach (var translation in baseTranslations)
                {
                    // is it in the database (may have been added)
                    var f = _translationService.Where("Module=? and Original=?", DataOptions.IgnorePermissions);
                    f.PageSize = 1;
                    f.Bind(translation.Module).Bind(translation.Original);
                    var latestTranslation = await f.First(new Context()
                    {
                        LocaleId = localeId
                    });

                    // do we have a backend entry for the core translation?
                    if (latestTranslation == null)
                    {
                        var newTranslation = new Translation()
                        {
                            Module = translation.Module,
                            Original = translation.Original,
                            Translated = translation.Original,
                            CreatedUtc = DateTime.UtcNow
                        };
                        var addedTranslation = await _translationService.Create(new Context(), newTranslation, DataOptions.IgnorePermissions);

						Log.Info("frontendcodeservice", $"Adding default translation {translation.Module}/{translation.Original}");

                        // add the translated value if necessary
                        if (translation.Original != translation.Translated && localeId != 1)
                        {
							Log.Info("frontendcodeservice", $"Updating default translation {addedTranslation.Id} {translation.Module}/{translation.Original} {translation.Translated}");

                            addedTranslation = await _translationService.Update(new Context() { LocaleId = localeId }, addedTranslation, (Context ctx, Translation trans, Translation orig) =>
                            {
                                trans.Translated = translation.Translated;
                            }, DataOptions.IgnorePermissions);
                        }
                    }
                    else if (localeId != 1 && translation.Original != translation.Translated && latestTranslation.Original == latestTranslation.Translated)
                    {
                        Log.Info("frontendcodeservice",$"Updating default translation {latestTranslation.Id} {translation.Module}/{translation.Original} {translation.Translated}");

                        await _translationService.Update(new Context() {  LocaleId = localeId }, latestTranslation, (Context ctx, Translation trans, Translation orig) =>
                        {
                            trans.Translated = translation.Translated;
                        }, DataOptions.IgnorePermissions);
                    }
                }
            }

            if (_translationServiceConfig.AutoAddTranslationElements)
            {
                if(BuiltJavascriptSegments == null)
                {
                    BuiltJavascriptSegments = GetSegments();
                }

                foreach (var segment in BuiltJavascriptSegments)
                {
                    if (segment.Source != null)
                    {
                        continue;
                    }

                    // Check for substitution
                    if (translationLookup.TryGetValue(segment.Module + "/" + segment.TemplateLiteralToSearch, out Translation translation))
                    {
                        // Got a hit so ignore 
                        continue;
                    }

                    if (_translationServiceConfig.AutoAddExcludeModules == null || !_translationServiceConfig.AutoAddExcludeModules.Contains(segment.Module, StringComparer.InvariantCultureIgnoreCase))
                    {
                        // final check of the database 
                        var f = _translationService.Where("Module=? and Original=?", DataOptions.IgnorePermissions);
                        f.PageSize = 1;
                        f.Bind(segment.Module).Bind(segment.TemplateLiteralToSearch);
                        var latestTranslation = await f.First(new Context()
                        {
                            LocaleId = localeId
                        });

                        if (latestTranslation == null)
                        {
                            // Auto add a new entry to the translations table 
                            var newTranslation = new Translation()
                            {
                                Module = segment.Module,
                                Original = segment.TemplateLiteralToSearch,
                                Translated = segment.TemplateLiteralToSearch,
                                CreatedUtc = DateTime.UtcNow
                            };
                            await _translationService.Create(new Context(), newTranslation, DataOptions.IgnorePermissions);

                            Log.Info("frontendcodeservice", $"Adding missing translation {segment.Module}/{segment.TemplateLiteralToSearch}");
                        }
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Compresses the given array of bytes.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
                gzipStream.Close();
                return compressedStream.ToArray();
            }
        }

        /// <summary>
        /// Remaps the variables in a template literal using the given mapping.
        /// Template literals here MUST use simple variable substitution only, rather than potentially complex expressions.
        /// </summary>
        /// <param name="templateLiteralContents"></param>
        /// <param name="variableMap"></param>
        /// <returns></returns>
        private string RemapTemplateLiteralVariables(string templateLiteralContents, Dictionary<string, string> variableMap)
        {
            if (variableMap == null || variableMap.Count == 0)
            {
                return templateLiteralContents;
            }

            var builder = new StringBuilder();
            var mode = 0;
            var expressionStart = 0;

            for (var i = 0; i < templateLiteralContents.Length; i++)
            {
                var chr = templateLiteralContents[i];

                if (mode == 1)
                {
                    // Inside an expression. Track it until the terminal.
                    // Huge simplification of the spec here - we only terminate when we encounter a }.
                    // Officially it could contain strings with }, or even functions with lots of bracket nesting, 
                    // but nobody should be expected to translate text with _functionality_ in it.
                    if (chr == '}')
                    {
                        // Terminal of the expression.
                        mode = 0;

                        var variableToLookup = templateLiteralContents.Substring(expressionStart, i - expressionStart);
                        if (variableMap.TryGetValue(variableToLookup, out string mappedTo))
                        {
                            builder.Append("${");
                            builder.Append(mappedTo);
                            builder.Append("}");
                        }
                        else
                        {
                            // Translation contains a variable that does not exist.
                            // Append it with the variable still in there, but without the $:
                            builder.Append("{" + variableToLookup + "}");
                        }

                    }
                }
                else if (chr == '$' && PeekString(templateLiteralContents, i + 1) == '{')
                {
                    mode = 1;
                    expressionStart = i + 2;
                    i++;
                }
                else
                {
                    builder.Append(chr);
                }

            }

            return builder.ToString();
        }

        /// <summary>
        /// Get filetype meta for the given path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="fileNameNoType"></param>
        /// <param name="fileType"></param>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public SourceFileType GetTypeMeta(string filePath, out string fileName, out string fileNameNoType, out string fileType, out string relativePath)
        {
            var lastSlash = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            fileName = filePath.Substring(lastSlash + 1);
            var relLength = lastSlash - SourcePath.Length - 1;
            fileNameNoType = null;
            fileType = null;
            relativePath = null;

            if (relLength <= 0)
            {
                // Directory
                return SourceFileType.Directory;
            }

            relativePath = filePath.Substring(SourcePath.Length + 1, relLength);
            var typeDot = fileName.LastIndexOf('.');

            if (typeDot == -1)
            {
                // Directory
                return SourceFileType.Directory;
            }

            fileType = fileName.Substring(typeDot + 1).ToLower();
            fileNameNoType = fileName.Substring(0, typeDot);

            // Check if the file name matters to us. If the path contains /static/ it never does:

            var tidyFileType = filePath.IndexOf(Path.DirectorySeparatorChar + "static" + Path.DirectorySeparatorChar) == -1 ?
                    DetermineFileType(fileType, fileName) :
                    SourceFileType.None;

            return tidyFileType;
        }

        /// <summary>
        /// Adds the file at the given source-relative path to the map.
        /// </summary>
        /// <param name="filePath"></param>
        private SourceFile AddToMap(string filePath)
        {
            var tidyFileType = GetTypeMeta(filePath, out string fileName, out string fileNameNoType, out string fileType, out string relativePath);

            if (tidyFileType == SourceFileType.None || tidyFileType == SourceFileType.Directory)
            {
                // Nope
                return null;
            }

            // Yes - start tracking it.

            var isGlobal = false;
            var priority = 100;

            if (tidyFileType == SourceFileType.Scss)
            {
                // Is it a global SCSS file?
                isGlobal = fileName.Contains(".global.") || fileName.StartsWith("global.");

                // 2nd last part of the file can be a number - the priority order of the scss.
                var parts = fileName.Split('.');

                if (parts.Length > 2)
                {
                    int.TryParse(parts[parts.Length - 2], out priority);
                }
            }

            // Is it a thirdparty module?
            var thirdParty = false;
            string modulePath = RootName + '/' + relativePath.Replace('\\', '/');


            if (modulePath.IndexOf("/ThirdParty/") != -1)
            {
                thirdParty = true;
                // Remove /ThirdParty/ and build module path that it represents:
                modulePath = modulePath.Replace("/ThirdParty/", "/");
            }

            if (modulePath.IndexOf(".Bundle/") != -1)
            {
                // Remove *.Bundle/ and build module path that it represents:
                var pieces = modulePath.Split('/');
                var newPath = "";
                for (var i = 0; i < pieces.Length; i++)
                {
                    if (pieces[i].EndsWith(".Bundle"))
                    {
                        continue;
                    }

                    if (newPath != "")
                    {
                        newPath += "/";
                    }

                    newPath += pieces[i];
                }

                modulePath = newPath;
            }

            // Use shortform module name if the last directory of the modulePath matches the filename.
            if (!modulePath.EndsWith("/" + fileNameNoType))
            {
                modulePath += "/" + fileName;
            }

            if (modulePath == "UI/Start")
            {
                ContainsStarterModule = true;
            }

            var file = new SourceFile()
            {
                Path = filePath,
                FileName = fileName,
                IsGlobal = isGlobal,
                Priority = priority,
                FileType = tidyFileType,
                RawFileType = fileType,
                ThirdParty = thirdParty,
                ModulePath = modulePath,
                FullModulePath = RootName + '/' + relativePath.Replace('\\', '/'),
                RelativePath = relativePath
            };

            if (isGlobal)
            {
                GlobalFileMap.FileMap[filePath] = file;
            }

            FileMap[filePath] = file;

            return file;
        }

        private void LoadPrebuiltUI()
        {
            var dir = PackDir;

            if (!string.IsNullOrEmpty(FilePathOverride))
            {
                dir = FilePathOverride;
            }

            // Load main.css (as-is):
            var cssFilePath = RootPath + "/public" + dir + "main.prebuilt.css";
            if (File.Exists(cssFilePath))
            {
                var mainCss = File.ReadAllText(cssFilePath);
                BuiltCss = (CssPrepend == null ? "" : CssPrepend) + mainCss;
            }
            else
            {
                Log.Fatal(
                    "frontendcodeservice",
                    null,
                    "Your UI is set to prebuilt mode (either in appsettings.json, or because you don't have a UI/Source folder), but the prebuilt CSS file at '" + cssFilePath +
                    "' does not exist. Your site is missing its CSS."
                );

                BuiltCss = "";
            }

            // Load main.js.
            var segments = new List<JavascriptFileSegment>();
            var jsFilePath = RootPath + "/public" + dir + "main.prebuilt.js";

            if (File.Exists(jsFilePath))
            {
                var mainJs = File.ReadAllText(jsFilePath);

                // Read the meta (a required file, as it includes the build date as well as localisation metadata).
                var metaPath = RootPath + "/public" + dir + "meta.json";
                PrebuiltMeta meta;

                if (File.Exists(metaPath))
                {
                    var metaJson = File.ReadAllText(metaPath);
                    meta = Newtonsoft.Json.JsonConvert.DeserializeObject<PrebuiltMeta>(metaJson);
                }
                else
                {
                    meta = new PrebuiltMeta();

                    // This will be an accurate guess in virtually all situations 
                    // (however, it would be better to fix the problem of the missing meta file):
                    meta.Starter = RootName == "UI";

                    Log.Error(
                        "frontendcodeservice",
                        null,
                        "Your UI is set to prebuilt mode (either in appsettings.json, or because you don't have a UI/Source folder), but the prebuilt metadata file at '" + metaPath +
                        "' does not exist. Your site will be unable to apply localised text and you'll encounter caching problems as the build number isn't being set properly."
                    );
                }

                // Build time:
                BuildTimestampMs = meta.BuildTime;
                ContainsStarterModule = meta.Starter;

                // Construct the JS segments now.
                var currentIndex = 0;

                if (meta.Templates != null)
                {
                    foreach (var template in meta.Templates)
                    {
                        // Create segment which goes from currentIndex -> template.Start:
                        segments.Add(
                            new JavascriptFileSegment()
                            {
                                Source = mainJs.Substring(currentIndex, template.Start - currentIndex)
                            }
                        );

                        // Add the template segment:
                        segments.Add(
                            new JavascriptFileSegment()
                            {
                                Module = template.Module,
                                TemplateLiteralToSearch = template.Original,
                                TemplateLiteralSource = template.Target,
                                VariableMap = template.VariableMap
                            }
                        );

                        // Current index is now the end of the template:
                        currentIndex = template.End;
                    }
                }

                // Final segment:
                segments.Add(
                    new JavascriptFileSegment()
                    {
                        Source = mainJs.Substring(currentIndex)
                    }
                );

                if (ContainsStarterModule)
                {
                    // Invoke start:
                    segments.Add(new JavascriptFileSegment() { Source = "_rq('UI/Start').default();" });
                }
            }
            else
            {
                Log.Fatal(
                    "frontendcodeservice",
                    null,
                    "Your UI is set to prebuilt mode (either in appsettings.json, or because you don't have a UI/Source folder), but the prebuilt JS file at '" + jsFilePath +
                    "' does not exist. Your site will be missing its JS."
                );

                // Be as helpful as we can - output to UI as well:
                segments.Add(new JavascriptFileSegment()
                {
                    Source = "console.error('This site is set to prebuilt mode but no prebuilt JS file exists. See the file location in your server log.');"
                });

            }

            BuiltJavascriptSegments = segments;
            _localeToMainJs = null;
            CssFile.FileContent = null;
        }

        /// <summary>
        /// Starts the watcher and loads files ready for first build.
        /// </summary>
        public void Start()
        {
            // Add callback for when remote global file changes happen:
            if (GlobalFileMap != null)
            {
                GlobalFileMap.OnUpdated += async () =>
                {
                    await BuildAllCss();
                };
            }

            if (Prebuilt)
            {
                LoadPrebuiltUI();
                return;
            }

            if (!Directory.Exists(SourcePath))
            {
                return;
            }

            // Iterate through the directory tree of SourcePath and populate the initial map now.
            foreach (var filePath in Directory.EnumerateFiles(SourcePath, "*", new EnumerationOptions()
            {
                RecurseSubdirectories = true
            }))
            {
                AddToMap(filePath);
            }

            watcher = new FileSystemWatcher();
            watcher.Path = SourcePath;

            watcher.IncludeSubdirectories = true;

            watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite;

            // Add event handlers for the source files:
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.Error += new ErrorEventHandler(OnWatcherError);
            watcher.EnableRaisingEvents = true;

            // Load global SCSS files now.
            foreach (var kvp in FileMap)
            {
                var file = kvp.Value;
                if (file.FileType == SourceFileType.Scss && file.IsGlobal)
                {
                    // We've found a global file! Load its content now:
                    file.Content = File.ReadAllText(kvp.Key);
                }
            }
        }

        /// <summary>
        /// The watcher for this bundle
        /// </summary>
        private FileSystemWatcher watcher;

        private void OnWatcherError(object source, ErrorEventArgs e)
        {
            Log.Error("canvasrendererservice", e.GetException(), "UI watcher errored");
        }

        /// <summary>
        /// Updates the main js.
        /// </summary>
        private async ValueTask ConstructJs()
        {
            JsBuildErrors.Clear();

            // Result segments:
            var segments = GetSegments();

            if (ContainsStarterModule)
            {
                // Invoke start:
                segments.Add(new JavascriptFileSegment() { Source = "_rq('UI/Start').default();" });
            }

            BuiltJavascriptSegments = segments;

            // Clear cache:
            UpdateBuildTimestamp();
            _localeToMainJs = null;

            // JS file has updated - tell the world:
            await Events.FrontendjsAfterUpdate.Dispatch(new Context(), BuildTimestampMs);
        }

        /// <summary>
        /// Extract the segments from javascript source files
        /// </summary>
        /// <returns></returns>
        public List<JavascriptFileSegment> GetSegments()
        {
            // Result segments:
            var segments = new List<JavascriptFileSegment>();

            foreach (var fileEntry in FileMap)
            {
                var file = fileEntry.Value;

                if (file.FileType == SourceFileType.Javascript)
                {
                    if (file.Failure != null)
                    {
                        JsBuildErrors.Add(file.Failure);
                    }
                    else
                    {
                        if (file.Templates != null && file.Templates.Count > 0)
                        {
                            // This file has templates in it. Note that this array is _always_ sorted first to last (in source order), so substrings can be nice and fast.
                            var currentIndex = 0;

                            foreach (var template in file.Templates)
                            {
                                // Create segment which goes from currentIndex -> template.Start:
                                segments.Add(
                                    new JavascriptFileSegment()
                                    {
                                        Source = file.TranspiledContent.Substring(currentIndex, template.Start - currentIndex)
                                    }
                                );

                                // Add the template segment:
                                segments.Add(
                                    new JavascriptFileSegment()
                                    {
                                        Module = template.Module,
                                        TemplateLiteralToSearch = template.Original,
                                        TemplateLiteralSource = template.Target,
                                        VariableMap = template.VariableMap
                                    }
                                );

                                // Current index is now the end of the template:
                                currentIndex = template.End;
                            }

                            // Final segment:
                            segments.Add(
                                new JavascriptFileSegment()
                                {
                                    Source = file.TranspiledContent.Substring(currentIndex)
                                }
                            );

                        }
                        else
                        {
                            segments.Add(new JavascriptFileSegment() { Source = file.TranspiledContent });
                        }
                    }
                }
            }

            return segments;
        }

        /// <summary>
        /// Updates the main css.
        /// </summary>
        private async ValueTask ConstructCss()
        {
            var builder = new StringBuilder();
            CssBuildErrors.Clear();

            if (CssPrepend != null)
            {
                builder.Append(CssPrepend);
            }

            // Sorted files:
            var files = new List<SourceFile>();

            foreach (var file in FileMap)
            {
                if (file.Value.FileType == SourceFileType.Scss)
                {
                    if (file.Value.Failure != null)
                    {
                        CssBuildErrors.Add(file.Value.Failure);
                    }
                    else
                    {
                        files.Add(file.Value);
                    }
                }
            }

            // Sort the CSS files by priority:
            files = files.OrderBy(s => s.Priority).ToList();

            foreach (var file in files)
            {
                builder.Append(file.TranspiledContent);
            }

            BuiltCss = builder.ToString();
            UpdateBuildTimestamp();
            CssFile.FileContent = null;

            // CSS file has updated - tell the world:
            await Events.FrontendCssAfterUpdate.Dispatch(new Context(), BuildTimestampMs);
        }

        /// <summary>
        /// Maps a source URL for CSS files. Don't use this for outputting JS because urls there are not relative to the JS file location (whereas CSS is).
        /// </summary>
        /// <param name="sourcePath">E.g. "./images/test.jpg"</param>
        /// <param name="filePathParts">The filesystem path of the module the source file is in relative to the bundle Source folder.</param>
        /// <returns></returns>
        private string MapUrl(string sourcePath, string[] filePathParts)
        {
            if (sourcePath.StartsWith('.'))
            {
                // Relative filesystem path.
                var pathParts = sourcePath.Split('/');
                var builtPath = new List<string>();

                for (var i = 0; i < filePathParts.Length; i++)
                {
                    builtPath.Add(filePathParts[i]);
                }

                for (var i = 0; i < pathParts.Length; i++)
                {
                    var pathPart = pathParts[i];
                    if (pathPart == ".")
                    {
                        // Just ignore this
                    }
                    else if (pathPart == "..")
                    {
                        // Pop:
                        if (builtPath.Count < 1)
                        {
                            throw new Exception(
                                "The source path '" + sourcePath + "' in a css file is referring to a file outside the scope of the source directory. It was in " + string.Join('/', filePathParts)
                            );
                        }
                        builtPath.RemoveAt(builtPath.Count - 1);
                    }
                    else
                    {
                        builtPath.Add(pathPart);
                    }
                }

                return "./static/" + string.Join('/', builtPath);
            }

            // Unchanged otherwise as it's absolute.
            return sourcePath;
        }

        /// <summary>
        /// Peek char at index. If it is out of range, a nul byte is returned.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private char PeekString(string str, int index)
        {
            return index >= str.Length ? '\0' : str[index];
        }

        /// <summary>
        /// Remap the url() calls in the given css string.
        /// </summary>
        /// <returns></returns>
        public string RemapUrlsInCssAndRemoveComments(string css, string moduleFileSystemPath)
        {
            // moduleFileSystemPath Always starts with the bundle name, so pop that off:
            var fs = moduleFileSystemPath.IndexOf('/');

            if (fs != -1)
            {
                moduleFileSystemPath = moduleFileSystemPath.Substring(fs + 1);
            }

            var moduleFSPath = moduleFileSystemPath.Split('/');
            // node-sass can't do this bit for us unfortunately, so we'll have to do a simple state machine instead.
            var sb = new StringBuilder();
            var mode = 0;
            var urlStart = 0;

            for (var i = 0; i < css.Length; i++)
            {
                var ch = css[i];
                if (mode == 1)
                {
                    // Note that this strips comments as well (we could retain them though).

                    if (ch == '*' && PeekString(css, i + 1) == '/')
                    {
                        mode = 0;
                        i++;
                    }
                }
                else if (mode == 2)
                {
                    if (ch == ')')
                    {
                        // Terminated at i-1.
                        var completeUrlText = css.Substring(urlStart, i - urlStart).Trim();

                        if (completeUrlText.Length > 1 && completeUrlText[0] == '"')
                        {
                            completeUrlText = completeUrlText.Substring(1, completeUrlText.Length - 2);
                        }

                        // Remap it:
                        sb.Append("url(\"");
                        sb.Append(MapUrl(completeUrlText.Trim(), moduleFSPath));
                        sb.Append("\")");
                        mode = 0;
                    }
                }
                else if (ch == '/' && PeekString(css, i + 1) == '*')
                {
                    mode = 1;
                }
                else if (ch == 'u' && PeekString(css, i + 1) == 'r' && PeekString(css, i + 2) == 'l' && PeekString(css, i + 3) == '(') // Spaces are not permitted in the CSS spec before (
                {
                    // We're in a url(.. - mark the starting index:
                    urlStart = i + 4;
                    mode = 2;
                }
                else
                {

                    sb.Append(ch);
                }
            }


            return sb.ToString();
        }

        /// <summary>
        /// Public URL of the site. Originates from PublicUrl config setting.
        /// </summary>
        private string _publicPath;

        private FrontendCodeService _frontend;

		/// <summary>
		/// Fully qualifies the given url. It MUST always be absolute, i.e. starting with a /.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="localeId"></param>
		/// <returns></returns>
		private string FullyQualify(string url, uint localeId)
        {
            if (_publicPath == null)
            {
                var pubUrl = _frontend.GetPublicUrl(localeId);

                if (string.IsNullOrEmpty(pubUrl))
                {
                    _publicPath = "";
                }
                else
                {
                    if (pubUrl.EndsWith('/'))
                    {
                        pubUrl = pubUrl.Substring(0, pubUrl.Length - 1);
                    }

                    _publicPath = pubUrl;
                }

            }

            return _publicPath + url;
        }

        private void SetCssPath(uint localeId)
        {
            CssFile.PublicUrl = PackDir + "main.css?v=" + BuildTimestampMs + "&h=" + CssFile.Hash + "&lid=" + localeId;
            CssFile.FqPublicUrl = FullyQualify(CssFile.PublicUrl, localeId);
        }

        /// <summary>
        /// Updates the build timestamp. Only happens on dev instances whenever css/js file is updated. On production builds, this originates from the meta.json file in the output.
        /// </summary>
        private void UpdateBuildTimestamp()
        {
            BuildTimestampMs = (long)((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds);

            if (CssFile.FileContent != null)
            {
                SetCssPath(1);
            }

            if (_localeToMainJs != null)
            {
                for (var i = 0; i < _localeToMainJs.Length; i++)
                {
                    if (_localeToMainJs[i].FileContent != null)
                    {
                        var localeId = (uint)(i+1);

                        // Update its path:
                        _localeToMainJs[i].PublicUrl = PackDir + "main.js?v=" + BuildTimestampMs + "&h=" + _localeToMainJs[i].Hash + "&lid=" + localeId;
                        _localeToMainJs[i].FqPublicUrl = FullyQualify(_localeToMainJs[i].PublicUrl, localeId);
                    }
                }
            }

        }

        /// <summary>
        /// The output JS segments. It's in segments such that translations can be quickly swapped in on demand.
        /// </summary>
        public List<JavascriptFileSegment> BuiltJavascriptSegments;

        /// <summary>
        /// The output CSS.
        /// </summary>
        private string BuiltCss = "";

        /// <summary>
        /// The SCSS prefix - constructed of global files plus any bundle specific ones as well (e.g. functions or mixins exclusively for admin modules).
        /// </summary>
        private string ScssHeader;

        /// <summary>
        /// Approx line count in the scss header.
        /// </summary>
        private int ScssHeaderLineCount;

        /// <summary>
        /// Gets the SCSS globals.
        /// </summary>
        /// <returns></returns>
        public string GetScssGlobals()
        {
            return ScssHeader;
        }

        private void ConstructScssHeader()
        {
            var sb = new StringBuilder();

            foreach (var kvp in GlobalFileMap.SortedGlobalFiles)
            {
                sb.Append(kvp.Content);
                sb.Append('\n');
            }

            sb.Append('\n');
            var header = sb.ToString();
            sb.Clear();

            // Strip wasted bytes (comments and newlines) to improve scss compiler performance - 
            // unfortunately it can't cache the ast so it parses the header every time it compiles a scss change:
            var mode = 0;
            var lineCount = 0;

            for (var i = 0; i < header.Length; i++)
            {
                var ch = header[i];
                var more = i < header.Length - 1;

                if (mode == 1)
                {
                    if (ch == '*' && more && header[i + 1] == '/')
                    {
                        mode = 0;
                        i++;
                    }
                }
                else if (mode == 2)
                {
                    if (ch == '\r' || ch == '\n')
                    {
                        mode = 0;
                    }
                }
                else if (mode == 3)
                {
                    // 'string'
                    if (ch == '\\' && more && header[i + 1] == '\'')
                    {
                        // Escaped end quote
                        sb.Append(ch);
                        sb.Append('\'');
                        i++;
                    }
                    else if (ch == '\'')
                    {
                        // exited string
                        mode = 0;
                        sb.Append(ch);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else if (mode == 4)
                {
                    // "string"
                    if (ch == '\\' && more && header[i + 1] == '"')
                    {
                        // Escaped end quote
                        sb.Append(ch);
                        sb.Append('"');
                        i++;
                    }
                    else if (ch == '"')
                    {
                        // exited string
                        mode = 0;
                        sb.Append(ch);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else if (ch == '\'')
                {
                    mode = 3;
                    sb.Append(ch);
                }
                else if (ch == '\"')
                {
                    mode = 4;
                    sb.Append(ch);
                }
                else if (ch == '/' && more && header[i + 1] == '*')
                {
                    mode = 1;
                    i++;
                }
                else if (ch == '/' && more && header[i + 1] == '/')
                {
                    mode = 2;
                    i++;
                }
                else
                {
                    if (ch == '\n')
                    {
                        lineCount++;
                    }
                    sb.Append(ch);
                }
            }

            ScssHeaderLineCount = lineCount;

            ScssHeader = sb.ToString();
        }

        /// <summary>
        /// Builds the given SCSS file.
        /// </summary>
        /// <param name="file"></param>
        private void BuildScssFile(SourceFile file)
        {
            try
            {
                var rawContent = File.ReadAllText(file.Path);

                // Transform the SCSS now:
                var transpiledCss = BuildEngine.Invoke(
                    "transformScss",
                    rawContent,
					ScssHeader,
					file.Path,
                    Minified
                ) as string;

                // Convert URLs:
                transpiledCss = RemapUrlsInCssAndRemoveComments(transpiledCss, file.FullModulePath);

                // Apply transpiled content:
                file.Content = rawContent;
                file.TranspiledContent = transpiledCss;
                file.Failure = null;
            }
            catch (Microsoft.ClearScript.ScriptEngineException e)
            {
                var message = e.Message;
                string title;
                string description;

                if (string.IsNullOrEmpty(message))
                {
                    title = "Unknown exception";
                    description = e.ToString();
                }
                else
                {
                    var titleEnd = e.Message.IndexOf('\n');

                    if (titleEnd != -1)
                    {
                        title = e.Message.Substring(0, titleEnd);
                        description = e.Message.Substring(titleEnd + 1);

                        if (!string.IsNullOrEmpty(ScssHeader))
                        {
                            description += "\n\n⚠ Line numbers off by ~" + ScssHeaderLineCount + " (the length of your globals)";
                        }

                    }
                    else
                    {
                        title = "Unknown exception";
                        description = e.Message;
                    }
                }

                file.TranspiledContent = "";
                file.Failure = new UIBuildError()
                {
                    File = file.Path,
                    Title = title,
                    Description = description
                };
            }
            catch (Exception e)
            {
                file.TranspiledContent = "";
                file.Failure = new UIBuildError()
                {
                    File = file.Path,
                    Title = e.Message,
                    Description = e.ToString()
                };
            }
        }

        /// <summary>
        /// Builds the given JS file.
        /// </summary>
        /// <param name="file"></param>
        private void BuildJsFile(SourceFile file)
        {

            try
            {
                // Transform it now (developers only here - we only care about ES8, i.e. minimal changes to source/ react only):
                var es8JavascriptResult = BuildEngine.Invoke(
                    "transformES8",
                    File.ReadAllText(file.Path),
                    file.ModulePath, // Module path
                    file.FullModulePath,
                    TransformOptions
                ) as ScriptObject;

                // Get the src:
                var es8Javascript = es8JavascriptResult["src"] as string;

                var literals = es8JavascriptResult["templateLiterals"] as System.Collections.IList;

                if (literals != null)
                {
                    List<TemplateLiteral> templates = null;

                    foreach (var literalRaw in literals)
                    {
                        var literal = literalRaw as ScriptObject;
                        var original = literal["original"] as string;
                        var target = literal["target"] as string;
                        var start = (int)literal["start"];
                        var end = (int)literal["end"];
                        var expressions = literal["expressions"] as System.Collections.IList;

                        if (templates == null)
                        {
                            templates = new List<TemplateLiteral>();
                        }

                        // variable map:
                        Dictionary<string, string> varMap = null;

                        if (expressions != null && expressions.Count > 0)
                        {
                            // has at least 1 expression in it. Create var map:
                            varMap = new Dictionary<string, string>();

                            foreach (var exprRaw in expressions)
                            {
                                var expr = exprRaw as ScriptObject;
                                var from = expr["from"] as string;
                                var to = expr["to"] as string;

                                if (from != null && to != null)
                                {
                                    varMap[from] = to;
                                }
                            }
                        }

                        templates.Add(new TemplateLiteral()
                        {
                            Module = file.ModulePath,
                            Target = target,
                            Original = original,
                            Start = start,
                            End = end,
                            VariableMap = varMap
                        });
                    }

                    file.Templates = templates;
                }

                // Apply transpiled content:
                file.TranspiledContent = es8Javascript;
                file.Failure = null;
            }
            catch (Microsoft.ClearScript.ScriptEngineException e)
            {
                var message = e.Message;
                string title;
                string description;

                if (string.IsNullOrEmpty(message))
                {
                    title = "Unknown exception";
                    description = e.ToString();
                }
                else
                {
                    var titleEnd = e.Message.IndexOf('\n');

                    if (titleEnd != -1)
                    {
                        title = e.Message.Substring(0, titleEnd);
                        description = e.Message.Substring(titleEnd + 1);
                    }
                    else
                    {
                        title = "Unknown exception";
                        description = e.Message;
                    }
                }

                file.TranspiledContent = "";
                file.Failure = new UIBuildError()
                {
                    File = file.Path,
                    Title = title,
                    Description = description
                };
            }
            catch (Exception e)
            {
                file.TranspiledContent = "";
                file.Failure = new UIBuildError()
                {
                    File = file.Path,
                    Title = e.Message,
                    Description = e.ToString()
                };
            }
        }

        /// <summary>
        /// A full compile of all the CSS.
        /// </summary>
        public async ValueTask BuildAllCss()
        {
            ConstructScssHeader();

            // Handle the initial compile of each file.
            foreach (var kvp in FileMap)
            {
                var file = kvp.Value;

                if (file.FileType == SourceFileType.Scss && !file.IsGlobal)
                {
                    BuildScssFile(file);
                }

            }

            // Construct the css file:
            await ConstructCss();
        }

        /// <summary>
        /// Load default module translations
        /// </summary>
        public List<Translation> LoadModuleTranslations(Locale locale)
        {
            List<Translation> translations = null;

            foreach (var file in FileMap
                .Select(f => f.Value)
                .Where(f =>
                    f.FileType == SourceFileType.Locale &&
                    f.FileName.Equals($"locale.{locale.Code}.json", StringComparison.InvariantCultureIgnoreCase)))
            {
                try
                {
                    var rawContent = File.ReadAllText(file.Path);
                    var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(rawContent);
                    if (values != null && values.Any())
                    {
                        foreach (var value in values)
                        {
                            if (translations == null)
                            {
                                translations = new List<Translation>();
                            }

                            translations.Add(new Translation()
                            {
                                Module = file.ModulePath.Replace($"/{file.FileName}", string.Empty),
                                Original = value.Key,
                                Translated = value.Value,
                                IsDraft = false
                            });
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log.Error("canvasrendererservice", ex, "Failed loading translations");
                }
            }

            return translations;
        }

        private static async ValueTask<string> WaitUntilAvailable(string path)
        {
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(path);
                    return content;
                }
                catch (Exception)
                {
                    await Task.Delay(50);
                }
            }
 
            // If it fell down here, we can assume a
            // permanent filesystem failure has occurred.
            // Try reading one last time to emit the actual exception outward:
            return File.ReadAllText(path);
		}


        /// <summary>
        /// A full compile of everything.
        /// </summary>
        public async ValueTask BuildEverything()
        {
            ConstructScssHeader();

            // Handle the initial compile of each file.
            foreach (var kvp in FileMap)
            {
                var file = kvp.Value;

                if (file.FileType == SourceFileType.Javascript)
                {
                    // do a check here make sure the file is writeable, await it being writeable
                    // set to key, shou;d've been that initially, didn't commit tho
                    await WaitUntilAvailable(kvp.Key);  

                    BuildJsFile(file);
                }
                else if (file.FileType == SourceFileType.Scss && !file.IsGlobal)
                {
                    // do a check here make sure the file is writeable, await it being writeable
                    // set to key, shou;d've been that initially, didn't commit tho
                    await WaitUntilAvailable(kvp.Key);

                    BuildScssFile(file);
                }
            }

            // Construct the js/ css file:
            await ConstructJs();
            await ConstructCss();
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                lock (fsUpdateLock)
                {
                    if (e.ChangeType == WatcherChangeTypes.Deleted)
                    {
                        FilesystemRequestUpdate(new FilesystemChange()
                        {
                            AbsolutePath = e.FullPath,
                            ChangeType = SourceFileChangeType.Deleted
                        });
                    }
                    else if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
                    {
                        FilesystemRequestUpdate(new FilesystemChange()
                        {
                            AbsolutePath = e.FullPath,
                            ChangeType = SourceFileChangeType.Changed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("canvasrendererservice", ex, "File system update failed");
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                lock (fsUpdateLock)
                {
                    FilesystemRequestUpdate(new FilesystemChange()
                    {
                        AbsolutePath = e.OldFullPath,
                        ChangeType = SourceFileChangeType.Deleted
                    });

                    FilesystemRequestUpdate(new FilesystemChange()
                    {
                        AbsolutePath = e.FullPath,
                        ChangeType = SourceFileChangeType.Changed
                    });
                }
            }
            catch (Exception ex)
            {
				Log.Error("canvasrendererservice", ex, "File system update failed during a rename");
			}
        }

        /// <summary>
        /// Pending filesys changes.
        /// </summary>
        private List<FilesystemChange> _pendingChanges;
        /// <summary>
        /// A timer for debouncing filesystem watch events.
        /// </summary>
        private System.Timers.Timer _fileSystemTimer;

        private object fsUpdateLock = new object();

        /// <summary>
        /// Requests an update. The filesystem will spam events for a moment 
        /// (particularly when code editors are storing a backup of every file they write out), so this method performs a debounce.
        /// </summary>
        public void FilesystemRequestUpdate(FilesystemChange change)
        {
            // Already got a change for this file?
            var exists = false;

            if (_pendingChanges != null)
            {
                foreach (var chg in _pendingChanges)
                {
                    if (chg.AbsolutePath == change.AbsolutePath)
                    {
                        // Only need to track the latest event for each file.
                        chg.ChangeType = change.ChangeType;
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
            {
                if (_pendingChanges == null)
                {
                    _pendingChanges = new List<FilesystemChange>();
                }

                _pendingChanges.Add(change);
            }

            if (_fileSystemTimer != null)
            {
                return;
            }

            // Delay for 100ms:
            _fileSystemTimer = new System.Timers.Timer(100)
            {
                AutoReset = false
            };

            _fileSystemTimer.Elapsed += async (timerElapsedSender, timerElapsedArgs) =>
            {
                _fileSystemTimer = null;
                var pending = _pendingChanges;
                _pendingChanges = null;

                if (pending == null)
                {
                    return;
                }

                var jsUpdate = false;
                var mapChange = false;
                var cssUpdate = false;
                var fullCssUpdate = false;

                foreach (var change in pending)
                {
                    // Commit the change to our in-memory model.
                    var path = change.AbsolutePath;
                    var existed = false;

                    // Remove from the map always:
                    if (FileMap.TryRemove(path, out SourceFile file))
                    {
                        existed = true;

                        if (file.FileType == SourceFileType.Javascript)
                        {
                            jsUpdate = true;
                        }

                        if (file.FileType == SourceFileType.Scss)
                        {
                            cssUpdate = true;

                            if (file.IsGlobal)
                            {
                                // Remove from global map:
                                if (GlobalFileMap.FileMap.Remove(file.Path))
                                {
                                    fullCssUpdate = true;
                                }
                            }
                        }

                        if (change.ChangeType == SourceFileChangeType.Deleted)
                        {
                            mapChange = true;
                        }
                    }

                    if (change.ChangeType == SourceFileChangeType.Changed)
                    {
                        // Add it back in:
                        file = AddToMap(change.AbsolutePath);

                        if (file == null)
                        {
                            continue;
                        }

                        if (!existed)
                        {
                            mapChange = true;
                        }

                        if (file.FileType == SourceFileType.Javascript)
                        {
                            jsUpdate = true;
                        }

                        if (file.FileType == SourceFileType.Scss)
                        {
                            cssUpdate = true;
                        }

                        // Build it:
                        if (file.FileType == SourceFileType.Javascript)
                        {
                            BuildJsFile(file);
                        }
                        else if (file.FileType == SourceFileType.Scss && !file.IsGlobal)
                        {
                            BuildScssFile(file);
                        }
                    }

                    if (file != null && file.IsGlobal)
                    {
                        // A global file changed. Must now rebuild all SCSS (but there might be some other changes, so wait).
                        Log.Info(
                            "frontendcodeservice",
                            "A slow global SCSS change happened which takes a little longer to process. We'll let you know when it's done. "+
                            "Tip: Put a mixin, function etc you're working on in a non-global file just whilst you build it out."
                        );

                        // Update content:
                        file.Content = File.ReadAllText(file.Path);

                        // Add to GSM:
                        GlobalFileMap.FileMap[file.Path] = file;

                        // Complete CSS update occurring:
                        fullCssUpdate = true;
                    }
                }

                if (mapChange)
                {
                    // The map either gained a new file or lost one.
                    OnMapChange?.Invoke();
                }

                if (jsUpdate)
                {
                    await ConstructJs();
                }

                if (fullCssUpdate)
                {
                    // Sort the GSM:
                    GlobalFileMap.Sort();

                    // Tell bundles they need to rebuild (this includes telling this one anyway):
                    await GlobalFileMap.HasChanged();
                }
                else if (cssUpdate)
                {
                    await ConstructCss();
                }

                // Now (local time, because this is for developers).
                Log.Ok("frontendcodeservice", "Done handling UI changes.");

                // Trigger event:
                await Events.FrontendAfterUpdate.Dispatch(new Context(), BuildTimestampMs);
            };
            _fileSystemTimer.Start();
        }

    }

    /// <summary>
    /// Tracks a change on a source file. We don't specifically separate changed from 
    /// created as they can often be actually triggered at the same time by code IDE's.
    /// </summary>
    public enum SourceFileChangeType
    {
        /// <summary>
        /// File changed or created
        /// </summary>
        Changed,
        /// <summary>
        /// File removed
        /// </summary>
        Deleted
    }

    /// <summary>
    /// Source file type.
    /// </summary>
    public enum SourceFileType
    {
        /// <summary>
        /// Not a source file we care about.
        /// </summary>
        None,
        /// <summary>
        /// JS source.
        /// </summary>
        Javascript,
        /// <summary>
        /// Scss source.
        /// </summary>
        Scss,
        /// <summary>
        /// module.json meta file.
        /// </summary>
        ModuleMeta,
        /// <summary>
        /// locale.json - the localisation template.
        /// </summary>
        LocaleTemplate,
        /// <summary>
        /// Locale for a particular country.
        /// </summary>
        Locale,
        /// <summary>
        /// Not a source file we care about.
        /// </summary>
        Directory
    }

    /// <summary>
    /// Global source file map.
    /// </summary>
    public class GlobalSourceFileMap
    {
        /// <summary>
        /// The file map of global files.
        /// </summary>
        public Dictionary<string, SourceFile> FileMap = new Dictionary<string, SourceFile>();

        /// <summary>
        /// Global files sorted by order of priority.
        /// </summary>
        public List<SourceFile> SortedGlobalFiles = new List<SourceFile>();

        /// <summary>
        /// Called when the GSM has been updated.
        /// </summary>
        public event Func<ValueTask> OnUpdated;

        /// <summary>
        /// Indicate the GSM has changed.
        /// </summary>
        /// <returns></returns>
        public async ValueTask HasChanged()
        {
            await OnUpdated();
        }

        /// <summary>
        /// Reconstructs sorted global files based on the map.
        /// </summary>
        public void Sort()
        {
            SortedGlobalFiles = FileMap.Values.OrderBy(s => s.Priority).ToList();
        }
    }

    /// <summary>
    /// A particular src file.
    /// </summary>
    public class SourceFile
    {
        /// <summary>
        /// Sort order of SCSS files.
        /// </summary>
        public int Priority = 100;
        /// <summary>
        /// Same as the key in the FileMap.
        /// </summary>
        public string Path;
        /// <summary>
        /// Name of file incl type.
        /// </summary>
        public string FileName;
        /// <summary>
        /// Module path for this file. It's essentially the path but always uses / and never contains "ThirdParty" or bundles.
        /// </summary>
        public string ModulePath;
        /// <summary>
        /// Module path for this file. It's essentially the path but always uses /. This one does contain ThirdParty and bundles, however.
        /// </summary>
        public string FullModulePath;
        /// <summary>
        /// Tidy file type.
        /// </summary>
        public SourceFileType FileType;
        /// <summary>
        /// True if this file is a "global" one. Only true for SCSS files with .global. in their filename.
        /// </summary>
        public bool IsGlobal;
        /// <summary>
        /// Lowercase filetype. "js", "jsx" etc.
        /// </summary>
        public string RawFileType;
        /// <summary>
        /// Relative to the Source directory in the parent builder.
        /// </summary>
        public string RelativePath;
        /// <summary>
        /// True if this file is a ThirdParty one.
        /// </summary>
        public bool ThirdParty;
        /// <summary>
        /// Raw file content, set once loaded.
        /// </summary>
        public string Content;
        /// <summary>
        /// The contents of this file, transpiled. If it's a format that doesn't require transpiling, then this is null.
        /// </summary>
        public string TranspiledContent;
        /// <summary>
        /// Set if this file failed to build.
        /// </summary>
        public UIBuildError Failure;
        /// <summary>
        /// Any template literals in this JS file.
        /// </summary>
        public List<TemplateLiteral> Templates;
    }

    /// <summary>
    /// A change to the filesystem.
    /// </summary>
    public class FilesystemChange
    {
        /// <summary>
        /// The file path.
        /// </summary>
        public string AbsolutePath;
        /// <summary>
        /// Deleted or updated.
        /// </summary>
        public SourceFileChangeType ChangeType;
    }

    /// <summary>
    /// Information about a template literal.
    /// </summary>
    public struct TemplateLiteral
    {
        /// <summary>
        /// Module name for this template literal. E.g. "UI/Thing".
        /// </summary>
        public string Module;
        /// <summary>
        /// Original template literal.
        /// </summary>
        public string Original;
        /// <summary>
        /// The target template literal. This is only different from original if the file was minified.
        /// </summary>
        public string Target;
        /// <summary>
        /// Start index.
        /// </summary>
        public int Start;
        /// <summary>
        /// End index.
        /// </summary>
        public int End;
        /// <summary>
        /// If the template literal has variables in it, this maps original variables to target ones. It's null otherwise.
        /// This exists whenever a template literal has variables in it, even if it maps a->a (non-minified source), 
        /// because it verifies if the variables even exist to avoid outputting JS with syntax errors in the event that a translator typoed.
        /// </summary>
        public Dictionary<string, string> VariableMap;
    }

    /// <summary>
    /// A segment of the javascript file.
    /// </summary>
    public struct JavascriptFileSegment
    {
        /// <summary>
        /// If not null, this is just a raw source string. It's added to output as-is.
        /// </summary>
        public string Source;

        /// <summary>
        /// Module that this template literal is from.
        /// </summary>
        public string Module;

        /// <summary>
        /// The raw source of the template literal to use, if this is a template literal substitution segment.
        /// Look up this template literal in the list of translations.
        /// Once a match is found, check if VariableMap is set.
        /// If so, you MUST remap variables using the map before outputting.
        /// </summary>
        public string TemplateLiteralToSearch;

        /// <summary>
        /// The raw template literal in the source. May be minified. Output this if no match was found.
        /// </summary>
        public string TemplateLiteralSource;

        /// <summary>
        /// If the template literal has variables in it, this maps original variables to target ones.
        /// This exists whenever a template literal has variables in it, even if it maps a->a (non-minified source), 
        /// because it verifies if the variables even exist to avoid outputting JS with syntax errors in the event that a translator typoed.
        /// </summary>
        public Dictionary<string, string> VariableMap;
    }
}