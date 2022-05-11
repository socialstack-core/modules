using Api.Contexts;
using Api.Eventing;
using Api.Startup;
using Lumity.BlockChains;

namespace Api.NetworkNodes;

/// <summary>
/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
/// </summary>
[LoadPriority(3)]
public partial class NetworkNodeService : AutoService<NetworkNode>
{
	/// <summary>
	/// This servers hostname.
	/// </summary>
	public string HostName;

	/// <summary>
	/// This nodes ID.
	/// </summary>
	public uint NodeId => Self == null ? 0 : Self.Id;

	/// <summary>
	/// The node info representing "this" one.
	/// </summary>
	public NetworkNode Self;

	/// <summary>
	/// Discovered IP addresses for this node.
	/// </summary>
	public IpSet DiscoveredIps;

	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public NetworkNodeService() : base(Events.NetworkNode)
	{
		HostName = System.Environment.MachineName.ToString();

		Cache(new CacheConfig() {
			OnCacheLoaded = async () => {
				var context = new Context(1, 1, 1);

				// Discover IPs:
				DiscoveredIps = await IpDiscovery.Discover();

				// Cache loaded - try to find "this" node:
				var self = await Where("HostName=?").Bind(HostName).First(context);
				Self = self; // X for this TODO: Call ServiceReady when cache is loaded. Must then also register this node.

				if (self == null)
				{
					// Register new node.

				}
			}
		});

	}
}
