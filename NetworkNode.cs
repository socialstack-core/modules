using Api.Database;


namespace Api.NetworkNodes;

/// <summary>
/// A node on the network.
/// </summary>
[DatabaseField(Group = "host")]
public partial class NetworkNode : Content<uint>
{
	/// <summary>
	/// The capabilities of this node in relation to its ability to submit transactions or be the BAS.
	/// A node joining the network has no capabilities (is recv only) unless the BAS trusts it immediately.
	/// Valid values include "*", "assemble", "submit", "assemble,submit".
	/// </summary>
	public string Capabilities = "";
	
	/// <summary>
	/// The port number used for Lumity services on this node.
	/// </summary>
	public int Port;

	/// <summary>
	/// Version information for this node. 0 indicates that it is a secp256k1 keypair using SHA3 digest signatures.
	/// </summary>
	public uint Version;

	/// <summary>
	/// The public key for this node.
	/// </summary>
	public byte[] PublicKey;

	/// <summary>
	/// The subnetwork this node is in. This is used to ignore nodes in other environments when copying a chain.
	/// Common values are dev, stage, prod.
	/// </summary>
	[DatabaseField(Length = 20)]
	public string Environment;
	
	/// <summary>
	/// Hostname of this node. Used to identify if a node has been seen before.
	/// </summary>
	[DatabaseField(Length=80)]
	public string HostName;
	
	/// <summary>
	/// DNS address for public internet traffic. Can also be an IPV4/ IPV6 address.
	/// </summary>
	public string PublicAddress;
	
	/// <summary>
	/// DNS address for local (LAN only) traffic. Can also be an IPV4/ IPV6 address.
	/// </summary>
	public string LocalAddress;
}