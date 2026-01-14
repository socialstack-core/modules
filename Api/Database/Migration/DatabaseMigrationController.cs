using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
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
	/// <returns></returns>
	[HttpGet("migrate")]
	public async ValueTask MigrateEverything(Context context, [FromQuery] string from, [FromQuery] string to)
	{
		if (!context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("migration", context);
		}

		await Services.Get<DatabaseMigrationService>().Migrate(from, to);
	}

}