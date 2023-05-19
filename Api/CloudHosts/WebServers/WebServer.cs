using System;
using System.Threading.Tasks;

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
		public virtual ValueTask Apply()
		{
			throw new NotSupportedException();
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
