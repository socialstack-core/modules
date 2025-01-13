using Api.Contexts;
using Api.Pages;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.CloudHosts
{
    /// <summary>Handles domainCertificateChallenge endpoints.</summary>
    [Route(".well-known/acme-challenge")]
	public partial class DomainCertificateChallengeController : Controller
    {

		/// <summary>
		/// Handles all token requests.
		/// </summary>
		/// <returns></returns>
		[Route("{token}")]
		public async ValueTask CatchAll([FromRoute] string token)
		{
			var context = await Request.GetContext();
			var match = await Services.Get<DomainCertificateChallengeService>()
				.Where("Token=?", DataOptions.IgnorePermissions)
				.Bind(token)
				.First(context);

			if (match == null)
			{
				Response.StatusCode = 404;
				return;
			}

			await Response.WriteAsync(match.VerificationValue);
		}

	}
}