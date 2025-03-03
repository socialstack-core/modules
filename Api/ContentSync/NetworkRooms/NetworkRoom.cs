using System;
using System.Threading.Tasks;
using Api.ContentSync;
using Api.Contexts;
using Api.Database;
using Api.SocketServerLibrary;
using Api.Startup;

namespace Api.WebSockets;

/// <summary>
/// Stores information for a particular network room for this particular server only.
/// You can also add e.g. room state by creating a parent class.
/// </summary>
public partial class NetworkRoom<T, ID, ROOM_ID> : NetworkRoom
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	where ROOM_ID : struct, IConvertible, IEquatable<ROOM_ID>, IComparable<ROOM_ID>
{
	/// <summary>
	/// Sends a message to all users in this room except the given sender. The message being sent MUST have a network room prefix.
	/// </summary>
	/// <returns></returns>
	public void Send(Writer message, WebSocketClient sender = null)
	{
		if (message.FirstBuffer == null || message.FirstBuffer.Bytes[0] != 8)
		{
			throw new Exception("Incorrectly attempted to send a writer to a network room. The writer must originate from the networkRoom.StartSend() method.");
		}

		SendLocally(message, sender);
		SendRemote(message);
	}

	/// <summary>
	/// Send to remote servers only.
	/// </summary>
	/// <param name="message"></param>
	public void SendRemote(Writer message)
	{
		var remotes = GetRemoteServers();

		if (remotes != null)
		{
			if (_contentSync == null)
			{
				_contentSync = Services.Get<ContentSyncService>();
			}

			/*
			var currentRemoteServer = remotes.First;
			while (currentRemoteServer != null)
			{
				var mapping = currentRemoteServer.Current;

				if (mapping != null)
				{
					// Send to this server by ID.
					var server = _contentSync.GetServer(mapping.TargetId);

					if (server != null)
					{
						server.Send(message);
					}
				}
				currentRemoteServer = currentRemoteServer.Next;
			}
			*/
		}

	}

	private static ContentSyncService _contentSync;
}