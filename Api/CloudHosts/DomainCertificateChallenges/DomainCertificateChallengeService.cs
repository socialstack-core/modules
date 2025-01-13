using Api.Eventing;

namespace Api.CloudHosts
{
	/// <summary>
	/// Handles domainCertificateChallenges.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DomainCertificateChallengeService : AutoService<DomainCertificateChallenge>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DomainCertificateChallengeService() : base(Events.DomainCertificateChallenge)
        {
			// Example admin page install:
			// InstallAdminPages("DomainCertificateChallenges", "fa:fa-rocket", new string[] { "id", "name" });
		}
	}
    
}
