using Api.Contexts;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Translate
{
	/// <summary>
	/// Handles multiple locales for text on the UI and in the database.
	/// Uses the common PO/ POT formats for interchanging text.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public interface ITranslateService
	{
		/// <summary>
		/// Gets all translations for a given locale. Adding caching to this would be an easy win.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		Task<Translations> GetLocale(int localeId);

		/// <summary>
		/// Get a locale entry by its name (e.g. en_US).
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		Task<Locale> GetLocaleEntry(int localeId);

		/// <summary>
		/// Does the same as GetLocale only if the locale name is the DefaultLocale ("en_US") then null is returned instead.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		Task<Translations> GetLocaleNotDefault(int localeId);

		/// <summary>
		/// Deletes a single translation by its ID.
		/// </summary>
		/// <returns></returns>
		Task<bool> Delete(int entryId);

		/// <summary>
		/// Deletes all translations related to a partiuclar piece of content.
		/// </summary>
		/// <param name="contentTypeId">The group the content is in. E.g. "products".</param>
		/// <param name="contentId">The ID of the content - e.g. a product ID.</param>
		/// <returns></returns>
		Task<bool> Delete(int contentTypeId, int contentId);

		/// <summary>
		/// Updates the content for a particular translation.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="translation">The translation to update.</param>
		/// <returns></returns>
		Task<Translation> Update(Context context, Translation translation);

		/// <summary>
		/// Generates a POT file using en_US as the primary locale.
		/// </summary>
		/// <returns></returns>
		Task<string> GetPot();

		/// <summary>
		/// Gets a list of locales.
		/// </summary>
		/// <returns></returns>
		Task<List<Locale>> GetLocales();

		/// <summary>
		/// Rebuilds the JSON file for a particular locale (they're used by the frontend).
		/// Use this if you've directly updated the database and want to pump out a new JSON file.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		Task<Translations> UpdateJson(int localeId);
		
		/// <summary>
		/// Generates a PO file for the given locale using en_US as the primary locale.
		/// </summary>
		/// <returns></returns>
		Task<string> GetPo(int localeId);

		/// <summary>
		/// Parses a PO from its text to a set of translations.
		/// </summary>
		/// <param name="po"></param>
		/// <returns></returns>
		Task<Translations> ParsePO(System.IO.Stream po);
		
		/// <summary>
		/// Creates a new translation.
		/// </summary>
		/// <returns></returns>
		Task<Translation> Create(Context context, Translation translation);

		/// <summary>
		/// Updates the translations for a particular locale with the given set. Optionally deletes ones that don't exist.
		/// </summary>
		/// <param name="translations">The latest translation set.</param>
		/// <param name="localeId">The locale to update, e.g. "fr_FR" or just "en".</param>
		/// <param name="deleteIfDoesntExist">True if you want to delete translations from 
		/// the database that aren't in this translation set for this locale.</param>
		/// <returns></returns>
		Task<DiffSet<Translation>> Update(Translations translations, int localeId, bool deleteIfDoesntExist);

		/// <summary>
		/// Gets a set of translations for one or more entire content groups.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		Task<TranslationLookup<int>> GetByContentGroup(int localeId);

		/// <summary>
		/// Gets a given content group from the given locale - e.g. get all product translations.
		/// Then groups the result by the content ID (i.e. such that it's grouped by product ID).
		/// </summary>
		/// <param name="localeId"></param>
		/// <param name="contentTypeId"></param>
		/// <returns></returns>
		Task<TranslationLookup<int>> Get(int localeId, int contentTypeId);

		/// <summary>
		/// Get a translation with the given ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="translationId"></param>
		/// <returns></returns>
		Task<Translation> Get(Context context, int translationId);

	}
}
