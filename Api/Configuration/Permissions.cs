using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Configuration
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
				Roles.Guest.Revoke("configuration_load", "configuration_list");
				Roles.Public.Revoke("configuration_load", "configuration_list");
				Roles.Member.Revoke("configuration_load", "configuration_list");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}