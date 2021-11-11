using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Matchmakers
{
	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Permissions
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Permissions()
		{
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("matchmaker_create");
				Roles.Public.Grant("matchmaker_create");
				Roles.Guest.Grant("matchmaker_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("matchmaker_load", "matchmaker_list");
				Roles.Public.Revoke("matchmaker_load", "matchmaker_list");
				Roles.Member.Revoke("matchmaker_load", "matchmaker_list");
				*/
				
				/*
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("match_create");
				Roles.Public.Grant("match_create");
				Roles.Guest.Grant("match_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("match_load", "match_list");
				Roles.Public.Revoke("match_load", "match_list");
				Roles.Member.Revoke("match_load", "match_list");
				*/
				
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("matchServer_create");
				Roles.Public.Grant("matchServer_create");
				Roles.Guest.Grant("matchServer_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("matchServer_load", "matchServer_list");
				Roles.Public.Revoke("matchServer_load", "matchServer_list");
				Roles.Member.Revoke("matchServer_load", "matchServer_list");
				*/
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}