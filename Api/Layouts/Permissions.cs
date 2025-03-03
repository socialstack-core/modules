using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Layouts
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
			Events.CapabilityOnSetup.AddEventListener((context, source) =>
			{
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("layout_create");
				Roles.Public.Grant("layout_create");
				Roles.Guest.Grant("layout_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("layout_load", "layout_list");
				Roles.Public.Revoke("layout_load", "layout_list");
				Roles.Member.Revoke("layout_load", "layout_list");
				*/
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}