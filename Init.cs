using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
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
			EntryPoint.OnConfigureHost += OnConfigureHost;
			WebServerStartupInfo.OnConfigure += OnConfigure;
			WebServerStartupInfo.OnConfigureApplication += OnConfigureApplication;
		}
		
		/// <summary>
		/// Called by the entry point whilst the HTTP listener is starting.
		/// </summary>
		private void OnConfigureHost(IWebHostBuilder host)
		{
			host.ConfigureLogging((hostingContext, logging) =>
			{
				logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
				logging.AddConsole();
				logging.AddDebug();
				logging.AddLog4Net();
			});
		}
		
		/// <summary>
		/// Called when the underlying HTTP pipeline is configured.
		/// </summary>
		private void OnConfigure(IApplicationBuilder app, IHostingEnvironment env, 
			ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IApplicationLifetime applicationLifetime)
		{
			
            loggerFactory.AddLog4Net();
			
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