using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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
		/// Sets up a new process.
		/// cmd is of the form "socialstack interactive -p .."
		/// </summary>
		/// <param name="cmd">The socialstack command to spawn.</param>
		/// <param name="customOutputHandlers">True if you'd like to add custom data/ error handlers to the process.</param>
		public NodeProcess(string cmd, bool customOutputHandlers = false)
		{
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

			_serializerConfig = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};
			
			if(customOutputHandlers){
				Process = process;
			}else{
				SetNodeProcess(process);
			}
		}

		/// <summary>
		/// Used by the serializer when sending JSON to the node process.
		/// </summary>
		private readonly JsonSerializerSettings _serializerConfig;

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

				OnStackToolsResponse responseHandler = null;
				JObject response = null;

				// Objects only for the response message type:
				if (e.Data[0] == '{')
				{
					try
					{
						response = JsonConvert.DeserializeObject(e.Data) as JObject;

						if (response != null)
						{
							var idField = response["_id"];

							if (idField != null)
							{
								var requestId = idField.Value<int>();
								responseHandler = GetResponseHandler(requestId);
							}
						}
					}
					catch
					{
						// Wasn't json (or a valid message) - just write it out.
						Log.Info("stacktools", e.Data);
					}
				}

				if (responseHandler == null)
				{
					if (OnData != null)
					{
						OnData(e.Data);
						return;
					}

					Log.Info("stacktools", e.Data);
					return;
				}

				// Valid message - run now:
				responseHandler.Invoke(null, response);
			});

			process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				// Responses pass through here. Check if the msg is JSON.
				
				if (string.IsNullOrEmpty(e.Data))
				{
					return;
				}

				OnStackToolsResponse responseHandler = null;
				JObject response = null;

				// Objects only for the response message type:
				if (e.Data[0] == '{')
				{
					try
					{
						response = JsonConvert.DeserializeObject(e.Data) as JObject;

						if (response != null)
						{
							var idField = response["_id"];

							if (idField != null)
							{
								var requestId = idField.Value<int>();
								responseHandler = GetResponseHandler(requestId);
							}
						}
					}
					catch
					{
						// Wasn't json (or a valid message) - just write it out (happens below).
					}
				}

				if (responseHandler == null)
				{
					if (OnErrorData != null)
					{
						OnErrorData(e.Data);
						return;
					}

					Console.WriteLine(e.Data);
					return;
				}

				// Valid message - run now:
				responseHandler.Invoke(null, response);
			});

		}

		/// <summary>
		/// Called when error data is received.
		/// </summary>
		public event Action<string> OnErrorData;

		/// <summary>
		/// Called when stdout data is received.
		/// </summary>
		public event Action<string> OnData;

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
		/// Runs synchronously.
		/// </summary>
		public void StartSync()
		{
			StateChange(NodeProcessState.STARTING);
			RunSync();
		}

		private void RunSync()
		{
			Process.Start();
			Process.BeginOutputReadLine();
			Process.BeginErrorReadLine();
			StateChange(NodeProcessState.READY);
			Process.WaitForExit();
			StateChange(NodeProcessState.EXITING);
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
				Task.Run(() => {
					RunSync();
				});
			}
			catch (Exception e)
			{
				Log.Warn("stacktools", e, "Error whilst trying to run socialstack tools");
				
				StateChange(NodeProcessState.FAILED);
				StateChange(NodeProcessState.EXITING);
			}

		}

		/// <summary>
		/// Submits a request to the running shared process. When it's done, it runs your given callback.
		/// </summary>
		/// <param name="request">The raw request data</param>
		/// <param name="onResponse">Called when the request returns.</param>
		public void Request(Request request, OnStackToolsResponse onResponse)
		{
			lock (this)
			{
				// Pop an ID:
				request._id = GetRequestId(onResponse);

				// Serialise the request as JSON:
				var jsonString = JsonConvert.SerializeObject(request, _serializerConfig);

				// Write to stdin:
				Process.StandardInput.Write(jsonString);
				Process.StandardInput.Flush();
			}
		}
	}
}