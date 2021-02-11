using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.Pages
{
	[EventListener]
	public class Permissions
    {
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
				Roles.Member.Grant("clientBrand_create");
				Roles.Public.Grant("clientBrand_create");
				Roles.Guest.Grant("clientBrand_create");

				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("clientBrand_load", "clientBrand_list");
				Roles.Public.Revoke("clientBrand_load", "clientBrand_list");
				Roles.Member.Revoke("clientBrand_load", "clientBrand_list");
				*/
				Roles.Guest.Revoke("page_load");
				Roles.Public.Revoke("page_load");
				Roles.Member.Revoke("page_load");
				Roles.Guest.Grant("page_load");
				Roles.Public.Grant("page_load");
				Roles.Member.Grant("page_load");
				return new ValueTask<object>(source);
			});
		}
    }
}
