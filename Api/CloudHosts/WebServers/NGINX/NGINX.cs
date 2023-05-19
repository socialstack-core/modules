using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Twilio.TwiML.Messaging;

namespace Api.CloudHosts
{
	
	/// <summary>
	/// NGINX specific configuration.
	/// </summary>
	public partial class NGINX : WebServer
    {
		/// <summary>
		/// Applies config changes and then performs a reload.
		/// </summary>
		/// <returns></returns>
		public override async ValueTask Apply()
		{
			// - todo - generate the actual nginx config file & write it out.

			await Reload();
		}

		/// <summary>
		/// Tells the webserver to reload config live. On supported servers this results in no downtime.
		/// Unsupported servers will perform a restart instead.
		/// </summary>
		public override async ValueTask Reload()
		{
			await CommandLine.Execute("nginx -s reload");
		}

		/// <summary>
		/// Stop/starts the web server service. Causes some downtime unlike Reload does.
		/// </summary>
		public override async ValueTask Restart()
		{
			await CommandLine.Execute("sudo service nginx restart");
		}

	}
    
}
