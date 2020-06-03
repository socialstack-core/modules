using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.CalendarEvents
{
	/// <summary>
	/// Handles calendarEvents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ICalendarEventService
    {
		/// <summary>
		/// Delete a calendarEvent by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a calendarEvent by its ID.
		/// </summary>
		Task<CalendarEvent> Get(Context context, int id);

		/// <summary>
		/// Create a calendarEvent.
		/// </summary>
		Task<CalendarEvent> Create(Context context, CalendarEvent e);

		/// <summary>
		/// Updates the database with the given calendarEvent data. It must have an ID set.
		/// </summary>
		Task<CalendarEvent> Update(Context context, CalendarEvent e);

		/// <summary>
		/// List a filtered set of calendarEvents.
		/// </summary>
		/// <returns></returns>
		Task<List<CalendarEvent>> List(Context context, Filter<CalendarEvent> filter);

	}
}
