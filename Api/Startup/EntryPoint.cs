using Api.Configuration;
using Api.Contexts;
using Api.Eventing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
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
        /// The main entry point for your project's API.
        /// </summary>
        public static async Task Main(string[] args)
        {
#if !DEBUG
            if(args == null || args.Length == 0)
            {
			    System.Console.WriteLine("API starting up. Log messages can be found in the socialstack log (not here) such that log entries are easier to filter and search.");
            }
#endif
            // Hello! The very first thing we'll do is instance all event handlers.
            Api.Eventing.Events.Init();

            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs e) =>
            {
                Log.Error("core", e.Exception, "A task threw an error which was not caught.");
            };

            // Clone stdout into error engine:
            StdOut.Writer = new ConsoleWriter(Console.Out);
            Console.SetOut(StdOut.Writer);

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

            // Set the host type:
            /* e.g. 
				"HostTypes": {
					"HostNameMappings" : [
						{"Hostname":"DEV-VM-KB" , "HostType":"web"},
						{"Hostname":"DEV-VM-KB" , "HostType":"index"}
					]
				}
			*/

            var hostTypeConfig = AppSettings.GetSection("HostTypes").Get<HostTypeConfig>();

            if (hostTypeConfig != null && hostTypeConfig.HostNameMappings != null)
            {
                Services.HostNameMappings = hostTypeConfig.HostNameMappings;

                var hostName = System.Environment.MachineName.ToString();

                // Establish which host type this server is.
                Services.HostMappings = Services.GetHostMappings(hostName);
            }

			// Get environment name:
			var env = AppSettings.GetString("Environment", null);

            if (string.IsNullOrEmpty(env))
            {
                throw new Exception("You must declare the \"Environment\" field in your appsettings.json - typically its value is either \"dev\", \"stage\" or \"prod\".");
            }

            // Set environment:
            Services.Environment = Services.SanitiseEnvironment(env);
            Services.OriginalEnvironment = env;

			var startupContext = new Context(1, 0, 1);

			// Fire off initial OnStart handlers:
			await Api.Eventing.Events.TriggerStart(startupContext);

			if (args.Length != 0)
            {
                var commandArgsState = new CommandArgs
                {
                    Arguments = args
                };

				commandArgsState = await Events.Service.CommandLine.Dispatch(startupContext, commandArgsState);

                if (!commandArgsState.Handled)
                {
                    throw new Exception("Unknown arguments given: " + string.Join(' ', args));
                }

                if (commandArgsState.Shutdown)
                {
                    System.Environment.Exit(0);
                    return;
                }
            }

            if (Services.HasHostType("web"))
			{
                // Create a host:
                var host = new WebHostBuilder();

                // Fire event so modules can also configure the host builder:
                await Events.WebServerStartup.ConfigureHost.Dispatch(startupContext, host);

                var builtHost = host.Build();

                builtHost.Start();

                await Events.WebServerStartup.HostReady.Dispatch(startupContext, builtHost);

                builtHost.WaitForShutdown();
            }
            else
            {
                // Running without the webserver
                Services.RegisterAndStart();

                // Use a wait handle which never quits in this scenario.
                new AutoResetEvent(false).WaitOne();
            }
        }
    }

}
