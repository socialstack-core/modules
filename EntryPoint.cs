using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Api.Configuration;
using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Api.Startup
{
	/// <summary>
	/// This defines the Main method used when starting up your API.
	/// This instances any object with the [EventListener] attribute so you can 
	/// hook in here without needing to override the module.
	/// </summary>
    public class EntryPoint
	{
		/// <summary>
		/// Event which fires during the configuration of Kestrel.
		/// </summary>
		public static event Action<KestrelServerOptions> OnConfigureKestrel;

		/// <summary>
		/// Event which fires during the configuration of the web builder.
		/// </summary>
		public static event Action<IWebHostBuilder> OnConfigureHost;
		
		/// <summary>
		/// The main entry point for your project's API.
		/// </summary>
		public static void Main(string[] args)
        {
			// Hello! The very first thing we'll do is instance all event handlers.
			Api.Eventing.Events.Init();

			// Next we find any EventListener classes.
			var allTypes = typeof(EntryPoint).Assembly.DefinedTypes; 
			
			foreach (var typeInfo in allTypes)
			{
				// If it:
				// - Is a class
				// - Has the EventListener attribute
				// Then we instance it.

				if (!typeInfo.IsClass)
				{
					continue;
				}

				if (typeInfo.GetCustomAttributes(typeof(EventListenerAttribute), true).Length == 0)
				{
					continue;
				}
				
				// Got one - instance it now:
				Activator.CreateInstance(typeInfo);
			}

			// Ok - modules have now connected any core events or have performed early startup functionality.

			Task.Run(async () =>
			{
				// Fire off initial OnStart handlers:
				await Api.Eventing.Events.TriggerStart();
			}).Wait();
			
			// Create a Kestrel host:
			var host = new WebHostBuilder()
                .UseKestrel(options => {
					
					var portNumber = AppSettings.GetInt32("Port", 5000);
					
					// If running inside a container, we'll need to listen to the 0.0.0.0 (any) interface:
					options.Listen(AppSettings.GetInt32("Container", 0) == 1 ? IPAddress.Any : IPAddress.Loopback, portNumber);
					
                    options.Limits.MaxRequestBodySize = AppSettings.GetInt64("MaxBodySize", 512000000); // 512MB by default

					// Fire event so modules can also configure Kestrel:
					OnConfigureKestrel?.Invoke(options);

                });
			
			// Fire event so modules can also configure the host builder:
			OnConfigureHost?.Invoke(host);
			
			var builtHost = host.UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<WebServerStartupInfo>()
                .Build();

            builtHost.Run();
        }
    }
	
}
