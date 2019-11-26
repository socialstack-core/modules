using System;
using Api.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


namespace Api.Contexts
{

	/// <summary>
	/// Listens for various events to setup the auth system.
	/// </summary>
	[EventListener]
	public class EventListener
	{

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public EventListener()
		{
			// Hook up the OnConfigureServices method:
			Api.Startup.WebServerStartupInfo.OnConfigureServices += (IServiceCollection services) => {
				services.AddUserAuthentication();
			};

			// Also hook up the configure app method:
			Api.Startup.WebServerStartupInfo.OnConfigureApplication += (IApplicationBuilder app) => {
				app.UseAuthentication();
			};

		}

	}
}
