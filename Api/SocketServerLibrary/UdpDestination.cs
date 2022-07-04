namespace Api.SocketServerLibrary;


/// <summary>
/// Stores some general data for UDP packet servers.
/// This is one instance per server.
/// </summary>
public class UdpDestination
{

	/// <summary>
	/// The port number this server is on
	/// </summary>
	public int Port;

	/// <summary>
	/// Established when the first raw IpV4 UDP packet is received here.
	/// </summary>
	public byte[] PortAndIpV4;

	/// <summary>
	/// Established when the first raw IpV6 UDP packet is received here.
	/// </summary>
	public byte[] PortAndIpV6;

}