using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Api.Configuration;
using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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

			// Fire off initial OnStart handle:
			Api.Eventing.Events.TriggerStart();

			// Ok - modules have now connected any core events or have performed early startup functionality.

			// Create a Kestrel host:
			var host = new WebHostBuilder()
                .UseKestrel(options => {
					
					var portNumber = AppSettings.GetInt32("Port", 5000);
					
					options.Listen(IPAddress.Loopback, portNumber);
					
                    options.Limits.MaxRequestBodySize = AppSettings.GetInt64("MaxBodySize", 512000000); // 512MB by default

					// Fire event so modules can also configure Kestrel:
					OnConfigureKestrel?.Invoke(options);

                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<WebServerStartupInfo>()
                .Build();

            host.Run();
        }
    }
	
}
