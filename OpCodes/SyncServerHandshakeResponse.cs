using Api.SocketServerLibrary;
using System.Net;


namespace Api.ContentSync
{
	/// <summary>
	/// Response to handshake
	/// </summary>
	public class SyncServerHandshakeResponse : Message
	{
		/// <summary>
		/// Other server's ID ("mine" when sending this message).
		/// </summary>
		public int ServerId;
	}
}