using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Chats
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
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("chat_create");

				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("chat_load", "chat_list" );
				Roles.Public.Revoke("chat_load", "chat_list");
				Roles.Member.Revoke("chat_load", "chat_list");

				// Chat Visibility
				Roles.Member.If("HasUserPermit() or IsSelf() or IsIncluded()").ThenGrant("chat_load", "chat_list", "chat_update");
				Roles.Member.If("IsIncluded() or IsSelf()").ThenGrant("message_load", "message_list");
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("chat_create");
				Roles.Public.Grant("chat_create");
				Roles.Guest.Grant("chat_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("chat_load", "chat_list");
				Roles.Public.Revoke("chat_load", "chat_list");
				Roles.Member.Revoke("chat_load", "chat_list");
				*/

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}