using Api.CloudHosts;
using Api.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Startup;

public partial class StdOutController : AutoController
{

	/// <summary>
	/// Triggers a certificate update (admin only).
	/// </summary>
	/// <returns></returns>
	[HttpGet("certs/update")]
	public async ValueTask<PublicMessage> UpdateCerts(Context context)
	{
		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			throw new PublicException("Admin only", "certs/admin_required");
		}

		// Run cert check (intentionally fails on local dev systems):
		await Services.Get<WebSecurityService>().CheckCertificate(context);

		return new PublicMessage("certificates updated", "certs/ok");
	}

	/// <summary>
	/// Triggers a webserver config file update (admin only).
	/// </summary>
	/// <returns></returns>
	[HttpGet("webserver/apply")]
	public async ValueTask<PublicMessage> UpdateWebserverConfig(Context context)
	{
		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			throw new PublicException("Admin only", "webserver/admin_required");
		}

		// Run regen:
		await Services.Get<WebServerService>().Regenerate(context);

		return new PublicMessage("webserver config regenerated", "webserver/ok");
	}

}
