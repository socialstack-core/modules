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
			/*
			var packetBuffer = new byte[512];

			for (var i = 0; i < 500; i++)
			{
				packetBuffer[i] = (byte)i;
			}

			var mac = new WebRTC.Crypto.HMac(new WebRTC.Crypto.Sha1Digest());
			mac.Init(new byte[32], 0, 32);

			for (var i = 0; i < 12; i++)
			{
				var t = new System.Threading.Thread(() =>
				{

					System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
					sw.Start();

					Span<byte> tempBuffer = stackalloc byte[20];

					for (var i = 0; i < 1500000; i++)
					{
						mac.StatelessOutput(packetBuffer, 0, 512, 1, tempBuffer);
					}

					var time = sw.ElapsedMilliseconds;
					Console.WriteLine("Watch took " + time + "ms");
					sw.Stop();
				});

				t.Start();
			}

			return;
			*/

			/*
			WebRTC.CertTest.Fingerprints();
			Console.WriteLine("DTLS server up");
			new WebRTC.WebRTCServer(41500);
			System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
			return;
			*/
			/*
			var dtlsMsg = "16FEFF000000000000000B0097010000A4000000000000008BFEFD60520DAADB4D37BDC6D3A073B52587C4538170F9AFBE893FB0AB42BB0A4C31C600000010C02BC02FCCA9CCA8C00AC009C013C0140100006A00170000FF01000100000A00080006001D00170018000B000201000010001200100677656272746308632D776562727463000D0020001E0403050306030203080408050806040105010601020104020502";
			
			var buffer = Api.SocketServerLibrary.Hex.Convert(dtlsMsg);
			
			var client = new WebRTC.RtpClient();
			
			WebRTC.Dtls.HandleMessage(buffer, buffer.Length, client);
			return;
			*/

			/*
			var stunMsg = "000100582112A442D631E608FFF2F8A731B980D3000600197769636568756E6F72376679777A36343A356562366633326400000000250000002400046E7E00FF802A00081822EEF08863526600080014C002F292F4713737C9E9AC87779E5670E59D968C8028000405786604";

			// message to be HMAC validated "000100582112A442D631E608FFF2F8A731B980D3000600197769636568756E6F72376679777A36343A356562366633326400000000250000002400046E7E00FF802A00081822EEF088635266"
			// (has not yet had its length adjustment)
			Console.WriteLine(stunMsg.Substring(0, 76 * 2));
			
			var buffer = Api.SocketServerLibrary.Hex.Convert(stunMsg);
			WebRTC.StunServer.HandleMessage(buffer, buffer.Length);

			return;
			*/

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


			// Create a Kestrel host:
			var host = new WebHostBuilder()
                .UseKestrel(options => {
					
					var portNumber = AppSettings.GetInt32("Port", 5000);
					var ip = AppSettings.GetInt32("Container", 0) == 1 ? IPAddress.Any : IPAddress.Loopback;
					Console.WriteLine("Ready on " + ip + ":" + portNumber);
					
					// If running inside a container, we'll need to listen to the 0.0.0.0 (any) interface:
					options.Listen(ip, portNumber, listenOpts => {
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
