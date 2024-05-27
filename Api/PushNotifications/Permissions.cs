using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.PushNotifications
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
				Roles.Member.Grant("userDevice_create");
				Roles.Public.Grant("userDevice_create");
				Roles.Guest.Grant("userDevice_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("userDevice_load", "userDevice_list");
				Roles.Public.Revoke("userDevice_load", "userDevice_list");
				Roles.Member.Revoke("userDevice_load", "userDevice_list");
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}