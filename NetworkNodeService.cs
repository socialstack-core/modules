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
				Self = self;

				if (self == null)
				{
					// Register new node. If it's the first one, its capabilities are *.
					self = await Create(context, new NetworkNode()
					{
						HostName = HostName,
						PublicAddress = DiscoveredIps.PublicIPv4 == null ? null : DiscoveredIps.PublicIPv4.ToString(),
						Environment = Configuration.Environment.Name,
						Capabilities = "*",
						LocalAddress = DiscoveredIps.PrivateIPv4 == null ? null : DiscoveredIps.PrivateIPv4.ToString()
					});

					Self = self;

					// Note that at this point, the entire chain has been loaded - all caches are ready.
					// Thus, the projects current AssemblerId should be accurate at this point.
					// If it isn't, we'll set it now.

					// Get the project and set self ID:
					var project = Services.Get<BlockDatabase.BlockDatabaseService>().Project;
					project.SetSelfNodeId(self.Id);

					if (project.AssemblerId == 0)
					{
						// Submit a txn to set the projects assemblerId to "mine" now.
						await project.SetAssembler(self.Id);
					}
				}
				else
				{
					// Get the project and set self ID:
					var project = Services.Get<BlockDatabase.BlockDatabaseService>().Project;
					project.SetSelfNodeId(self.Id);
				}
			}
		});

	}
}
