using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Uploader
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
				Roles.Guest.Grant("upload_create");
				Roles.Member.Grant("upload_create");
				
				Roles.Guest.Revoke("upload_list", "upload_load");
				Roles.Member.Revoke("upload_list", "upload_load");
				Roles.Public.Revoke("upload_list", "upload_load");
				return new ValueTask<object>(source);
			});
		}
	}
}