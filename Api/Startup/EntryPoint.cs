using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Api.Configuration;
using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using Api.Vcs;
using System.Diagnostics;

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
#if !DEBUG
			System.Console.WriteLine("API starting up. Log messages can be found in the socialstack log (not here) such that log entries are easier to filter and search.");
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

			Task.Run(async () =>
			{
				// Fire off initial OnStart handlers:
				await Api.Eventing.Events.TriggerStart();
			}).Wait();

			string apiSocketFile = null;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// Listen on a Unix socket too:
				apiSocketFile = System.IO.Path.GetFullPath("api.sock");

				try
				{
					// Delete if exists:
					System.IO.File.Delete(apiSocketFile);
				}
				catch { }
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

			if (args.Length != 0)
			{
				// this has been added as a switch case to allow for future expansion.
				// for the meantime though, we only support the "git" arg, this is 
				// to fire off scripts built in C# as not everyone will have node installed
				// or be on a specific platform, but we know for certain that C# will be available.

				switch (args[0])
				{
					case "git":

						// Services.BuildHost = "git";
						// Services.RegisterAndStart();

						// this isn't a GIT replacement, simply fires off when hooks are executed. 
						// this allows the ecosystem to run scripts in C# rather than node, which is useful for those who don't have node installed.

						// var svc = Services.Get<GitService>();
						string[] validOptions = ["pre-commit", "commit-msg", "pre-push"];

						if (args.Length == 1)
						{
							// no follow up args, this should just exit out with an error message.
							throw new InvalidOperationException("You must provide an option after 'dotnet run git'. Options are: " + string.Join(", ", validOptions));
						}

						var option = args[1].ToLowerInvariant();

						if (!validOptions.Contains(option))
						{
							throw new InvalidOperationException("Invalid option provided after 'dotnet run git'. Options are: " + string.Join(", ", validOptions));
						}

						// switch(option)
						// {
						//     // case "pre-commit":
						//     //     // run pre-commit hooks:
						//     //     svc.RunPreCommit().GetAwaiter().GetResult();
						//     //     break;
						//     // case "commit-msg":
						//     //     // run commit-msg hooks:
						//     //     svc.RunCommitMessage().GetAwaiter().GetResult();
						//     //     break;
						//     // case "pre-push":
						//     //     // run pre-push hooks:
						//     //     svc.RunPrePush().GetAwaiter().GetResult();
						//     //     break;
						// }

						break;
				}
				// then goto shutdown.
				System.Environment.Exit(0);
				return;
			}

			if (Services.HasHostType("web"))
			{
				// Create a Kestrel host:
				var host = new WebHostBuilder()
					.UseKestrel(options =>
					{
						var portNumber = AppSettings.GetInt32("Port", 5000);
						var ip = AppSettings.GetInt32("Container", 0) == 1 ? IPAddress.Any : IPAddress.Loopback;
						Log.Info("webserverservice", null, "Ready on " + ip + ":" + portNumber);

						// If running inside a container, we'll need to listen to the 0.0.0.0 (any) interface:
						options.Listen(ip, portNumber, listenOpts =>
						{
							listenOpts.Protocols = HttpProtocols.Http1AndHttp2;
						});

						if (apiSocketFile != null)
						{
							options.ListenUnixSocket(apiSocketFile);
						}

						options.Limits.MaxRequestBodySize = AppSettings.GetInt64("MaxBodySize", 5120000000); // 5G by default

						// Fire event so modules can also configure Kestrel:
						OnConfigureKestrel?.Invoke(options);

					});

				// Fire event so modules can also configure the host builder:
				OnConfigureHost?.Invoke(host);

				var builtHost = host.UseContentRoot(Directory.GetCurrentDirectory())
					.UseStartup<WebServerStartupInfo>()
					.Build();

				builtHost.Start();

				if (apiSocketFile != null)
				{
					Chmod.Set(apiSocketFile); // 777
				}

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
