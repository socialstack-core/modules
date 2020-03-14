using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Translate
{
	/// <summary>
	/// Handles locales - the core of the translation (localisation) system.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ILocaleService
    {
		/// <summary>
		/// Delete a locale by its ID. Optionally also deletes posts.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a locale by its ID.
		/// </summary>
		Task<Locale> Get(Context context, int id);

		/// <summary>
		/// Create a new locale.
		/// </summary>
		Task<Locale> Create(Context context, Locale locale);

		/// <summary>
		/// Updates the database with the given locale data. It must have an ID set.
		/// </summary>
		Task<Locale> Update(Context context, Locale locale);

		/// <summary>
		/// List a filtered set of locales.
		/// </summary>
		/// <returns></returns>
		Task<List<Locale>> List(Context context, Filter<Locale> filter);
		
		/// <summary>
		/// Gets a locale from the cache.
		/// </summary>
		Task<Locale> GetCached(Context context, int id);

		/// <summary>
		/// Gets the locale cache.
		/// </summary>
		Task<Locale[]> GetAllCached(Context context);

	}
}
