using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.ContentSync
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
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				// Block all clustered server EPs:
				Roles.Member.Revoke("clusteredserver_list", "clusteredserver_load");
				Roles.Guest.Revoke("clusteredserver_list", "clusteredserver_load");
				Roles.Public.Revoke("clusteredserver_list", "clusteredserver_load");
				
				return new ValueTask<object>(source);
			});
		}
	}
}