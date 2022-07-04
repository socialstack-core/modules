using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Eventing;
using Api.Permissions;
using Microsoft.Extensions.Logging;
using Api.Startup;


namespace Api.ErrorLogging
{
    /// <summary>
	/// Instanced automatically at startup.
	/// </summary>
	[EventListener]
	public partial class Init
    {
        /// <summary>
        /// Instanced automatically at startup.
        /// </summary>
        public Init()
        {
            WebServerStartupInfo.OnConfigureApplication += OnConfigureApplication;
        }

		/// <summary>
		/// Called when the underlying HTTP handling application is configured.
		/// </summary>
		private void OnConfigureApplication(IApplicationBuilder app)
		{
            // Must be before configuring MVC
            app.UseMiddleware<ExceptionMiddleware>();
		}
		
	}
	
}