using Api.SocketServerLibrary;
using System.Net;


namespace Api.ContentSync
{
	/// <summary>
	/// Initial hello message
	/// </summary>
	public class SyncServerHandshake : Message<SyncServerHandshake>
	{
		/// <summary>
		/// Other server's ID ("mine" when sending this message).
		/// </summary>
		public uint ServerId;
		/// <summary>
		/// Signature of A=>B
		/// </summary>
		public string Signature;
	}
}