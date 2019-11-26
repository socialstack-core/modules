using System;
using System.Threading.Tasks;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using Api.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.CalendarEvents
{
    /// <summary>
    /// Handles event endpoints.
    /// </summary>

    [Route("v1/event")]
	[ApiController]
	public partial class EventController : ControllerBase
    {
        private IEventService _events;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventController(
			IEventService events

		)
        {
			_events = events;
        }

		/// <summary>
		/// GET /v1/event/2/
		/// Returns the event data for a single event.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<Event> Load([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _events.Get(context, id);
			return await Api.Eventing.Events.EventLoad.Dispatch(context, result, Response);
		}

		/// <summary>
		/// DELETE /v1/event/2/
		/// Deletes an event
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _events.Get(context, id);
			result = await Api.Eventing.Events.EventDelete.Dispatch(context, result, Response);

			if (result == null || !await _events.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
			return new Success();
		}

		/// <summary>
		/// GET /v1/event/list
		/// Lists all events available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<Event>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/event/list
		/// Lists filtered events available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<Event>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<Event>(filters);

			filter = await Api.Eventing.Events.EventList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _events.List(context, filter);
			return new Set<Event>() { Results = results };
		}

		/// <summary>
		/// POST /v1/event/
		/// Creates a new event. Returns the ID.
		/// </summary>
		[HttpPost]
		[HttpPost]
		public async Task<Event> Create([FromBody] EventAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var evt = new Event
			{
				UserId = context.UserId
			};
			
			if (!ModelState.Setup(form, evt))
			{
				return null;
			}

			form = await Events.EventCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			evt = await _events.Create(context, form.Result);

			if (evt == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return evt;
        }

		/// <summary>
		/// POST /v1/event/1/
		/// Updates an event with the given ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<Event> Update([FromRoute] int id, [FromBody] EventAutoForm form)
		{
			var context = Request.GetContext();

			var evt = await _events.Get(context, id);
			
			if (!ModelState.Setup(form, evt)) {
				return null;
			}

			form = await Events.EventUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			evt = await _events.Update(context, form.Result);

			if (evt == null)
			{
				Response.StatusCode = 500;
				return null;
			}

			return evt;
		}
		
    }

}
