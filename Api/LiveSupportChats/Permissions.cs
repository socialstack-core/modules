using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.LiveSupportChats
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
				Roles.Member.Grant("liveSupportChat_create");
				Roles.Public.Grant("liveSupportChat_create");
				Roles.Guest.Grant("liveSupportChat_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("liveSupportChat_load", "liveSupportChat_list");
				Roles.Public.Revoke("liveSupportChat_load", "liveSupportChat_list");
				Roles.Member.Revoke("liveSupportChat_load", "liveSupportChat_list");
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("liveSupportMessage_create");
				Roles.Public.Grant("liveSupportMessage_create");
				Roles.Guest.Grant("liveSupportMessage_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("liveSupportMessage_load", "liveSupportMessage_list");
				Roles.Public.Revoke("liveSupportMessage_load", "liveSupportMessage_list");
				Roles.Member.Revoke("liveSupportMessage_load", "liveSupportMessage_list");

				// Can load and list chats if yours (or admin/ support):
				Roles.Guest.If((Filter f) => f.IsSelf()).ThenGrant("liveSupportChat_load", "liveSupportChat_list");
				Roles.Member.If((Filter f) => f.IsSelf()).ThenGrant("liveSupportChat_load", "liveSupportChat_list");

				Roles.Guest.If((Filter f) => {
					return f.Equals("ChatCreatorUserId", (Context ctx) => Task.FromResult((object)ctx.UserId));
				}).ThenGrant("liveSupportMessage_load", "liveSupportMessage_list");

				Roles.Member.If((Filter f) => {
					return f.Equals("ChatCreatorUserId", (Context ctx) => Task.FromResult((object)ctx.UserId));
				}).ThenGrant("liveSupportMessage_load", "liveSupportMessage_list");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}