using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

	public partial class StackToolsService : IStackToolsService
	{
		/// <summary>
		/// True if we attempted an install.
		/// </summary>
		internal bool AttemptedInstall = false;
		/// <summary>
		/// The node.js process handler
		/// </summary>
		private ProcessLink Node;

		private Socket ListenSocket;
		
		/// <summary>
		/// Processes which have been spawned and are currently waiting to connect.
		/// </summary>
		private List<NodeProcess> PendingProcesses = new List<NodeProcess>();

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public StackToolsService()
		{

			// Spawn the service now. We spawn it in the "interactive" mode which means we get one node.js service
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

		private int Port;

		private void StartListening()
		{
			if (Port != 0)
			{
				// We're already listening.
				return;
			}

			ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			// Bind to port 0 will tell the OS to give us any available port.
			ListenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
			Port = ((IPEndPoint)ListenSocket.LocalEndPoint).Port;

			Console.WriteLine("Ready for stack tools processes on port " + Port);

			ListenSocket.Listen(5);
			ListenSocket.BeginAccept(OnConnect, null);
		}

		private void OnConnect(IAsyncResult ar)
		{
			if (ListenSocket == null)
			{
				return;
			}

			// Get the socket:
			Socket socket = ListenSocket.EndAccept(ar);

			// Continue accepting more connections:
			ListenSocket.BeginAccept(OnConnect, null);

			// Non-blocking socket:
			socket.Blocking = false;

			var link = new ProcessLink(socket);
			Node = link;
			link.OnReady = () => {
				// Get the process that this relates to:
				var process = PendingProcesses.Find(proc => proc.Id == Node.Id);

				if (process == null)
				{
					// This wasn't meant for us.
					return;
				}

				PendingProcesses.Remove(process);
				Node.Process = process;
				process.StateChange(NodeProcessState.READY);
			};

			link.Begin();
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
			var np = new NodeProcess("npm install -g socialstack", false);
			
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
			StartListening();
			
			var process = new NodeProcess("socialstack interactive -p " + Port, true);

			process.OnStateChange += (NodeProcessState state) =>
			{
				if (state == NodeProcessState.EXITING)
				{
					// The process is exiting - respawn:
					Spawn();
					return;
				}

				if (state == NodeProcessState.FAILED)
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

				if (state != NodeProcessState.READY)
				{
					return;
				}

				// Start the UI watcher straight away:
				Node.Request(new { action = "watch" }, (string e, JObject response) => {
					if (e != null)
					{
						return;
					}

					Console.WriteLine("UI watcher started successfully.");

				});

			};

			// Add to the list of processes currently waiting to connect:
			PendingProcesses.Add(process);

			// Start it now:
			process.Start();
			

		}
		
	}
    
}
