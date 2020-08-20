using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Presence
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
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("presenceRecord_load", "presenceRecord_list");
				Roles.Public.Revoke("presenceRecord_load", "presenceRecord_list");
				Roles.Member.Revoke("presenceRecord_load", "presenceRecord_list");
				
				return Task.FromResult(source);
			}, 20);
		}
	}
}