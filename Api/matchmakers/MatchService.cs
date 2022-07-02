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

namespace Api.Matchmakers
{
	/// <summary>
	/// Handles matches.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class MatchService : AutoService<Match>
    {
		private SignatureService _signatures;
		private MatchServerService _serverService;
		
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public MatchService(SignatureService signatures, MatchServerService serverService) : base(Events.Match)
        {
			_signatures = signatures;
			_serverService = serverService;

			Cache();
		}
		
		
		/// <summary>
		/// Generates a join link for the given match.
		/// </summary>
		public async ValueTask<string> Join(Context context, Match match)
		{
			var user = context.User;
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
		
	}
    
}
