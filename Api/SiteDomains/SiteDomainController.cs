using Microsoft.AspNetCore.Mvc;

namespace Api.SiteDomains
{
    /// <summary>Handles siteDomain endpoints.</summary>
    [Route("v1/siteDomain")]
	public partial class SiteDomainController : AutoController<SiteDomain>
    {
    }
}