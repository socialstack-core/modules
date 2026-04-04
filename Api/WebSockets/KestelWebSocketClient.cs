using Api.SocketServerLibrary;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Api.WebSockets;

/// <summary>
/// A kestrel specific websocket client.
/// </summary>
public class KestrelWebSocketClient : WebSocketClient {

	private WebSocket _ws;

	/// <summary>
	/// A kestrel specfic WS client.
	/// </summary>
	/// <param name="ws"></param>
	public KestrelWebSocketClient(WebSocket ws)
	{
		_ws = ws;
	}

	/// <summary>
	/// Sends the given writer async to this ws.
	/// </summary>
	/// <param name="writer"></param>
	/// <returns></returns>
	public override async ValueTask SendAsync(Writer writer)
	{
		var buffer = writer.FirstBuffer;

		while (buffer != null)
		{
			var isLast = buffer.After == null;

			await _ws.SendAsync(
				new ArraySegment<byte>(buffer.Bytes, buffer.Offset, buffer.Length),
				WebSocketMessageType.Binary,
				isLast,
				CancellationToken.None);

			buffer = buffer.After;
		}
	}

	/// <summary>
	/// Starts a receive task for this WS.
	/// </summary>
	/// <returns></returns>
	public async Task ReceiveAsync()
	{
		while (true)
		{
			// todo (handle received bytes)

			if (Last == null || Last.Offset == Last.Length)
			{
				// Buffer is full, or there isn't one.
				// Get another buffer.
				var next = BinaryBufferPool.OneKb.Get();
				next.After = null;

				if (Last == null)
				{
					First = next;
				}
				else
				{
					Last.After = next;
				}

				Last = next;
			}

			var receiveResult = await _ws.ReceiveAsync(
				new ArraySegment<byte>(Last.Bytes, Last.Offset, Last.Length), CancellationToken.None);

			if (receiveResult.CloseStatus.HasValue)
			{
				// Client DC.
				await ClientDisconnectedEvent();

				await _ws.CloseAsync(
					receiveResult.CloseStatus.Value,
					receiveResult.CloseStatusDescription,
					CancellationToken.None
				);
				break;
			}

			var bytesRead = receiveResult.Count;

			Last.Offset += bytesRead;
			BytesInBuffer += bytesRead;

			// Process the stack next.
			while (BytesInBuffer >= RecvStack[RecvStackPointer].BytesRequired)
			{
				if (Socket == null)
				{
					// Process requested the socket to close.
					return;
				}

				// Ask the top of stack to process the data:
				RecvStack[RecvStackPointer].Reader.Process(ref RecvStack[RecvStackPointer], this);

				if (WaitForTaskBeforeReceive != null && !WaitForTaskBeforeReceive.IsCompleted)
				{
					// The task will call TaskCompletedContinueReceive when it is done.
					return;
				}
			}
		}
	}

}