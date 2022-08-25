using System.IO;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Api.Configuration;
using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

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
		public static void Main()
        {
			// Hello! The very first thing we'll do is instance all event handlers.
			Api.Eventing.Events.Init();
			
			TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs e) => 
			{
				Console.WriteLine(e.Exception.ToString());
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
			var hostTypeConfig = AppSettings.GetSection("HostTypes").Get<HostTypeConfig>();

			if (hostTypeConfig != null && hostTypeConfig.HostNameMappings != null)
			{
				Services.HostNameMappings = hostTypeConfig.HostNameMappings;

				var hostName = System.Environment.MachineName.ToString();

				// Establish which host type this server is.
				Services.HostMapping = Services.GetHostMapping(hostName);
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
				
				try{
					// Delete if exists:
					System.IO.File.Delete(apiSocketFile);
				}catch{}
			}

			// Get environment name:
			var env = AppSettings.GetString("Environment", null);

			if (string.IsNullOrEmpty(env))
			{
				throw new Exception("You must declare the \"Environment\" field in your appsettings.json - typically its value is either \"dev\", \"stage\" or \"prod\".");
			}

			// Set environment:
			Services.Environment = env.ToLower().Trim();

			// Get webserver mode:
			var webserverMode = AppSettings.GetString("WebServerMode");

			if (webserverMode != null && webserverMode.ToLower() == "none")
			{
				// Running without the webserver
				Services.RegisterAndStart();
				
				// Use a wait handle which never quits in this scenario.
				new AutoResetEvent(false).WaitOne();
			}
			else
			{

				// Create a Kestrel host:
				var host = new WebHostBuilder()
					.UseKestrel(options =>
					{
						var portNumber = AppSettings.GetInt32("Port", 5000);
						var ip = AppSettings.GetInt32("Container", 0) == 1 ? IPAddress.Any : IPAddress.Loopback;
						Console.WriteLine("Ready on " + ip + ":" + portNumber);

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
        }
    }
	
	/// <summary>
	/// Helper for *nix file permissions.
	/// </summary>
	public static class Chmod
	{
		[DllImport("libc", EntryPoint="chmod", SetLastError = true)]
		[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "interop")]
		private static extern int chmod(string pathname, int mode);

		// user permissions
		/// <summary>
		/// user read
		/// </summary>
		public const int S_IRUSR = 0x100;

		/// <summary>
		/// user write
		/// </summary>
		public const int S_IWUSR = 0x80;

		/// <summary>
		/// user exec
		/// </summary>
		public const int S_IXUSR = 0x40;

		// group permission
		/// <summary>
		/// group read
		/// </summary>
		public const int S_IRGRP = 0x20;

		/// <summary>
		/// group write
		/// </summary>
		public const int S_IWGRP = 0x10;

		/// <summary>
		/// group exec
		/// </summary>
		public const int S_IXGRP = 0x8;

		// other permissions

		/// <summary>
		/// other read
		/// </summary>
		public const int S_IROTH = 0x4;

		/// <summary>
		/// other write
		/// </summary>
		public const int S_IWOTH = 0x2;

		/// <summary>
		/// other exec
		/// </summary>
		public const int S_IXOTH = 0x1;
		
		/// <summary>
		/// Sets 644 global read (+owner write) file permissions on a *nix platform
		/// </summary>
		/// <param name="filename"></param>
		public static void SetRead(string filename)
		{
			// 644
			var perms =
			S_IRUSR | S_IWUSR
			| S_IRGRP
			| S_IROTH;

			if (0 != chmod(Path.GetFullPath(filename), (int)perms))
				throw new Exception("Could not set Unix socket permissions");
		}
		
		/// <summary>
		/// Sets file permissions on a *nix platform
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="perms"></param>
		public static void Set(string filename, int perms = 0)
		{
			if(perms == 0){
				// 777
				perms =
				S_IRUSR | S_IXUSR | S_IWUSR
				| S_IRGRP | S_IXGRP | S_IWGRP
				| S_IROTH | S_IXOTH | S_IWOTH;
			}
			
			if (0 != chmod(Path.GetFullPath(filename), (int)perms))
				throw new Exception("Could not set Unix socket permissions");
		}
	}
	
}
