using Api.Contexts;
using Api.Database;
using Api.Users;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Huddles
{
    /// <summary>Handles huddle endpoints.</summary>
    [Route("v1/huddle")]
	public partial class HuddleController : AutoController<Huddle>
    {
		
		/// <summary>
		/// Join a huddle. Provided the user is permitted, this returns the connection information.
		/// </summary>
		[HttpGet("{id}/join")]
		public async Task<object> Join(int id)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			var service = (_service as HuddleService);
			
			// Get the huddle:
			var huddle = await service.Get(context, id);
			
			if(huddle == null){
				// Doesn't exist or not permitted (the permission system internally checks huddle type and invites).
				return null;
			}

			// Is the current contextual user permitted to join?
			// Either it's open, or they must be on the invite list (don't have to specifically have accepted though):
			if (!service.IsPermitted(context, huddle))
			{
				return null;
			}
			
			// Sign a join URL:
			var connectionUrl = await service.SignUrl(context, huddle);
			
			return new {
				huddle,
				role = huddle.UserId == context.UserId ? 1 : 4,
				connectionUrl
			};
		}
		
    }
	
}