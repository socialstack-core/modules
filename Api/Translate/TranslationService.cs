using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Contexts;
using Api.Eventing;
using Api.CanvasRenderer;
using Api.Startup;
using System;
using System.IO;
using Karambolo.PO;
using System.Linq;
using Api.Pages;

namespace Api.Translate
{
    /// <summary>
    /// Handles translations.
    /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
    /// </summary>
    public partial class TranslationService : AutoService<Translation>
    {
        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public TranslationService() : base(Events.Translation)
        {
            // Always cache by default:
            Cache();

            // The above is such that translations are always content-synced, 
            // which is important as they are internally cached in a variety of ways.

            // Example admin page install:
            InstallAdminPages(null, null, new string[] { "id", "module", "original", "translation" });

            Events.Service.AfterStart.AddEventListener((Context context, object sender) => {

                // This route is suggested rather than dependency injection
                // Because some projects (particularly fully headless and micro instances) don't have a page service installed.
                var pageService = Services.Get<PageService>();

                pageService.Install(
                    new Page()
                    {
                        Url = "/en-admin/translation/upload",
                        Title = "Translation Upload",
                        BodyJson = @"{
	                            ""c"": {
		                            ""t"": ""Admin/Layouts/Default"",
		                            ""d"": {},
		                            ""c"": {
			                            ""t"": ""Admin/Tile"",
			                            ""d"": {},
			                            ""c"": {
				                            ""t"": ""Admin/TranslationUpload"",
				                            ""d"": {},
				                            ""i"": 2
			                            },
			                            ""i"": 3
		                            },
		                            ""i"": 4
	                            },
	                            ""i"": 5
                            }"
                    });

                return new ValueTask<object>(sender);
            });

        }

        /// <summary>
        /// Extract any default translations from the modules and locale.xx.json files 
        /// </summary>
        public async ValueTask<bool> LoadDefaultTranslations()
        {
            var localeService = Services.Get<LocaleService>();

            // user the UI builder to hunt out any locale files in UI modules
            var globalMap = new GlobalSourceFileMap();

            // Create a group of build/watchers for each bundle of files (all in parallel):
            var builder = new UIBundle("UI", "/pack/", this, localeService, null, null, globalMap, false);

            builder.Start();

            // Sort global map:
            globalMap.Sort();

            // process the modules for each locale
            var localeList = await localeService.Where(DataOptions.IgnorePermissions).ListAll(new Context());

            foreach (var locale in localeList)
            {
                Console.WriteLine($"Extract default translations for {locale.Id}-{locale.Name}");
                await builder.PopulateDefaultTranslations(locale.Id);
            }

            return true;
        }

        public async ValueTask<bool> LoadPotFiles(Context context)
        {
            var sourcePath = Path.GetFullPath("Translations");

            HashSet<string> potFiles = new HashSet<string>();

            // Iterate through the directory tree of SourcePath and populate the initial map now.
            foreach (var filePath in Directory.EnumerateFiles(sourcePath, "*.po", new EnumerationOptions()
            {
                RecurseSubdirectories = false
            }))
            {
                potFiles.Add(filePath);
            }

            if (!potFiles.Any())
            {
                return false;
            }

            foreach (var potfile in potFiles)
            {
                using (FileStream fsSource = new FileStream(potfile, FileMode.Open, FileAccess.Read))
                {
                    var parsedPO = await ParsePOData(context, fsSource, ServicedType);

                    Console.WriteLine($"Procesing po file {potfile} {context.LocaleId}");

                    // Load all the local database translations
                    var translationList = await Where(DataOptions.IgnorePermissions).ListAll(new Context()
                    {
                        LocaleId = context.LocaleId
                    });

                    // For each translation in the pot file, we'll look for translations which specifically target this content type in their msgctxt.
                    foreach (var po in parsedPO)
                    {
                        // Lookup the entry by its ID:
                        var itemId = ConvertId(po.Id);
                        var translations = translationList.Where(f => f.Id == itemId);

                        if (translations == null || !translations.Any())
                        {
                            Console.WriteLine($"Missing translation [{itemId}] !! [{po.Original}->{po.Translated}] looking for entries based on Original text");

                            // Not found by its Id so do we have any where the source text matches ? 
                            translations = translationList.Where(f => f.Original == po.Original);
                        }

                        if (translations != null)
                        {
                            foreach(var translation in translations)
                            {
                                if (translation.Translated != po.Translated) {

                                    Console.WriteLine($"Updating translation [{translation.Id}] {translation.Module} {translation.Original}->{po.Translated}");

                                    await Update(context, translation, (Context ctx, Translation trans, Translation orig) =>
                                    {
                                        trans.Translated = po.Translated;
                                    }, DataOptions.IgnorePermissions);
                                }

                            }
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Parse and load PO language file data
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        /// <exception cref="PublicException"></exception>
        public async ValueTask<List<PoTranslation>> ParsePOData(Context context, Stream stream, Type serviceType)
        {

            var service = Services.GetByContentType(serviceType);
            if (service == null)
            {
                // The supplied service is invalid
                throw new PublicException("Unknown service type '" + serviceType.ToString(), "Invalid service");
            }

            List<PoTranslation> poTranslations = new List<PoTranslation>();

            var poParser = new POParser();
            POParseResult parsedPO = poParser.Parse(stream);
            if (!parsedPO.Success)
            {
                // The supplied PO is dodgy!
                throw new PublicException(parsedPO.Diagnostics.ToString(), "invalid_po");
            }

            // Locale code is..
            var localeCode = parsedPO.Catalog.Language.ToLower();

            // Get the locale with that code:
            var localeService = Services.Get<LocaleService>();
            var localeId = await localeService.GetId(localeCode);
            if (!localeId.HasValue)
            {
                throw new PublicException("Unknown locale code '" + localeCode + "' in your pot file", "invalid_locale");
            }

            // Get the locale:
            var locale = await localeService.Get(context, localeId.Value);
            if (locale == null)
            {
                // Internal fault
                throw new PublicException(
                    "Faulted locale code in the pot file. The locale ID was found (" + localeId.Value + "), but the locale itself appears to have been removed.",
                    "invalid_locale"
                );
            }

            // We'll now switch the context to the locale of the pot file:
            context.LocaleId = locale.Id;

            // Pre-process the translations
            foreach (var poTranslation in parsedPO.Catalog)
            {
                var key = poTranslation.Key;
                // Context and msgid required
                if (key.ContextId == null || key.Id == null)
                {
                    continue;
                }

                // no translated value so skip 
                if (key.Id == poTranslation[0])
                {
                    continue;
                }

                //split out the key components
                var index = key.ContextId.IndexOf('>');
                if (index == -1)
                {
                    continue;
                }

                var parts = key.ContextId.Split('>');
                // Parts must be length of 3, as it is contentType>id>field
                if (parts.Length != 3)
                {
                    continue;
                }

                var contentType = parts[0];
                var contentId = parts[1];
                var contentField = parts[2];

                // Filter out entries which aren't for this content type.
                if (contentType.ToLower() != service.EntityName.ToLower())
                {
                    // Skip
                    continue;
                }

                if (!ulong.TryParse(contentId, out var uId) || uId == 0)
                {
                    // Skip - bad ID
                    continue;
                }

                // TranslatedValue could be textual, numeric etc - it's unknown.
                poTranslations.Add(new PoTranslation()
                {
                    ContentType = contentType,
                    ContentId = contentId,
                    Id = uId,
                    FieldName = contentField,
                    Original = key.Id,
                    Translated = poTranslation[0]
                });
            }

            return poTranslations;
        }


    }

}
