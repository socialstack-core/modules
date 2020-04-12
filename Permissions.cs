using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Views
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
				Roles.Member.Revoke("view_list", "view_load", "view_update", "view_create");
				Roles.Guest.Revoke("view_list", "view_load", "view_update", "view_create");
				Roles.Public.Revoke("view_list", "view_load", "view_update", "view_create");
				
				return Task.FromResult(source);
			});
		}
	}
}