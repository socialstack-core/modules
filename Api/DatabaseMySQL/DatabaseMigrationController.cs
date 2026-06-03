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
public partial class DatabaseMigrationController : AutoController
{

	/// <summary>
	/// Converts data within a target engine.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="replacements"></param>
	/// <returns></returns>
	[HttpPost("premap")]
	public async ValueTask MigratePremap(Context context, [FromBody] Dictionary<string, string> replacements)
	{
		if (!context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("migration", context);
		}
		
		var mysql = Services.Get<MySQLDatabaseService>();

		await mysql.MigrateLocalizationAsync();

		await mysql.MigrateMappingsToInterface(
			replacements
		);
	}

}