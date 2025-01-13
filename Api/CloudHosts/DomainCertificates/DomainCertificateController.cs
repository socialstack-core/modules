using Microsoft.AspNetCore.Mvc;

namespace Api.CloudHosts
{
    /// <summary>Handles domainCertificate endpoints.</summary>
    [Route("v1/domainCertificate")]
	public partial class DomainCertificateController : AutoController<DomainCertificate>
    {
    }
}