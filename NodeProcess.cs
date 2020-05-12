using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Api.StackTools
{
	
	/// <summary>
	/// The current state of a node process.
	/// </summary>
	public enum NodeProcessState : int
	{
		/// <summary>
		/// Exiting
		/// </summary>
		EXITING = 0,
		/// <summary>
		/// Starting
		/// </summary>
		STARTING = 1,
		/// <summary>
		/// Dropped
		/// </summary>
		FAILED = 2,
		/// <summary>
		/// Ready to go
		/// </summary>
		READY = 3
	}
	
	/// <summary>
	/// An instance of a node.js process
	/// </summary>
	public partial class NodeProcess
	{
		/// <summary>
		/// An ID to use to identify node processes.
		/// </summary>
		private static int SpawnId = 1;

		/// <summary>
		/// This process's ID.
		/// </summary>
		public int Id;

		/// <summary>
		/// Sets up a new process.
		/// cmd is of the form "socialstack interactive -p .."
		/// </summary>
		/// <param name="cmd">The socialstack command to spawn.</param>
		/// <param name="appendId">True if the process Id should be appended to the command, in the form " -id ID"</param>
		public NodeProcess(string cmd, bool appendId)
		{
			Id = SpawnId++;

			if (appendId)
			{
				cmd += " -id " + Id;
			}

			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			
			Process process = new Process();
			
			// Configure the process using the StartInfo properties.
			process.StartInfo.FileName = isWindows ? "cmd.exe" : "/bin/bash";
			process.StartInfo.Arguments = isWindows ? "/C " + cmd : "-c \"" + cmd + "\"";
			process.StartInfo.WorkingDirectory = "";
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.ErrorDialog = false;
			process.StartInfo.CreateNoWindow = true;
			process.EnableRaisingEvents = true;
			
			SetNodeProcess(process);
		}
		
		/// <summary>
		/// The system process.
		/// </summary>
		public Process Process;
		
		/// <summary>
		/// How full the freeSlots array currently is.
		/// </summary>
		private int SlotFill = 0;

		/// <summary>
		/// Indicies in the callback array which are currently unused.
		/// </summary>
		private int[] FreeSlots;

		/// <summary>
		/// Pending callbacks.
		/// </summary>
		private OnStackToolsResponse[] PendingRequests;

		/// <summary>
		/// Kills the underlying process.
		/// </summary>
		public void Kill()
		{
			if (Process.HasExited)
			{
				Process = null;
				return;
			}
			Process?.Kill();
		}

		/// <summary>
		/// Gets the handler for a particular request ID.
		/// </summary>
		/// <param name="requestId"></param>
		/// <returns></returns>
		public OnStackToolsResponse GetResponseHandler(int requestId)
		{
			OnStackToolsResponse responseHandler;

			lock (PendingRequests)
			{
				responseHandler = PendingRequests[requestId];
				PendingRequests[requestId] = null;
				FreeSlots[SlotFill] = requestId;
				SlotFill++;
			}

			return responseHandler;
		}

		/// <summary>
		/// Obtains a request ID to use.
		/// </summary>
		/// <returns></returns>
		public int GetRequestId(OnStackToolsResponse onResponse)
		{
			if (SlotFill == 0)
			{
				// Gain another 10 slots:
				Resize(FreeSlots.Length + 10);
			}

			SlotFill--;
			var requestId = FreeSlots[SlotFill];
			PendingRequests[requestId] = onResponse;

			return requestId;
		}

		/// <summary>
		/// The link has collapsed and the process should now exit.
		/// </summary>
		public void Collapse()
		{
			for (var i = 0; i < PendingRequests.Length; i++)
			{
				PendingRequests[i]?.Invoke("Node process data stream went out of sync", null);
			}
		}

		/// <summary>
		/// Resizes the pending arrays because they're full, or because this is the first setup.
		/// </summary>
		/// <param name="newSize"></param>
		private void Resize(int newSize)
		{
			var newPR = new OnStackToolsResponse[newSize];
			var newFR = new int[newSize];
			var oldSize = 0;

			lock (this)
			{
				if (PendingRequests != null)
				{
					oldSize = PendingRequests.Length;
					Array.Copy(PendingRequests, 0, newPR, 0, oldSize);
					Array.Copy(FreeSlots, 0, newFR, 0, oldSize);
				}

				PendingRequests = newPR;
				FreeSlots = newFR;
				SlotFill += newSize - oldSize;

				for (var i = oldSize; i < newSize; i++)
				{
					FreeSlots[i] = i;
				}
			}


		}
		
		/// <summary>
		/// Sets the node process.
		/// </summary>
		/// <param name="process"></param>
		private void SetNodeProcess(Process process)
		{
			Process = process;

			// Setup initial buffer size (5 parallel requests at once - resizes if it needs to).
			Resize(5);

			// Hook output/ error data received event:
			process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}

				// Forward to our output stream:
				Console.WriteLine(e.Data);
			});

			process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}

				Console.WriteLine(e.Data);
			});

		}
		
		/// <summary>
		/// Called when the process has changed state. 0=Exiting, 1=Connecting, 2=Connected
		/// </summary>
		public event Action<NodeProcessState> OnStateChange;
		
		/// <summary>
		/// Trigger a state change.
		/// </summary>
		/// <param name="state"></param>
		public void StateChange(NodeProcessState state)
		{
			OnStateChange?.Invoke(state);
		}
		
		/// <summary>
		/// Attempts to start the process. Will output to stdout any messages that occur
		/// and raise a StateChange event if it doesn't start.
		/// </summary>
		public void Start()
		{
			StateChange(NodeProcessState.STARTING);
			
			try
			{
				Process.Start();

				Task.Run(() => {
					Process.BeginOutputReadLine();
					Process.BeginErrorReadLine();
					Process.WaitForExit();
				});

			}
			catch (Exception e)
			{
				Console.WriteLine("[WARN] Caught this whilst trying to start socialstack tools:");
				Console.WriteLine(e.ToString());
				
				StateChange(NodeProcessState.FAILED);
			}

		}

	}
	
}