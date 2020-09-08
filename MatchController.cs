using Api.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Matchmaking
{
    /// <summary>Handles match endpoints.</summary>
    [Route("v1/match")]
	public partial class MatchController : AutoController<Match>
    {
		/// <summary>
		/// Asks the given matchmaker to matchmake. Provided the user is permitted, this returns the connection information.
		/// </summary>
		[HttpGet("{matchmakerId}/matchmake")]
		public async Task<object> Matchmake(int matchmakerId)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			// Get matchmaker:
			var service = (_service as IMatchService);
			
			var match = await service.Matchmake(context, matchmakerId, 1);
			
			if(match == null)
			{
				return null;
			}
			
			// Sign a join URL.
			// Note! We don't care if a match has already started - we'll still generate the join URL.
			// This permits friend spectators and similar.
			// It's up to the server to decide if people can actually join a match or not.
			var connectionUrl = await service.Join(context, match);
			
			return new {
				match,
				connectionUrl
			};
		}
		
		/// <summary>
		/// Join match. Provided the user is permitted, this returns the connection information.
		/// </summary>
		[HttpGet("{id}/join")]
		public async Task<object> Join(int id)
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}
			
			var service = (_service as IMatchService);
			
			// Get the match:
			var match = await service.Get(context, id);
			
			if(match == null){
				// Doesn't exist or not permitted (the permission system internally checks huddle type and invites).
				return null;
			}
			
			// Sign a join URL.
			// This permits friend spectators and similar.
			// It's up to the server to decide if people can actually join a match or not.
			var connectionUrl = await service.Join(context, match);
			
			return new {
				match,
				connectionUrl
			};
		}
		
    }
}