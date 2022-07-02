using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.CalendarEvents
{
	/// <summary>
	/// Handles calendarEvents.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CalendarEventService : AutoService<CalendarEvent>, ICalendarEventService
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CalendarEventService() : base(Events.CalendarEvent)
        {
			InstallAdminPages("Events", "fa:fa-calendar", new string[] { "id", "name" });
		}
	}
    
}
