using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using Api.Permissions;
using System.Threading.Tasks;

namespace Api.CloudHosts
{
	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class DomainCertificatePermissions
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public DomainCertificatePermissions()
		{
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("domainCertificate_load", "domainCertificate_list");
				Roles.Public.Revoke("domainCertificate_load", "domainCertificate_list");
				Roles.Member.Revoke("domainCertificate_load", "domainCertificate_list");
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}