using System.Net;


namespace Api.ContentSync
{
	/// <summary>
	/// Another ContentSync server in the cluster connected to "us".
	/// </summary>
	public class ContentSyncServerInfo
	{
		/// <summary>
		/// The remote DNS address (or IP).
		/// </summary>
		public string RemoteAddress;
		/// <summary>
		/// CSync server port on remote host.
		/// </summary>
		public int Port = 12020;
		/// <summary>
		/// Remote server's declared ID.
		/// </summary>
		public int ServerId;
		/// <summary>
		/// The IP this server should bind to. Will almost always be on a private LAN, but * is also supported.
		/// </summary>
		public IPAddress BindAddress = IPAddress.Loopback;
	}
}