using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.Translate
{
	/// <summary>
	/// Handles locales - the core of the translation (localisation) system.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	[LoadPriority(2)]
	public partial interface ILocaleService
    {
		/// <summary>
		/// The name of the cookie when locale is stored.
		/// </summary>
		string CookieName { get;  }

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
		
	}
}
