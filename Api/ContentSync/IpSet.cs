using Api.ContentSync;


namespace Api.Startup;


/// <summary>
/// Extensions to ip set for contentSync.
/// </summary>
public partial class IpSet
{
	
	/// <summary>
	/// Copies the IPs into the given server.
	/// </summary>
	/// <param name="server"></param>
	/// <returns></returns>
	public void CopyTo(ClusteredServer server)
	{
		server.PublicIPv4 = PublicIPv4?.GetAddressBytes();
		server.PublicIPv6 = PublicIPv6?.GetAddressBytes();
		server.PrivateIPv4 = PrivateIPv4?.GetAddressBytes();
		server.PrivateIPv6 = PrivateIPv6?.GetAddressBytes();
	}

	/// <summary>
	/// True if the IPs in this set have changed from the ones in the given server.
	/// </summary>
	/// <param name="server"></param>
	/// <returns></returns>
	public bool ChangedSince(ClusteredServer server)
	{
		if (!Match(server.PublicIPv4, PublicIPv4))
		{
			return true;
		}

		if (!Match(server.PublicIPv6, PublicIPv6))
		{
			return true;
		}

		if (!Match(server.PrivateIPv4, PrivateIPv4))
		{
			return true;
		}

		if (!Match(server.PrivateIPv6, PrivateIPv6))
		{
			return true;
		}
		
		return false;

	}

}