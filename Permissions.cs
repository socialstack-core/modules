using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Followers
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
                Roles.Member.Revoke("follower_delete", "follower_list")
                    .Grant("follower_create","follower_delete", "follower_list");

                Roles.Public.Revoke("follower_load");
				Roles.Public.Revoke("follower_list");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}