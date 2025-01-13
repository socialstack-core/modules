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
	public class DomainChallengePermissions
	{
		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public DomainChallengePermissions()
		{
			// Hook the default role setup. It's done like this so it can be removed by a plugin if wanted.
			Events.CapabilityOnSetup.AddEventListener((Context context, object source) =>
			{
				// Remove public viewing (as it's enabled by default):
				Roles.Guest.Revoke("domainCertificateChallenge_load", "domainCertificateChallenge_list");
				Roles.Public.Revoke("domainCertificateChallenge_load", "domainCertificateChallenge_list");
				Roles.Member.Revoke("domainCertificateChallenge_load", "domainCertificateChallenge_list");
				
				return new ValueTask<object>(source);
			}, 20);
		}
	}
}