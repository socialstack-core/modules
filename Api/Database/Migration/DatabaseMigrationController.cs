using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Database;


/// <summary>
/// Instanced automatically. SSMF endpoints.
/// </summary>
/// 
[Route("v1/migration")]
public partial class DatabaseMigrationController : AutoController
{

	/// <summary>
	/// Migrate everything from src to target.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="cc">Custom content. Happens in a second pass, after the descriptions of the types have migrated.</param>
	/// <param name="dry">dry run.</param>
	/// <returns></returns>
	[HttpGet("migrate")]
	public async ValueTask MigrateEverything(Context context, [FromQuery] string from, [FromQuery] string to, [FromQuery] bool? cc, [FromQuery] bool dry = false)
	{
		if (!context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("migration", context);
		}

		await Services.Get<DatabaseMigrationService>().Migrate(from, to, cc, dry);
	}
}