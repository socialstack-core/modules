using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Invites
{
	/// <summary>
	/// 
	/// </summary>
	[EventListener]
	public class Permissions
    {
		/// <summary>
		/// 
		/// </summary>
        public Permissions()
        {
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				Roles.Guest.Revoke("invite_load", "invite_list", "invite_create");
				Roles.Public.Revoke("invite_load", "invite_list", "invite_create");
				Roles.Member.Revoke("invite_load", "invite_list");
				
				return new ValueTask<object>(source);
			});
		}
    }
}
