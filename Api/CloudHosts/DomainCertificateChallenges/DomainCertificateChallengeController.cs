using Api.Contexts;
using Api.Startup;
using Api.Startup.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

namespace Api.CloudHosts
{
    /// <summary>Handles domainCertificateChallenge endpoints.</summary>
    [Route(".well-known/acme-challenge")]
    public partial class DomainCertificateChallengeController : AutoController
    {
		/// <summary>
		/// Handles all token requests.
		/// </summary>
		/// <returns></returns>
		[HttpGet("{token}")]
		public async ValueTask<FileContent?> CatchAll(Context context, [FromRoute] string token)
		{
			var match = await Services.Get<DomainCertificateChallengeService>()
				.Where("Token=?", DataOptions.IgnorePermissions)
				.Bind(token)
				.First(context);

			if (match == null || match.VerificationValue == null)
			{
				return null;
			}

			return new FileContent(Encoding.UTF8.GetBytes(match.VerificationValue), "text/plain");
		}

	}
}