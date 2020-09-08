using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Signatures;
using System.Web;
using System;
using Api.Startup;

namespace Api.Matchmaking
{
	/// <summary>
	/// Handles matches.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MatchService : AutoService<Match>, IMatchService
    {
		private ISignatureService _signatures;
		private IMatchServerService _serverService;
		private IMatchmakerService _matchmakers;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MatchService(ISignatureService signatures, IMatchServerService serverService, IMatchmakerService matchmakers) : base(Events.Match)
        {
			_signatures = signatures;
			_matchmakers = matchmakers;
			_serverService = serverService;
		}
		
		
		/// <summary>
		/// Generates a join link for the given match.
		/// </summary>
		public async Task<string> Join(Context context, Match match)
		{
			var user = await context.GetUser();
			string displayName = user == null ? "Anonymous" : user.Username;
			string avatarRef = user == null ? (string)null : user.AvatarRef;
			
			var queryStr = "i=" + match.Id + 
			"&u=" + context.UserId + 
			"&d=" + HttpUtility.UrlEncode(displayName) + 
			"&a=" + HttpUtility.UrlEncode(avatarRef) +
			"&type=" + match.ActivityId;
			
			// This signature is what allows the user to fully authenticate on a db-less target server:
			var sig = _signatures.Sign(queryStr);

			var server = await _serverService.Get(context, match.MatchServerId);

			if (server == null)
			{
				return null;
			}
			
			return server.Address + "/?" + queryStr + "&sig=" + HttpUtility.UrlEncode(sig);
		}
		
		private void StartMatch(int matchId){
			
			// Tell the match server that it should start immediately, as we're no longer matchmaking people to the match.
			
		}
		
		/// <summary>
		/// Matchmakes using the given matchmaker.
		/// </summary>
		public async Task<Match> Matchmake(Context context, int matchmakerId, int teamSize)
		{
			// Get the matchmaker:
			var matchmaker = await _matchmakers.Get(context, matchmakerId);
			
			if(matchmaker == null)
			{
				return null;
			}
			
			if(teamSize > matchmaker.MaxTeamSize)
			{
				// E.g. squad queued up in a solo queue.
				throw new PublicException("Team too big to join this queue.", "too_big");
			}
			
			// Note: under extreme pressure spread over a cluster, this matchmaking will result in duplicated updates to matchmakers.
			// It may, for example, generate 2 new matches.

			Match result = null;
			var now = DateTime.UtcNow;
			var alreadyStarted = matchmaker.StartTimeUtc != null && matchmaker.StartTimeUtc < now;

			if (alreadyStarted || ((matchmaker.UsersAdded + teamSize) > matchmaker.MaxMatchSize))
			{
				// Close off this previous match and essentially mark it full. Matchmaker will no longer pack users into it.
				if (!alreadyStarted)
				{
					StartMatch(matchmaker.CurrentMatchId);
				}
				matchmaker.CurrentMatchId = 0;
				matchmaker.TeamsAdded = 0;
				matchmaker.UsersAdded = 0;
				matchmaker.StartTimeUtc = null;
			}
			
			if(matchmaker.CurrentMatchId == 0)
			{
				// Spawn a new match now:
				var matchServer = _serverService.Allocate(context, matchmaker);

				if (matchServer == null)
				{
					throw new PublicException("No servers available", "no_servers");
				}

				result = new Match()
				{
					CreatedUtc = now,
					EditedUtc = now,
					MatchmakerId = matchmaker.Id,
					RegionId = matchmaker.RegionId,
					ActivityId = matchmaker.ActivityId,
					MatchServerId = matchServer.Id
				};

				result = await Create(context, result);
				matchmaker.CurrentMatchId = result.Id;
				matchmaker.TeamsAdded = 1;
				matchmaker.UsersAdded = teamSize;
			}
			else
			{
				matchmaker.TeamsAdded++;
				matchmaker.UsersAdded+=teamSize;
			}
			
			if(matchmaker.StartTimeUtc == null && matchmaker.TeamsAdded >= matchmaker.MinTeamCount)
			{
				// Countdown now starting - where we droppin' boys!
				matchmaker.StartTimeUtc = DateTime.UtcNow.AddSeconds(matchmaker.MaxQueueTime);
			}
			
			await _matchmakers.Update(context, matchmaker);
			
			return result;
		}
		
	}
    
}
