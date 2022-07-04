using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.ProfilePermits
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
				// Public - the role used by anonymous users.
                Roles.Member.Revoke("profilepermit_delete", "profilepermit_list")
                    .Grant("profilepermit_create", "profilepermit_delete", "profilepermit_list");

                Roles.Public.Revoke("profilepermit_load");
				Roles.Public.Revoke("profilepermit_list");

                return Task.FromResult(source);
			}, 20);
		}
	}
}