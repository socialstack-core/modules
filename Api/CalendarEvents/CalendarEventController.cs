using Microsoft.AspNetCore.Mvc;

namespace Api.CalendarEvents
{
    /// <summary>Handles calendarEvent endpoints.</summary>
    [Route("v1/calendarEvent")]
	public partial class CalendarEventController : AutoController<CalendarEvent>
    {
    }
}