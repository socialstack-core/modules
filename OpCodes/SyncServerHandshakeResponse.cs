using Api.SocketServerLibrary;
using System.Net;


namespace Api.ContentSync
{
	/// <summary>
	/// Response to handshake
	/// </summary>
	public class SyncServerHandshakeResponse : Message<SyncServerHandshakeResponse>
	{
		/// <summary>
		/// Other server's ID ("mine" when sending this message).
		/// </summary>
		public uint ServerId;
	}

	/// <summary>
	/// Used when remote content changed in some way.
	/// </summary>
	public class SyncServerRemoteType : Message<SyncServerRemoteType>
	{
		/// <summary>
		/// Locale ID.
		/// </summary>
		public uint LocaleId;

		/// <summary>
		/// Type name.
		/// </summary>
		public string TypeName;

		/// <summary>
		/// Bytes remaining in the message.
		/// </summary>
		public uint RemainingBytes;

		/// <summary>
		/// The metadata for the current object.
		/// </summary>
		public ContentSyncTypeMeta Meta;
	}
}