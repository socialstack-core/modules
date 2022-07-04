using Api.ContentSync;
using Api.SocketServerLibrary;

namespace Api.WebSockets;

/// <summary>
/// Extending the websocket room type meta.
/// </summary>
public partial class NetworkRoomTypeMeta
{
	/// <summary>
	/// The opcode
	/// </summary>
	public OpCode OpCode;

	/// <summary>
	/// Reads an object of this type from the given client.
	/// </summary>
	/// <param name="opcode"></param>
	/// <param name="client"></param>
	/// <param name="remoteType"></param>
	/// <param name="action"></param>
	/// <returns></returns>
	public virtual void Handle(OpCode<SyncServerRemoteType> opcode, Client client, SyncServerRemoteType remoteType, int action)
	{
	}

}
