using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.PasswordResetRequests
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
				Roles.Member.Grant("passwordResetRequest_create");
				Roles.Public.Grant("passwordResetRequest_create");
				Roles.Guest.Grant("passwordResetRequest_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("passwordResetRequest_load", "passwordResetRequest_list");
				Roles.Public.Revoke("passwordResetRequest_load", "passwordResetRequest_list");
				Roles.Member.Revoke("passwordResetRequest_load", "passwordResetRequest_list");

				return new ValueTask<object>(source);
			}, 20);
		}
	}
}