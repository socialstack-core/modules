using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.ChatBotSimple
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
				Roles.Member.Grant("chatBotDecision_create");
				Roles.Public.Grant("chatBotDecision_create");
				Roles.Guest.Grant("chatBotDecision_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("chatBotDecision_load", "chatBotDecision_list");
				Roles.Public.Revoke("chatBotDecision_load", "chatBotDecision_list");
				Roles.Member.Revoke("chatBotDecision_load", "chatBotDecision_list");
				*/
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}