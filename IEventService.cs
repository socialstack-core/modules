using Api.Contexts;
using Api.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.CalendarEvents
{
	/// <summary>
	/// Handles events - usually seen in e.g. knowledge bases or help guides.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IEventService
    {
		/// <summary>
		/// Delete a event by its ID.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<bool> Delete(Context context, int id);

		/// <summary>
		/// Get a event by its ID.
		/// </summary>
		Task<Event> Get(Context context, int id);

		/// <summary>
		/// Create a new event.
		/// </summary>
		Task<Event> Create(Context context, Event evt);

		/// <summary>
		/// Updates the database with the given event data. It must have an ID set.
		/// </summary>
		Task<Event> Update(Context context, Event evt);

		/// <summary>
		/// List a filtered set of events.
		/// </summary>
		/// <returns></returns>
		Task<List<Event>> List(Context context, Filter<Event> filter);

	}
}
