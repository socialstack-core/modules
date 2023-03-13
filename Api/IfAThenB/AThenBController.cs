using Api.AvailableEndpoints;
using Api.Contexts;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.IfAThenB
{
    /// <summary>
    /// Handles a then b endpoints.
    /// </summary>

    [Route("v1/athenb")]
	public partial class AThenBController : AutoController<AThenB>
	{
		/// <summary>
		/// GET /v1/athenb/event-list
		/// Returns meta about all the available events in the API. Admin only.
		/// </summary>
		[HttpGet("event-list")]
		public async ValueTask<EventList> Get()
		{
			var context = await Request.GetContext();

			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("event_list", context);
			}

			return (_service as AThenBService).GetEventList();
		}


	}

}
