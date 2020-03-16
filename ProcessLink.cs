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
	/// A link to a particular process.
	/// </summary>
	public class ProcessLink
	{
		/// <summary>
		/// ID was received.
		/// </summary>
		public const int STATE_ID = 4;
		
		/// <summary>
		/// The socket link.
		/// </summary>
		private Socket SocketLink;
		
		/// <summary>
		/// Creates a new request queue.
		/// </summary>
		public ProcessLink(Socket socket, Action onReady)
		{
			SocketLink = socket;
			CurrentBuffer = HeaderBuffer;
			OnReady = onReady;
			ReceiveTcp();
		}
		
		private int BytesReceived = 0;
		private byte[] HeaderBuffer = new byte[7];
		private byte[] CurrentBuffer;
		private int ReceiveRequestIndex;

		/// <summary>
		/// An ID we assign to identify the process that is connecting to us.
		/// </summary>
		public int Id = -1;

		/// <summary>
		/// Info for the process itself.
		/// </summary>
		public NodeProcess Process;

		private void ReceiveTcp()
		{
			SocketLink.BeginReceive(CurrentBuffer, BytesReceived, CurrentBuffer.Length - BytesReceived, SocketFlags.None, OnReceivedTcp, null);
		}
		
		private void LinkOutOfSync()
		{
			// The socket link bytestream went out of sync.
			// Kill all pending requests, as well as the process.
			Process?.Collapse();
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
			if (Process != null)
			{
				Process.Kill();
				Process = null;
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
			LinkOutOfSync();
		}

		private Action OnReady;

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

					// Opcode - either 1 or 2 (2 = heartbeat):
					if (CurrentBuffer[0] != 1)
					{
						if (CurrentBuffer[0] == 2)
						{
							// Wait for another header
							BytesReceived = 0;
							ReceiveTcp();
							return;
						}
						else if (Id == -1 && CurrentBuffer[0] == 3)
						{
							// 4 byte ID (Not a OS process ID - we give it the ID to tell us).
							Id = CurrentBuffer[1] | (CurrentBuffer[2] << 8) | (CurrentBuffer[3] << 16) | (CurrentBuffer[4] << 24);

							OnReady?.Invoke();

							// Wait for another header
							BytesReceived = 0;
							ReceiveTcp();
							return;
						}

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
				var responseHandler = Process?.GetResponseHandler(requestId);
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
				var requestId = Process.GetRequestId(onResponse);
				
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
