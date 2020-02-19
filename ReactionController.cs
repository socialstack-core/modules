using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Results;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Reactions
{
    /// <summary>
    /// Handles reaction endpoints (liking, disliking, upvoting, downvoting, love hearting etc).
    /// </summary>

    [Route("v1/reaction")]
	public partial class ReactionController : AutoController<Reaction, ReactionAutoForm>
    {
		/// <summary>
		/// POST /v1/reaction/
		/// Creates a new reaction. Returns the ID.
		/// </summary>
		[HttpPost]
		public override async Task<Reaction> Create([FromBody] ReactionAutoForm form)
		{
			var context = Request.GetContext();
			
			// Start building up our object.
			// Most other fields, particularly custom extensions, are handled by autoform.
			var reaction = new Reaction
			{
				UserId = context.UserId
			};
			
			#warning improvement - would be nice to describe this in AutoForm, so this Create method can be vanilla
			reaction.ContentTypeId = ContentTypes.GetId(form.ContentType);
			
			if (!ModelState.Setup(form, reaction))
			{
				return null;
			}

			form = await Events.Reaction.Create.Dispatch(context, form, Response) as ReactionAutoForm;

			if (form == null || form.Result == null)
			{
				// A handler rejected this request.
				return null;
			}

			reaction = await _service.Create(context, form.Result);

			if (reaction == null)
			{
				Response.StatusCode = 500;
				return null;
			}
			
            return reaction;
        }

    }

}
