using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.StackTools
{
	/// <summary>
	/// Delegate used when a response is received.
	/// </summary>
	/// <param name="error"></param>
	/// <param name="data"></param>
	public delegate void OnStackToolsResponse(string error, JObject data);

	/// <summary>
	/// This service is used to invoke the socialstack command line tools (node.js) 
	/// which e.g. build/ serverside render the UI and render emails etc.
	/// </summary>
	public partial class StackToolsService
	{
		
		private bool _stopping;
		/// <summary>
		/// True if we attempted an install.
		/// </summary>
		internal bool AttemptedInstall = false;
		/// <summary>
		/// The node.js Process
		/// </summary>
		private NodeProcess NodeProcess;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StackToolsService(
#if !NETCOREAPP2_1 && !NETCOREAPP2_2
			IHostApplicationLifetime lifetime
#endif
		)
		{
			Task.Run(() =>
			{
				// Version + install check:
				CheckInstall();

				// Spawn the service now. We spawn it in the "interactive" mode which means we get one node.js service
				// which can handle multiple simultaneous requests via stdin.
				Spawn();
			});

#if !NETCOREAPP2_1 && !NETCOREAPP2_2
			lifetime.ApplicationStopping.Register(() => {
				StopAll();
			});
#endif

			AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => {
				StopAll();
			};

		}
		
		/// <summary>
		/// Stops all processes.
		/// </summary>
		private void StopAll()
		{
			if (_stopping)
			{
				return;
			}

			_stopping = true;
			
			if (NodeProcess != null && NodeProcess.Process != null && !NodeProcess.Process.HasExited)
			{
#if NETCOREAPP2_1 || NETCOREAPP2_2
				NodeProcess.Process.Kill();
#else
				NodeProcess.Process.Kill(true);
#endif
			}
		}

		/// <summary>
		/// Get node.js to do something via sending it a serialisable request.
		/// Of the form {action: "name", ..anything else..}.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="onResult">This callback runs when it responds.</param>
		public void Request(Request msg, OnStackToolsResponse onResult)
		{
			NodeProcess.Request(msg, onResult);
		}
		
		/// <summary>
		/// Null if not installed.
		/// </summary>
		/// <returns></returns>
		private Version GetToolsVersion()
		{
			// Version check:
			var versionChecker = new NodeProcess("socialstack version", true);

			string versionText = "";

			versionChecker.Process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}
				versionText += e.Data;
			});

			try
			{
				versionChecker.StartSync();
			}
			catch (System.ComponentModel.Win32Exception)
			{
				// Socialstack isn't installed at all (or is just really old!)
				return null;
			}
			return versionText == "" ? null : new Version(versionText.Trim());
		}

		/// <summary>
		/// Min tools version
		/// </summary>
		private Version MinVersion = new Version("1.0.90");

		private void CheckInstall()
		{
			// Get the version:
			var version = GetToolsVersion();
			
			if (version == null)
			{
				Console.WriteLine("Socialstack tools isn't installed - attempting to install now.");
			}
			else if (version < MinVersion)
			{
				Console.WriteLine("Socialstack tools is below the required min version - upgrading now.");
			}
			else
			{
				Console.WriteLine("Socialstack tools passed version check.");
				return;
			}

			AttemptedInstall = true;

			// Try to globally install socialstack tools now:
			var np = new NodeProcess("npm install -g socialstack");
			
			try
			{
				np.Process.Start();
				np.Process.WaitForExit();
			}
			catch (System.ComponentModel.Win32Exception winE)
			{
				if (winE.NativeErrorCode == 2)
				{
					// Node probably isn't installed.
					// If it is installed, then this error can also occur 
					// if you don't have npm in your PATH (on Windows).
					Console.WriteLine("Unable to install socialstack tools because you don't have npm. " +
						"Make sure node.js is installed and npm is available on your shell/ command line.");
				}

				throw;
			}
		}

		/// <summary>
		/// Spawns a new interactive process.
		/// If it fails because the tools aren't installed, it will go ahead and try to install them.
		/// You can install manually too if you'd like: run "npm install -g socialstack".
		/// </summary>
		/// <returns></returns>
		private void Spawn()
		{
			NodeProcess = new NodeProcess("socialstack interactive -parent " + Process.GetCurrentProcess().Id); // Environment.ProcessId (.NET 5)

			NodeProcess.OnStateChange += (NodeProcessState state) => {
				if (state == NodeProcessState.READY)
				{

					// We default to prod mode if we're a release build.
#if DEBUG
					var prod = false;
#else
			var prod = true;
#endif
					// Start the UI watcher straight away:
					NodeProcess.Request(new WatchRequest()
					{
						minified = prod,
						compress = prod
					}, (string e, JObject response) => {
						if (e != null)
						{
							return;
						}

						Console.WriteLine("UI watcher started successfully.");
					});
				}
			};

			// Start it now:
			NodeProcess.Start();

		}
		
	}
    
}
