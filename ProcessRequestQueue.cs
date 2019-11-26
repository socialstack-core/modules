using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace Api.StackTools
{
	/// <summary>
	/// A queue of requests to a particular process.
	/// </summary>
	public class ProcessRequestQueue
	{
		/// <summary>
		/// The localhost TCP port the nodejs process will listen to.
		/// </summary>
		private int Port;

		/// <summary>
		/// Single shared "socialstack interactive" process.
		/// </summary>
		private Process NodeProcess;

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
		/// The socket link.
		/// </summary>
		private Socket SocketLink;

		/// <summary>
		/// Set to true if we successfully connected at some point.
		/// </summary>
		internal bool HasListened;

		/// <summary>
		/// Creates a new request queue.
		/// </summary>
		/// <param name="process"></param>
		/// <param name="port">TCP port to listen on</param>
		public ProcessRequestQueue(Process process, int port)
		{
			Port = port;
			NodeProcess = process;

			// Setup initial buffer size (5 parallel requests at once - resizes if it needs to).
			Resize(5);
			
			// Hook output data received event:
			process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (String.IsNullOrEmpty(e.Data))
				{
					return;
				}

				if (SocketLink == null && e.Data == "[NodeReadyForConnections]") {

					// The node.js process has told us that it's ready for us to connect.
					// It's listening on a port.

					// Failures around this are likely because we either don't have permission to listen on the port
					// or there's some orphaned nodejs process which is still listening on that port
					// or some other process happened to be using it anyway.
					ConnectTcp();

					return;
				}

				// Forward to our output stream:
				Console.WriteLine(e.Data);
			});

			process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
			{
				if (String.IsNullOrEmpty(e.Data))
				{
					return;
				}

				Console.WriteLine(e.Data);
			});

		}

		/// <summary>
		/// Attempts to start the process. Will output to stdout any messages that occur
		/// and raise a StateChange event if it doesn't start.
		/// </summary>
		public void Start()
		{

			try
			{
				NodeProcess.Start();

				Task.Run(() => {
					NodeProcess.BeginOutputReadLine();
					NodeProcess.BeginErrorReadLine();
					NodeProcess.WaitForExit();

					if (!HasListened)
					{
						// Exiting (non-starter):
						OnStateChange(3);
					}
				});

			}
			catch (Exception e)
			{
				Console.WriteLine("[WARN] Caught this whilst trying to start socialstack tools:");
				Console.WriteLine(e.ToString());

				// Exiting (non-starter):
				OnStateChange?.Invoke(3);
			}

		}

		/// <summary>
		/// Called when the process has changed state. 0=Exiting, 1=Connecting, 2=Connected
		/// </summary>
		public event Action<int> OnStateChange;

		private int BytesReceived = 0;
		private byte[] HeaderBuffer = new byte[7];
		private byte[] CurrentBuffer;
		private int ReceiveRequestIndex;

		private void ReceiveTcp()
		{
			SocketLink.BeginReceive(CurrentBuffer, BytesReceived, CurrentBuffer.Length - BytesReceived, SocketFlags.None, OnReceivedTcp, null);
		}

		/// <summary>
		/// Trigger a state change.
		/// </summary>
		/// <param name="state"></param>
		public void StateChange(int state)
		{
			OnStateChange?.Invoke(state);
		}

		private void LinkOutOfSync()
		{
			// The socket link bytestream went out of sync.
			// We have to kill all pending requests, but the process can remain up.

			SocketLink.Close();
			SocketLink = null;

			for (var i = 0; i < PendingRequests.Length; i++)
			{
				PendingRequests[i]?.Invoke("Node process data stream went out of sync", null);
			}
			
			ConnectTcp();

		}

		/// <summary>
		/// Call this to kill the process.
		/// </summary>
		public void Stop()
		{
			if (SocketLink != null)
			{
				SocketLink.Shutdown(SocketShutdown.Both);
				SocketLink = null;
			}
			if (NodeProcess != null)
			{
				NodeProcess.Kill();
				NodeProcess = null;
			}
		}

		/// <summary>
		/// This happens when the node.js process has failed and collapsed.
		/// This whole PRQ must reject requests.
		/// </summary>
		private void NodeDisconnected()
		{
			SocketLink.Close();
			SocketLink = null;
			NodeProcess = null;

			OnStateChange?.Invoke(0);

			for (var i = 0; i < PendingRequests.Length; i++)
			{
				PendingRequests[i]?.Invoke("Node process has failed", null);
			}
			
		}

		private void OnReceivedTcp(IAsyncResult result)
		{
			int rcv = 0;

			if (SocketLink == null)
			{
				// Disconnect from this end.
				return;
			}

			try
			{
				rcv = SocketLink.EndReceive(result);
			}
			catch (Exception e)
			{
				Console.WriteLine("[WARN] Attempting automatic recovery from this error: " + e.ToString());
				NodeDisconnected();
				return;
			}

			if (rcv == 0)
			{
				NodeDisconnected();
				return;
			}

			BytesReceived += rcv;

			if (BytesReceived == CurrentBuffer.Length)
			{
				if (CurrentBuffer == HeaderBuffer)
				{
					// Receiving the header.

					// Opcode (should be 1):
					if (CurrentBuffer[0] != 1)
					{
						LinkOutOfSync();
						return;
					}

					// 4 byte JSON payload length:
					int length = CurrentBuffer[1] | (CurrentBuffer[2] << 8) | (CurrentBuffer[3] << 16) | (CurrentBuffer[4] << 24);

					// 2 byte request index:
					ReceiveRequestIndex = CurrentBuffer[5] | (CurrentBuffer[6] << 8);

					// (Ideally pool these):
					BytesReceived = 0;
					CurrentBuffer = new byte[length];
				}
				else
				{
					// Received a JSON string.
					var jsonPayload = System.Text.Encoding.UTF8.GetString(CurrentBuffer);

					BytesReceived = 0;
					CurrentBuffer = HeaderBuffer;
					
					GotResponse(ReceiveRequestIndex, jsonPayload);
				}

			}

			// Wait for the next payload:
			ReceiveTcp();
		}

		/// <summary>
		/// Called when Node.js has responded to a particular request by its ID.
		/// </summary>
		/// <param name="requestId"></param>
		/// <param name="json"></param>
		private void GotResponse(int requestId, string json) {

			try
			{
				OnStackToolsResponse responseHandler;

				lock (PendingRequests)
				{
					responseHandler = PendingRequests[requestId];
					PendingRequests[requestId] = null;
					FreeSlots[SlotFill] = requestId;
					SlotFill++;
				}

				var result = JsonConvert.DeserializeObject(json) as JObject;

				// Run the handler now:
				responseHandler?.Invoke(null, result);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error in node.js response handler:");
				Console.WriteLine(e.ToString());
			}

		}

		private void ConnectTcp() {
			OnStateChange?.Invoke(1);
			
			// Connect the TCP socket now:
			SocketLink = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			SocketLink.BeginConnect("localhost", Port, (IAsyncResult result) => {

				try
				{
					SocketLink.EndConnect(result);

					if (SocketLink.Connected)
					{
						// Great - we're ready to send/ receive messages now.
						HasListened = true;
						CurrentBuffer = HeaderBuffer;
						BytesReceived = 0;
						ReceiveTcp();
						OnStateChange?.Invoke(2);

					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Was not able to establish a connection to the socialstack command line service on node.js");
					throw ex;
				}

			}, null);

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
		/// Submits a request to the running shared process. When it's done, it runs your given callback.
		/// </summary>
		/// <param name="serialisableRequestData">The raw request data</param>
		/// <param name="onResponse">Called when the request returns.</param>
		public void Request(object serialisableRequestData, OnStackToolsResponse onResponse)
		{
			// Serialise the request as JSON:
			var jsonString = JsonConvert.SerializeObject(serialisableRequestData);

			RequestJson(jsonString, onResponse);
		}

		/// <summary>
		/// Submits a request to the running shared process. When it's done, it runs your given callback.
		/// </summary>
		/// <param name="json">The raw json data</param>
		/// <param name="onResponse">Called when the request returns.</param>
		public void RequestJson(string json, OnStackToolsResponse onResponse)
		{
			var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

			// Get the length and write it to the stream:
			var length = jsonBytes.Length;
			
			if (SocketLink == null || !SocketLink.Connected)
			{
				// It crashed or hasn't started yet - error immediately:
				onResponse("Node process unavailable", null);
				return;
			}
			
			lock (SocketLink)
			{
				// Pop an index:
				if (SlotFill == 0)
				{
					// Gain another 10 slots:
					Resize(FreeSlots.Length + 10);
				}

				SlotFill--;
				var requestId = FreeSlots[SlotFill];
				PendingRequests[requestId] = onResponse;

				var header = new byte[7];
				header[0] = 1;
				header[1] = (byte)(length & 255);
				header[2] = (byte)((length >> 8) & 255);
				header[3] = (byte)((length >> 16) & 255);
				header[4] = (byte)((length >> 24) & 255);
				header[5] = (byte)(requestId & 255);
				header[6] = (byte)((requestId >> 8) & 255);
				
				// Write the header and the JSON request object:
				SocketLink.Send(header);
				SocketLink.Send(jsonBytes);
			}
		}


	}
}
