using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using System.IO;
using Api.CloudHosts;

namespace Api.Redirects
{
	/// <summary>
	/// Handles redirects.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class RedirectService : AutoService<Redirect>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public RedirectService(WebServerService webServer) : base(Events.Redirect)
        {
			InstallAdminPages("Redirects", "fa:fa-reply", new string[] { "id", "from", "to" });

            // reference config file
            // var path = "/etc/nginx/sites-enabled";

            Events.Redirect.AfterUpdate.AddEventListener(async (Context context, Redirect redirect) =>
            {
                await webServer.Regenerate(context);
                return redirect;
            });

            Events.Redirect.AfterCreate.AddEventListener(async (Context context, Redirect redirect) =>
            {
                await webServer.Regenerate(context);
                return redirect;
            });

            Events.Redirect.AfterDelete.AddEventListener(async (Context context, Redirect redirect) =>
            {
                await webServer.Regenerate(context);
                return redirect;
            });

        }

	}

}
