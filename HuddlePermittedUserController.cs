using Api.Contexts;
using Api.Database;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Huddles
{
    /// <summary>Handles huddlePermittedUser endpoints.</summary>
    [Route("v1/huddlepermitteduser")]
	public partial class HuddlePermittedUserController : AutoController<HuddlePermittedUser>
    {
		
		/// <summary>
        /// Used to accept a huddle invite.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/accept")]
        public async Task<HuddlePermittedUser> Accept(int id)
        {
            var context = Request.GetContext();
            
            // Grab the request
            var invite = await _service.Get(context, id, DataOptions.IgnorePermissions);
			
			if(invite == null || context == null || context.UserId == 0)
			{
				return null;
			}

            // Is the context user able to accept this request?
            if (!context.HasContent(invite.InvitedContentTypeId, invite.InvitedContentId))
            {
                // Nope!
                return null;
            }

            // Accept it now, unless it's already accepted.
            var queryStr = Request.Query;
            var force = false;

            if (queryStr != null && queryStr.ContainsKey("force"))
            {
                // Force overwrite the invite if it's already taken.
                force = true;
            }

            return await (_service as HuddlePermittedUserService).Accept(context, invite, force);
		}

        /// <summary>
        /// Used to reject a huddle invite.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/reject")]
        public async Task<HuddlePermittedUser> Reject(int id)
        {
            var context = Request.GetContext();

            // Grab the request
            var invite = await _service.Get(context, id, DataOptions.IgnorePermissions);

            if (invite == null)
            {
                return null;
            }

            // Is the context user able to accept this request?
            if (!context.HasContent(invite.InvitedContentTypeId, invite.InvitedContentId))
            {
                // Nope!
                return null;
            }

            return await (_service as HuddlePermittedUserService).RejectOrCancel(context, invite);
        }

    }
}