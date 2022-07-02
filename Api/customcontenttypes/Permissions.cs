using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.CustomContentTypes
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
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("customContentType_create");
				Roles.Public.Grant("customContentType_create");
				Roles.Guest.Grant("customContentType_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("customContentType_load", "customContentType_list");
				Roles.Public.Revoke("customContentType_load", "customContentType_list");
				Roles.Member.Revoke("customContentType_load", "customContentType_list");
				*/
				
				/*
				Example permission rules.
				
				Member role: A verified user account. Not an admin.
				Guest role: A user account. The transition from guest to member is up to you.
				Public role: Not logged in at all.
				
				// Allow public creation (as it's disabled by default):
				Roles.Member.Grant("customContentTypeField_create");
				Roles.Public.Grant("customContentTypeField_create");
				Roles.Guest.Grant("customContentTypeField_create");
				
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("customContentTypeField_load", "customContentTypeField_list");
				Roles.Public.Revoke("customContentTypeField_load", "customContentTypeField_list");
				Roles.Member.Revoke("customContentTypeField_load", "customContentTypeField_list");
				*/
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}