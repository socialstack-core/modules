using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#warning Needs Linux support checking and also improve the error mode handling.
// -> E.g. when the node process goes down it should start a new one without requests failing in the meantime.
// -> ProcessRequestQueue is designed to be pooled for that purpose.

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

	public partial class StackToolsService : IStackToolsService
	{
		/// <summary>
		/// True if we attempted an install.
		/// </summary>
		internal bool AttemptedInstall = false;
		/// <summary>
		/// The node.js process handler
		/// </summary>
		private ProcessRequestQueue Node;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StackToolsService()
		{

			// Spawn the process now. We spawn it in the "interactive" mode which means we get one node.js service
			// which can handle multiple simultaneous requests via stdin.
			Spawn();


			// When the application shuts down, kill the node child process:
			Startup.WebServerStartupInfo.OnShutdown += () => {
				if (Node != null)
				{
					Node.Stop();
				}
			};
		}

		/// <summary>
		/// Get node.js to do something via sending it a serialisable request.
		/// Of the form {action: "name", ..anything else..}.
		/// </summary>
		/// <param name="serialisableMessage"></param>
		/// <param name="onResult">This callback runs when it responds.</param>
		public void Request(object serialisableMessage, OnStackToolsResponse onResult)
		{
			Node.Request(serialisableMessage, onResult);
		}

		/// <summary>
		/// Get node.js to do something via sending it a raw json request.
		/// Of the form {action: "name", ..anything else..}.
		/// </summary>
		/// <param name="json"></param>
		/// <param name="onResult">This callback runs when it responds.</param>
		public void RequestJson(string json, OnStackToolsResponse onResult)
		{
			Node.RequestJson(json, onResult);
		}

		private void Install()
		{
			AttemptedInstall = true;
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			// Try to globally install socialstack tools now:
			Process process = new Process();
			process.StartInfo.FileName = isWindows ? "cmd.exe" : "/bin/bash";
			process.StartInfo.Arguments = isWindows ? "/C npm install -g socialstack" : "-c \"npm install -g socialstack\"" ;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.ErrorDialog = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;

			process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (String.IsNullOrEmpty(e.Data))
				{
					return;
				}

				Console.WriteLine(e.Data);
			});

			process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				// Write to our output stream:
				Console.WriteLine(e.Data);
			});

			try
			{
				process.Start();
				process.WaitForExit();
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

				throw winE;
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
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var port = 17061;

			Process process = new Process();
			var npq = new ProcessRequestQueue(process, port);
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = isWindows ? "cmd.exe" : "/bin/bash";
			process.StartInfo.Arguments = isWindows ? "/C socialstack interactive -p " + port : "-c \"socialstack interactive -p "+port+"\"";
			process.StartInfo.WorkingDirectory = "";
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.ErrorDialog = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;

			npq.OnStateChange += (int state) =>
			{
				if (state == 0)
				{
					// The process is exiting - respawn:
					Spawn();
					return;
				}

				if (state == 3)
				{
					// It failed to start entirely.
					if (AttemptedInstall)
					{
						// Already tried an install - this module has a hard failure which we can't recover from.
						throw new Exception("Unable to start socialstack tools. " +
						"Try manually running 'npm install -g socialstack', possibly after installing node.js");
					}
					else
					{
						Console.WriteLine("[WARN] Socialstack tools didn't start.");
						Console.WriteLine("Attempting to install socialstack tools and we'll try again shortly.");

						// (install blocks)
						Install();
						Spawn();
					}
					
					return;
				}

				if (state != 2)
				{
					return;
				}

				// Start the UI watcher straight away:
				npq.Request(new { action = "watch" }, (string e, JObject response) => {
					if (e != null)
					{
						return;
					}

					Console.WriteLine("UI watcher started successfully.");

				});

			};

			// Start now:
			npq.Start();

			Node = npq;
		}
		
	}
    
}
