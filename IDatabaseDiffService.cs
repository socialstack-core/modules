using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Api.DatabaseDiff
{
	/// <summary>
	/// This service checks the site database to see if any new columns are required during startup or on demand.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface IDatabaseDiffService
    {
		/// <summary>
		/// Ensures all used columns are available in the database.
		/// Runs any not yet run database migrations too.
		/// </summary>
		void UpdateDatabaseSchema();

		/// <summary>
		/// Asyncronously ensures all used columns are available in the database.
		/// Runs any not yet run database migrations too.
		/// </summary>
		Task UpdateDatabaseSchemaAsync();

	}
}
