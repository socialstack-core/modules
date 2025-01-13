using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Redirects;
using Api.Contexts;
using Api.Startup;

namespace Api.CloudHosts
{
	/// <summary>
	/// Generic webserver features.
	/// </summary>
	public partial class WebServer
    {

        /// <summary>
        /// Applies config changes and then performs a reload.
        /// </summary>
        /// <returns></returns>
        public virtual async ValueTask Apply(Context context)
        {
            await Reload();
        }

		/// <summary>
		/// Informs the webserver that the certificate set has updated. It might have not changed at all.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="certSet"></param>
		/// <returns></returns>
		public virtual async ValueTask UpdateCertificates(Context context, Dictionary<string, DomainCertificateLocales> certSet)
		{
			// Simply calls Apply by default. It is expected that Apply internally gets the cert set from the webserver service.
			await Apply(context);
		}

        /// <summary>
        /// Tells the webserver to reload config live. On supported servers this results in no downtime.
        /// Unsupported servers will perform a restart instead.
        /// </summary>
        public virtual ValueTask Reload()
		{
			return Restart();
		}

		/// <summary>
		/// Stop/starts the web server service. Causes some downtime unlike Reload does.
		/// </summary>
		public virtual ValueTask Restart()
		{
			throw new NotSupportedException();
		}

    }

}
