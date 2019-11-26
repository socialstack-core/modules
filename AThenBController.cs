using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Users;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.IfAThenB
{
    /// <summary>
    /// Handles a then b endpoints.
    /// </summary>

    [Route("v1/athenb")]
	[ApiController]
	public partial class AThenBController : ControllerBase
    {
        private IAThenBService _aThenB;
        private IUserService _users;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public AThenBController(
            IAThenBService aThenB,
			IUserService users
        )
        {
            _aThenB = aThenB;
            _users = users;
        }

		/// <summary>
		/// GET /v1/athenb/2/
		/// Returns the a then b data for a single athenb.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<AThenB> Load([FromRoute] int id)
        {
			var context = Request.GetContext();
            var result = await _aThenB.Get(context, id);
			return await Events.AThenBLoad.Dispatch(context, result, Response);
        }

		/// <summary>
		/// DELETE /v1/athenb/2/
		/// Deletes an a then b
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<Success> Delete([FromRoute] int id)
		{
			var context = Request.GetContext();
			var result = await _aThenB.Get(context, id);
			result = await Events.AThenBDelete.Dispatch(context, result, Response);

			if (result == null || !await _aThenB.Delete(context, id))
			{
				// The handlers have blocked this one from happening, or it failed
				return null;
			}
			
            return new Success();
        }

		/// <summary>
		/// GET /v1/athenb/list
		/// Lists all athenb rules available to this user.
		/// </summary>
		/// <returns></returns>
		[HttpGet("list")]
		public async Task<Set<AThenB>> List()
		{
			return await List(null);
		}

		/// <summary>
		/// POST /v1/athenb/list
		/// Lists filtered a then b rules available to this user.
		/// See the filter documentation for more details on what you can request here.
		/// </summary>
		/// <returns></returns>
		[HttpPost("list")]
		public async Task<Set<AThenB>> List([FromBody] JObject filters)
		{
			var context = Request.GetContext();
			var filter = new Filter<AThenB>(filters);

			filter = await Events.AThenBList.Dispatch(context, filter, Response);

			if (filter == null)
			{
				// A handler rejected this request.
				return null;
			}

			var results = await _aThenB.List(context, filter);
			return new Set<AThenB>() { Results = results };
		}

		/// <summary>
		/// POST /v1/athenb/
		/// Creates a new a then b rule. Returns the ID.
		/// </summary>
		[HttpPost]
		public async Task<AThenB> Create([FromBody] AThenBAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var athenb = new AThenB
			{
				UserId = context.UserId
			};

			if (!ModelState.Setup(form, athenb))
			{
				return null;
			}

			form = await Events.AThenBCreate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			athenb = await _aThenB.Create(context, form.Result);

			if (athenb == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return athenb;
        }

		/// <summary>
		/// POST /v1/athenb/1/
		/// Creates a new a then b rule. Returns the ID.
		/// </summary>
		[HttpPost("{id}")]
		public async Task<AThenB> Update([FromRoute] int id, [FromBody] AThenBAutoForm form)
		{
			var context = Request.GetContext();

			var athenb = await _aThenB.Get(context, id);
			
			if (!ModelState.Setup(form, athenb)) {
				return null;
			}

			form = await Events.AThenBUpdate.Dispatch(context, form, Response);

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			athenb = await _aThenB.Update(context, form.Result);

			if (athenb == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
			return athenb;
		}

	}

}
