using Api.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Karambolo.PO;
using Api.Configuration;
using System.Linq;
using Api.Contexts;
using Api.Eventing;

namespace Api.Translate
{
	/// <summary>
	/// Handles multiple locales for text on the UI and in the database.
	/// Uses the common PO/ POT formats for interchanging text.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public class TranslateService : ITranslateService
	{
		/// <summary>
		/// The default site locale ID.
		/// </summary>
		public const int DefaultLocale = 1;
		
		private IDatabaseService _database;
		private readonly Query<Translation> deleteByContentQuery;
		private readonly Query<Translation> deleteQuery;
		private readonly Query<Translation> createQuery;
		private readonly Query<Translation> selectQuery;
		private readonly Query<Locale> selectLocaleByCodeQuery;
		private readonly Query<Translation> listByContentQuery;
		private readonly Query<Translation> listByLocaleQuery;
		private readonly Query<Locale> listLocaleQuery;
		private readonly Query<Locale> selectLocaleQuery;
		private readonly Query<Translation> updateQuery;
		private readonly Query<Translation> replaceQuery;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public TranslateService(IDatabaseService database)
        {
            _database = database;
			deleteQuery = Query.Delete<Translation>();
			createQuery = Query.Insert<Translation>();
			updateQuery = Query.Update<Translation>();
			selectQuery = Query.Select<Translation>();
			selectLocaleQuery = Query.Select<Locale>();
			listLocaleQuery = Query.Select<Locale>();
			replaceQuery = Query.Replace<Translation>();

			deleteByContentQuery = Query.Delete<Translation>();
			deleteByContentQuery.Where().EqualsArg("ContentTypeId", 0).And().EqualsArg("ContentId", 1);
			
			selectLocaleByCodeQuery = Query.Select<Locale>();
			selectLocaleByCodeQuery.Where().EqualsArg("Code", 0);
			
			listByContentQuery = Query.Select<Translation>();
			listByContentQuery.Where().EqualsArg("ContentTypeId", 0).And().Equals("ContentId", 1);

			listByLocaleQuery = Query.Select<Translation>();
			listByLocaleQuery.Where().EqualsArg("Locale", 0);
		}

        /// <summary>
        /// Deletes a single translation by its ID.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Delete(int entryId)
        {
            return await _database.Run(deleteQuery, entryId);
		}

        /// <summary>
        /// Gets a set of translations for a particular piece of content.
        /// Indexed by locale, then by key.
        /// </summary>
        /// <param name="contentGroup">
        /// The content group - e.g. "products".
        /// </param>
        /// <param name="contentId">
        /// The content ID - that's e.g. the product ID.
        /// </param>
        /// <returns></returns>
        public async Task<Translations> GetForContent(string contentGroup, int contentId)
        {
            var translationList = await _database.List(listByContentQuery, null, contentGroup, contentId);

            // Next just return this as a translations set:
            return new Translations(translationList, 0);
        }

        /// <summary>
        /// Parses a PO from its text to a set of translations.
        /// </summary>
        /// <param name="po"></param>
        /// <returns></returns>
        public async Task<Translations> ParsePO(Stream po)
        {
            var poParser = new POParser();
            POParseResult parsedPO = null;
            try
            {
                parsedPO = poParser.Parse(po);
            }catch(Exception e)
            {
                // The parser can straight crash - the "Success" field isn't entirely reliable.
                Console.Write(e);
                return null;
            }

            if (!parsedPO.Success) {
                // The supplied PO is dodgy.
                Console.Write(parsedPO.Diagnostics);
                return null;
            }

            // Create result set:
            var result = new Translations();
            
            // Locale is..
            var localeCode = parsedPO.Catalog.Language;

			var locale = await GetLocaleEntry(localeCode);

            foreach (var poTranslation in parsedPO.Catalog)
            {
                var key = poTranslation.Key;

                if (key == null || key.ContextId == null || key.Id == null)
                {
                    continue;
                }

                var translation = new Translation();
                translation.Locale = locale.Id;
                translation.Key = key.Id;
                
                // Actual translation text is just the first value 
                // (because sometimes they have plural variants - we don't use those at the moment):
                translation.Html = poTranslation[0];

				// ContextId is of the form ContentTypeName>ContentId>Key  e.g. "products>2>name"
				// It's just ContentTypeName if there's no ContentId.

				var index = key.ContextId.IndexOf('>');

                if (index != -1)
                {
					var parts = key.ContextId.Split('>');

                    translation.ContentTypeId = ContentTypes.GetId(parts[0].ToLower());
                    
                    // Set the content ID:
                    if (int.TryParse(parts[1], out int contentId))
                    {
                        translation.ContentId = contentId;
                    }

					if (parts.Length >= 3)
					{
						// Overwrite the key (ContentTypeName>ContentId>Key e.g. "product>2>name"):
						translation.Key = parts[2];
					}

				}
				else
                {
					// It's just the content type (its *name*):
					var id = ContentTypes.GetId(key.ContextId.ToLower());
					translation.ContentTypeId = id;
                }

                // Alright - now add the translation to the set:
                result.Add(translation);

            }

            return result;
        }

        /// <summary>
        /// Generates a POT file using en_US as the primary locale.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetPot()
        {
            // Get all en_US translations:
            var translationList = await _database.List(listByLocaleQuery, null, DefaultLocale);
            
            // Create translation set:
            var set = new Translations(translationList, DefaultLocale);

            // Convert to pot:
            return set.ToPot();
        }

        /// <summary>
        /// Generates a PO file for the given locale using en_US as the primary locale.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetPo(int localeId)
        {
            // Create translation set (en_US):
            var usSet = await GetLocale(DefaultLocale);

            // Create translation set (target locale):
            var targetSet = await GetLocale(localeId);

            // Generate the PO:
            return usSet.ToPo(targetSet);
        }

		/// <summary>
		/// Gets a given content group from the given locale - e.g. get all product translations.
		/// Then groups the result by the content ID (i.e. such that it's grouped by product ID).
		/// </summary>
		/// <param name="localeId"></param>
		/// <param name="contentTypeId"></param>
		/// <returns></returns>
		public async Task<TranslationLookup<int>> Get(int localeId, int contentTypeId)
		{
			var locale = await GetLocale(localeId);
			var group = locale.GetGroup(contentTypeId);
			return group.GroupByContentId();
		}

		/// <summary>
		/// Gets a given content group from the given locale - e.g. get all product translations.
		/// Then groups the result by the content ID (i.e. such that it's grouped by product ID).
		/// </summary>
		/// <param name="context"></param>
		/// <param name="translationId"></param>
		/// <returns></returns>
		public async Task<Translation> Get(Context context, int translationId)
		{
			return await _database.Select(selectQuery, translationId);
		}

		/// <summary>
		/// Does the same as GetLocale only if the locale name is the DefaultLocale ("en_US") then null is returned instead.
		/// </summary>
		/// <param name="locale"></param>
		/// <returns></returns>
		public async Task<Translations> GetLocaleNotDefault(int locale)
		{
			if (locale == DefaultLocale)
			{
				return null;
			}

			return await GetLocale(locale);
		}

		/// <summary>
		/// Get a locale entry by its name (e.g. en_US).
		/// </summary>
		/// <param name="locale"></param>
		/// <returns></returns>
		public async Task<Locale> GetLocaleEntry(string locale)
		{
			if (string.IsNullOrEmpty(locale))
			{
				// Nope!
				return null;
			}

			return await _database.Select(selectLocaleByCodeQuery, locale);
		}

		/// <summary>
		/// Get a locale entry by its ID.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		public async Task<Locale> GetLocaleEntry(int localeId)
		{
			return await _database.Select(selectLocaleQuery, localeId);
		}

		/// <summary>
		/// Gets all translations for a given locale. Adding caching to this would be an easy win.
		/// </summary>
		/// <param name="locale"></param>
		/// <returns></returns>
		public async Task<Translations> GetLocale(int locale)
        {
            var transList = await _database.List(listByLocaleQuery, null, locale);

			return new Translations(transList, locale);
        }

		/// <summary>
		/// Rebuilds the JSON file for a particular locale (they're used by the frontend).
		/// Use this if you've directly updated the database and want to pump out a new JSON file.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		public async Task<Translations> UpdateJson(int localeId)
        {
            // Get locale from the DB:
            var locale = await GetLocale(localeId);

            // And save the JSON now:
            locale.SaveJson(AppSettings.Configuration["Content"] + "/languages/" + localeId + ".json");

            return locale;
        }

		/// <summary>
		/// Gets a set of translations for one or more entire content groups.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		public async Task<TranslationLookup<int>> GetByContentGroup(int localeId)
		{
			// Get locale from the DB:
			var locale = await GetLocale(localeId);
			
			return locale.GroupByContentType();
		}

		/// <summary>
		/// Updates the translations for a particular locale with the given set. Optionally deletes ones that don't exist.
		/// </summary>
		/// <param name="translations">The latest translation set.</param>
		/// <param name="localeId">The locale to update</param>
		/// <param name="deleteIfDoesntExist">True if you want to delete translations from 
		/// the database that aren't in this translation set for this locale.</param>
		/// <returns></returns>
		public async Task<DiffSet<Translation>> Update(Translations translations, int localeId, bool deleteIfDoesntExist)
        {
            // Get existing locale from the DB:
            var locale = await GetLocale(localeId);

            // Diff:
            var translationDiff = locale.Diff(translations);

            // Note:
            // - translationDiff.Updated and translationDiff.Deleted both contain Translation objects with row IDs. 
            //   We'll use these IDs to update things quickly.

            // The overhead of this is always at most 3 queries.

            int changeCount = translationDiff.Changed.Count;
            int removeCount = translationDiff.Removed.Count;
            int addedCount = translationDiff.Added.Count;

            // First, handle changed rows:

            if (changeCount > 0)
            {
				await _database.Run(replaceQuery, translationDiff.Changed);
            }

            // Next, handle any new rows:
            if (addedCount > 0)
            {
                await _database.Run(createQuery, translationDiff.Added);
            }

            // Finally, handle any rows that were removed:

            if (deleteIfDoesntExist && removeCount > 0)
            {
				// Run it now:
				await _database.Run(deleteQuery, translationDiff.Removed.Select(t => t.Id));
            }

            // Apply the diff to the locale such that locale ends up with the latest set of translations:
            locale.Apply(translationDiff);

            // And output the locale as a JSON file into the content directory so it can be used directly by the frontend:
            locale.SaveJson(AppSettings.Configuration["Content"] + "/languages/" + localeId + ".json");

            return translationDiff;
        }

        /// <summary>
        /// Gets a list of locales.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Locale>> GetLocales()
        {
            return await _database.List(listLocaleQuery, null);
        }

		/// <summary>
		/// Deletes all translations related to a partiuclar piece of content.
		/// </summary>
		/// <param name="contentTypeId">The group the content is in. E.g. "products".</param>
		/// <param name="contentId">The ID of the content - e.g. a product ID.</param>
		/// <returns></returns>
		public async Task<bool> Delete(int contentTypeId, int contentId)
        {
			return await _database.Run(deleteByContentQuery, contentTypeId, contentId);
        }

		/// <summary>
		/// Updates the content for a particular translation.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="translation">The translation to update.</param>
		/// <returns></returns>
		public async Task<Translation> Create(Context context, Translation translation)
		{
			translation = await Events.TranslationBeforeCreate.Dispatch(context, translation);

			// Note: The Id field is automatically updated by Run here.
			if (translation == null || !await _database.Run(createQuery, translation))
			{
				return null;
			}

			translation = await Events.TranslationAfterCreate.Dispatch(context, translation);
			return translation;
		}

		/// <summary>
		/// Updates the given translation
		/// </summary>
		public async Task<Translation> Update(Context context, Translation translation)
		{
			translation = await Events.TranslationBeforeUpdate.Dispatch(context, translation);

			if (translation == null || !await _database.Run(updateQuery, translation, translation.Id))
			{
				return null;
			}

			translation = await Events.TranslationAfterUpdate.Dispatch(context, translation);
			return translation;
		}

	}
    
}
