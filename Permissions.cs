using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.PrivateChats
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
				// Allow member creation (as it's disabled by default):
				Roles.Member.Grant("privatechat_create");
				Roles.Member.Grant("privatechatmessage_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("privateChat_load", "privateChat_list");
				Roles.Public.Revoke("privateChat_load", "privateChat_list");
				// Roles.Member.Revoke("privateChat_load", "privateChat_list");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("privateChatMessage_load", "privateChatMessage_list");
				Roles.Public.Revoke("privateChatMessage_load", "privateChatMessage_list");
				// Roles.Member.Revoke("privateChatMessage_load", "privateChatMessage_list");
				
				// Grant member load/ list if they're able to view the chat.
				// (TODO)
				
				return Task.FromResult(source);
			}, 20);
		}
	}
}