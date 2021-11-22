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

			// Note: if rps pressure is expected, you should direct all matchmaking requests to 1 server in your cluster to eliminate match creation collisions.

			Match result = null;
			var thisThreadCreated = false;
			
			lock (matchmaker)
			{
				if ((matchmaker.UsersAdded + teamSize) > matchmaker.MaxMatchSize)
				{
					// Match is full - start packing a new one.
					matchmaker.CurrentMatchId = 0;
					matchmaker.TeamsAdded = 0;
					matchmaker.UsersAdded = 0;
				}

				if (matchmaker.CurrentMatchId == 0)
				{
					thisThreadCreated = true;

					matchmaker.MatchCreateTask = Task.Run(async () => {
						var now = DateTime.UtcNow;
						
						var res = new Match()
						{
							CreatedUtc = now,
							EditedUtc = now,
							MatchmakerId = matchmaker.Id,
							RegionId = matchmaker.RegionId,
							ActivityId = matchmaker.ActivityId
						};

						result = res;

						if (matchmaker.UsesServers)
						{
							var matchServer = _serverService.Allocate(context, matchmaker);

							if (matchServer == null)
							{
								throw new PublicException("No servers available", "no_servers");
							}

							res.MatchServerId = matchServer.Id;
						}

						res = await _matchService.Create(context, res, DataOptions.IgnorePermissions);
						matchmaker.CurrentMatchId = res.Id;
						matchmaker.TeamsAdded = 1;
						matchmaker.UsersAdded = teamSize;

						// Done:
						matchmaker.MatchCreateTask = null;

						return res;
					});

				}
			}

			if (matchmaker.MatchCreateTask != null)
			{
				result = await matchmaker.MatchCreateTask;
			}

			if (!thisThreadCreated)
			{
				lock (matchmaker)
				{
					// Add user to match:
					matchmaker.TeamsAdded++;
					matchmaker.UsersAdded += teamSize;
				}

				result = await _matchService.Get(context, matchmaker.CurrentMatchId, DataOptions.IgnorePermissions);
			}

			// Save matchmaker changes to DB.
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
