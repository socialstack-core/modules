using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.UserAgendaEntries
{
	/// <summary>
	/// Handles userAgendaEntries.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IUserAgendaEntryService
    {
		/// <summary>
		/// Delete an userAgendaEntry by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get an userAgendaEntry by its ID.
		/// </summary>
		Task<UserAgendaEntry> Get(Context context, int id);

		/// <summary>
		/// Create an userAgendaEntry.
		/// </summary>
		Task<UserAgendaEntry> Create(Context context, UserAgendaEntry e);

		/// <summary>
		/// Updates the database with the given userAgendaEntry data. It must have an ID set.
		/// </summary>
		Task<UserAgendaEntry> Update(Context context, UserAgendaEntry e);

		/// <summary>
		/// List a filtered set of userAgendaEntries.
		/// </summary>
		/// <returns></returns>
		Task<List<UserAgendaEntry>> List(Context context, Filter<UserAgendaEntry> filter);

	}
}
