using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Api.Startup;
using Microsoft.Extensions.Logging;


namespace Api.ErrorLogging
{

	/// <summary>
	/// Event handlers are instanced automatically and form a major part of the pluggable architecture.
	/// Modules can define events via simply extending the Events class.
	/// Handlers are also heaviest on add - they're designed to maximise repeat run performance - so avoid rapidly adding and removing them.
	/// Instead add one handler at startup and then do a check inside it to see if it should run or not.
	/// </summary>
	public partial class EventHandler
	{
		
		public EventHandler()
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