
using Api.Contexts;
using Api.Eventing;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Api.ContentSync
{
	/// <summary>
	/// Utility mechanism for automatic IP discovery. Occurs when a server starts to check that its public record is correct.
	/// </summary>
	public static class IpDiscovery
	{
		/// <summary>
		/// Ipv4 helper site (in future, will be provided by socialstack cloud).
		/// </summary>
		private static string IPv4Site="https://ipv4.icanhazip.com/";
		
		/// <summary>
		/// Ipv6 helper site (in future, will be provided by socialstack cloud).
		/// </summary>
		private static string IPv6Site="https://ipv6.icanhazip.com/";
		
		
		private static async Task<byte[]> GetBytesAsync(string url) {
			var request = (HttpWebRequest)WebRequest.Create(url);
			using (var response = await request.GetResponseAsync())
			using (var content = new MemoryStream())
			using (var responseStream = response.GetResponseStream()) {
				await responseStream.CopyToAsync(content);
				return content.ToArray();
			}
		}
		
		private static async Task<string> GetStringAsync(string url) {
			var bytes = await GetBytesAsync(url);
			return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		}
		
		/// <summary>Finds the public IPv4 address.</summary>
		public static async Task<IPAddress> FindPublicAddress(string addr){
			
			string httpResult=await GetStringAsync(addr);
			
			if(string.IsNullOrEmpty(httpResult)){
				return null;
			}
			
			// So far so good:
			IPAddress res;
			if(!IPAddress.TryParse(httpResult.Trim(), out res))
			{
				return null;
			}
			
			return res;
		}
		
		/// <summary>Gets the IP set.</summary>
		public static async Task<IpSet> Discover()
		{
			var ips = new IpSet();
			
			// Find public addresses:
			ips.PublicIPv4 = await FindPublicAddress(IPv4Site);

			// Find private addresses. A computer might have multiple network interfaces, 
			// and we can't know what its preferred IP is without just trying to use it. 
			// Note that this doesn't actually send any network packets, but will expose the preferred local IP for us.
			
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
			{
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				ips.PrivateIPv4 = endPoint.Address;
			}

			try
			{
				ips.PublicIPv6 = await FindPublicAddress(IPv6Site);

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
				Console.WriteLine("[WARN] Ipv6 appears to be unsupported or not configured.");
			}

			ips = await Events.Service.AfterDiscoverIPs.Dispatch(new Context(), ips);

			return ips;
		}
	}
	
	/// <summary>
	/// A group of 4 IP addresses. Some can be null.
	/// </summary>
	public class IpSet
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
}


namespace Api.Startup
{
	/// <summary>
	/// The group of events for services. See also Events.Service
	/// </summary>
	public partial class ServiceEventGroup : Eventing.EventGroupCore<AutoService, uint>
	{

		/// <summary>
		/// Just after IPs are discovered. You can modify the IP results if needed.
		/// </summary>
		public Api.Eventing.EventHandler<ContentSync.IpSet> AfterDiscoverIPs;

	}

}