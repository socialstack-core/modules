using Api.Contexts;
using Api.Eventing;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Api.Startup
{
	/// <summary>
	/// Utility mechanism for automatic IP discovery. Occurs when a server starts to check that its public record is correct.
	/// </summary>
	public static class IpDiscovery
	{
		/// <summary>
		/// Ipv4 helper site (in future, will be provided by socialstack cloud).
		/// </summary>
		private static readonly string IPv4Site="https://ipv4.icanhazip.com/";

		/// <summary>
		/// Ipv6 helper site (in future, will be provided by socialstack cloud).
		/// </summary>
		private static readonly string IPv6Site="https://ipv6.icanhazip.com/";
		
		
		private static async Task<string> GetStringAsync(string url) {
			// No need to reuse this client
			using (var client = new HttpClient())
			{
				return await client.GetStringAsync(url);
			}
		}
		
		/// <summary>Finds the public IPv4 address.</summary>
		public static async Task<IPAddress> FindPublicAddress(string addr){
			
			string httpResult=await GetStringAsync(addr);
			
			if(string.IsNullOrEmpty(httpResult)){
				return null;
			}

			// So far so good:
			if (!IPAddress.TryParse(httpResult.Trim(), out IPAddress res))
			{
				return null;
			}

			return res;
		}

		/// <summary>
		/// Discovers preferred private LAN IPs and puts them into the given set.
		/// </summary>
		/// <param name="ips"></param>
		/// <param name="v6"></param>
		public static void DiscoverPrivateIps(IpSet ips, bool v6 = true)
		{
			// A computer might have multiple network interfaces.
			// and we can't know what its preferred IP is without just trying to use it. 
			// Note that this doesn't actually send any network packets, but will expose the preferred local IP for us.
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
			{
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				ips.PrivateIPv4 = endPoint.Address;
			}

			if (v6)
			{
				try
				{
					using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, 0))
					{
						socket.Connect("2001:4860:4860::8888", 65530);
						IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
						ips.PrivateIPv6 = endPoint.Address;
					}
				}
				catch
				{
					// Ipv6 unsupported
					Log.Warn("ip", "Ipv6 appears to be unsupported or not configured.");
				}
			}
		}

		private static IpSet _discoveredSet;

		/// <summary>Gets the IP set.</summary>
		public static async Task<IpSet> Discover(bool forceFresh = false)
		{
			if (_discoveredSet != null && !forceFresh)
			{
				return _discoveredSet;
			}
			
			var ips = new IpSet();

			// Find public addresses:
			try
			{
				ips.PublicIPv4 = await FindPublicAddress(IPv4Site);
			}
			catch
			{
				Log.Warn("ip", "Unable to collect public IPv4 address. This server will not be part of a cluster.");
			}
			
			var ipv6 = true;

			try
			{
				ips.PublicIPv6 = await FindPublicAddress(IPv6Site);
			}
			catch
			{
				// Ipv6 unsupported
				Log.Warn("ip", "Ipv6 appears to be unsupported or not configured.");
				ipv6 = false;
			}

			// Find private addresses.
			DiscoverPrivateIps(ips, ipv6);

			ips = await Events.Service.AfterDiscoverIPs.Dispatch(new Context(), ips);
			_discoveredSet = ips;
			return ips;
		}
	}
	
	/// <summary>
	/// A group of 4 IP addresses. Some can be null.
	/// </summary>
	public partial class IpSet
	{
		/// <summary>Public IPv4</summary>
		public IPAddress PublicIPv4;
		
		/// <summary>Public IPV6</summary>
		public IPAddress PublicIPv6;
		
		/// <summary>First "most appropriate" private IPv4</summary>
		public IPAddress PrivateIPv4;
		
		/// <summary>First "most appropriate" private IPv6</summary>
		public IPAddress PrivateIPv6;
		
		
		/// <summary>True if any IP matches the given one.</summary>
		private bool Match(byte[] addrBytes, IPAddress addr) {
			if (addr == null)
			{
				if (addrBytes == null || addrBytes.Length == 0)
				{
					return true;
				}

				return false;
			}
			else if (addrBytes == null || addrBytes.Length == 0)
			{
				return false;
			}

			var compareTo = addr.GetAddressBytes();

			if (addrBytes.Length != compareTo.Length)
			{
				return false;
			}

			for (var i = 0; i < addrBytes.Length; i++)
			{
				if (addrBytes[i] != compareTo[i])
				{
					return false;
				}
			}

			return true;
		}

	}

	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class ServiceEventGroup : Eventing.EventGroupCore<AutoService, uint>
	{

		/// <summary>
		/// Just after IPs are discovered. You can modify the IP results if needed.
		/// </summary>
		public Api.Eventing.EventHandler<IpSet> AfterDiscoverIPs;

	}

}
