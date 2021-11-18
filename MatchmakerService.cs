using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Api.Signatures;
using System;
using Api.Users;

namespace Api.Matchmakers
{
	/// <summary>
	/// Handles matchmakers.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MatchmakerService : AutoService<Matchmaker>
    {
		// private Dictionary<int, Matchmaker> matchmakerLookup
		private MatchServerService _serverService;
		private MatchService _matchService;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MatchmakerService(SignatureService signatures, MatchServerService serverService, MatchService matchService) : base(Events.Matchmaker)
        {
			_serverService = serverService;
			_matchService = matchService;

			// Example admin page install:
			InstallAdminPages("Matchmakers", "fa:fa-rocket", new string[] { "id", "name" });
			
			Cache(new CacheConfig<Matchmaker>(){
				Retain = true,
				Preload = true,
				OnCacheLoaded = () => {
					// The cache ID index is a matchmaker lookup.
					// matchmakerLookup = GetCacheForLocale(1).GetPrimary();

					return new ValueTask();
				}
			});
		}

		private void StartMatch(uint matchId)
		{

			// Tell the match server that it should start immediately, as we're no longer matchmaking people to the match.

		}

		/// <summary>
		/// Matchmakes using the given matchmaker.
		/// </summary>
		public async ValueTask<Match> Matchmake(Context context, Matchmaker matchmaker, int teamSize)
		{
			if (teamSize > matchmaker.MaxTeamSize)
			{
				// E.g. squad queued up in a solo queue.
				throw new PublicException("Team too big to join this queue.", "too_big");
			}

			if (matchmaker.Sticky && context.UserId != 0)
			{
				// Has this user already been matchmade?
				var stickyMatch = await _matchService.Where("MatchmakerId=? and UserInMatch=?", DataOptions.IgnorePermissions)
					.Bind(matchmaker.Id)
					.Bind(context.UserId)
					.First(context);

				if (stickyMatch != null)
				{
					return stickyMatch;
				}

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

			if (matchmaker.CurrentMatchId == 0)
			{
				// Spawn a new match now:

				result = new Match()
				{
					CreatedUtc = now,
					EditedUtc = now,
					MatchmakerId = matchmaker.Id,
					RegionId = matchmaker.RegionId,
					ActivityId = matchmaker.ActivityId
				};
				
				if (matchmaker.UsesServers)
				{
					var matchServer = _serverService.Allocate(context, matchmaker);

					if (matchServer == null)
					{
						throw new PublicException("No servers available", "no_servers");
					}

					result.MatchServerId = matchServer.Id;
				}

				result = await _matchService.Create(context, result, DataOptions.IgnorePermissions);
				matchmaker.CurrentMatchId = result.Id;
				matchmaker.TeamsAdded = 1;
				matchmaker.UsersAdded = teamSize;
			}
			else
			{
				matchmaker.TeamsAdded++;
				matchmaker.UsersAdded += teamSize;
				result = await _matchService.Get(context, matchmaker.CurrentMatchId, DataOptions.IgnorePermissions);
			}

			if (matchmaker.StartTimeUtc == null && matchmaker.TeamsAdded >= matchmaker.MinTeamCount)
			{
				// Countdown now starting - where we droppin' boys!
				matchmaker.StartTimeUtc = DateTime.UtcNow.AddSeconds(matchmaker.MaxQueueTime);
			}

			await Update(context, matchmaker, (Context c, Matchmaker mm) => {

			}, DataOptions.IgnorePermissions);

			if (matchmaker.Sticky && context.UserId != 0)
			{
				var stickyMapping = await _matchService.GetMap<User, uint>("UserInMatch");

				// Add match->user to the sticky map:
				await stickyMapping.CreateIfNotExists(context, result.Id, context.UserId);
			}

			return result;
		}

	}

}
