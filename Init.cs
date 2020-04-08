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


namespace Api.Logging_Log4Net
{ 
	/// <summary>
	/// Instanced automatically at startup.
	/// </summary>
	[EventListener]
	public partial class Init
    {
        private ILogger _logger;

        /// <summary>
        /// Instanced automatically at startup.
        /// </summary>
        public Init()
        {
            EntryPoint.OnConfigureHost += OnConfigureHost;
            WebServerStartupInfo.OnConfigure += OnConfigure;

            Events.Logging.AddEventListener((context, logging) =>
            {
                LogLevel level = LogLevel.None;

                switch (logging.LogLevel)
                {
                    case LOG_LEVEL.Debug:
                        level = LogLevel.Debug;
                        break;
                    case LOG_LEVEL.Error:
                        level = LogLevel.Error;
                        break;
                    case LOG_LEVEL.Warning:
                        level = LogLevel.Warning;
                        break;
                    case LOG_LEVEL.Information:
                        level = LogLevel.Information;
                        break;
                }

                _logger.Log(level, logging.Message);

                return Task.FromResult(logging);
            });
        }

        private static string CONFIG_FILE_PATH = "Api/ThirdParty/Logging_Log4Net/log4net.config";

        /// <summary>
		/// Called by the entry point whilst the HTTP listener is starting.
		/// </summary>
		private void OnConfigureHost(IWebHostBuilder host)
		{
			host.ConfigureLogging((hostingContext, logging) =>
			{
				logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                if (hostingContext.HostingEnvironment.IsDevelopment())
                {
                    logging.AddConsole();
                    logging.AddDebug();
                }
                logging.AddLog4Net(Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, CONFIG_FILE_PATH));
			});
		}
		
		/// <summary>
		/// Called when the underlying HTTP pipeline is configured.
		/// </summary>
		private void OnConfigure(IApplicationBuilder app, IHostingEnvironment env, 
			ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IApplicationLifetime applicationLifetime)
		{
            loggerFactory.AddLog4Net(Path.Combine(env.ContentRootPath, CONFIG_FILE_PATH));

            _logger = loggerFactory.CreateLogger("Api");
        }
    }
}