namespace Api.Startup;

/// <summary>
/// Host details used by the monitoring system (if in use).
/// </summary>
public class HostDetails
{
	/// <summary>
	/// Server hostname or other unique identifying feature.
	/// </summary>
	public string HostName;

	/// <summary>
	/// The env name.
	/// </summary>
	public string Environment;

	/// <summary>
	/// This is the host key in host update, and the project host key in host create.
	/// </summary>
	public string Key;

	/// <summary>
	/// A region or group within a region.
	/// </summary>
	public uint HostGroupId;

	/// <summary>
	/// The type of server. Loadbalancers are special because they will receive a list of IPs.
	/// </summary>
	public uint ServerType;

	/// <summary>
	/// Private ip
	/// </summary>
	public string PrivateIPv4;

	/// <summary>
	/// Private ip (optional)
	/// </summary>
	public string PrivateIPv6;
	
	/// <summary>
	/// Public ip
	/// </summary>
	public string IPv4;
	
	/// <summary>
	/// Public ip
	/// </summary>
	public string IPv6;
	
	/// <summary>
	/// CPU usage
	/// </summary>
	public double Cpu; // 0-1
	
	/// <summary>
	/// CPU core count
	/// </summary>
	public uint CoreCount; // number of cores
	
	/// <summary>
	/// RAM usage
	/// </summary>
	public double Ram; // 0-1

	/// <summary>
	/// amount of ram available
	/// </summary>
	public ulong RamMax; // in mb

	/// <summary>
	/// Disk storage usage
	/// </summary>
	public double Storage; // 0-1
	
	/// <summary>
	/// amount of storage available
	/// </summary>
	public ulong StorageMax; // in gb

}