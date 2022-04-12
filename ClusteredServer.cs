using Api.Database;


namespace Api.ContentSync
{
	/// <summary>
	/// A server in the cluster. Each one owns one stripe, and they adjust (globally) when a new server joins the cluster.
	/// </summary>
	public class ClusteredServer : Content<uint>
	{
		/// <summary>
		/// The port number used for contentsync on this server.
		/// </summary>
		public int Port;

		/// <summary>
		/// The environment this server is in.
		/// </summary>
		[DatabaseField(Length = 20)]
		public string Environment;

		/// <summary>
		/// Private IPv4 address for this server.
		/// </summary>
		[DatabaseField(Length=4)]
		public byte[] PrivateIPv4;
		
		/// <summary>
		/// Public IPv4 address for this server.
		/// </summary>
		[DatabaseField(Length=4)]
		public byte[] PublicIPv4;
		
		/// <summary>
		/// Private IPv6 address for this server. Often matches the public one.
		/// </summary>
		[DatabaseField(Length=16)]
		public byte[] PrivateIPv6;
		
		/// <summary>
		/// Public IPv6 address for this server.
		/// </summary>
		[DatabaseField(Length=16)]
		public byte[] PublicIPv6;
		
		/// <summary>
		/// Hostname of this server. Used to identify if a server has been seen before.
		/// </summary>
		[DatabaseField(Length=80)]
		public string HostName;
		
		/// <summary>
		/// Socialstack server type. Often 0 if undeclared.
		/// </summary>
		public uint ServerTypeId;

		/// <summary>
		/// Socialstack host platform ID. Often 0 if undeclared.
		/// </summary>
		public uint HostPlatformId;

		/// <summary>
		/// Socialstack region ID for a given host platform.
		/// </summary>
		public uint RegionId;

	}

}