using Microsoft.Extensions.Configuration;
using Api.Configuration;
using System.Threading.Tasks;
using Api.Database;
using System.Text;
using Api.Eventing;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Api.DatabaseDiff
{
	/// <summary>
	/// This service checks the site database to see if any new columns are required during startup or on demand.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class DatabaseDiffService : IDatabaseDiffService
    {
		private IDatabaseService _database;



		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public DatabaseDiffService(IDatabaseService database)
		{
			_database = database;

			// Important that this occurs before other services start such 
			// that anything they setup including pages all exist.
			UpdateDatabaseSchema();
		}
		
		/// <summary>
		/// Ensures all used columns are available in the database.
		/// Runs any not yet run database migrations too.
		/// </summary>
		public void UpdateDatabaseSchema()
		{
			var task = UpdateDatabaseSchemaAsync();
			task.Wait();
		}

		/// <summary>
		/// Ensures all used columns are available in the database.
		/// Runs any not yet run database migrations too.
		/// </summary>
		public async Task UpdateDatabaseSchemaAsync()
		{
			// Get MySQL version:
			var versionQuery = Query.List<DatabaseVersion>();
			versionQuery.SetRawQuery("SELECT VERSION() as Version");

			var dbVersion = await _database.Select(null, versionQuery);

			// Get DB version:
			var version = dbVersion.Version.ToLower().Trim();

			var versionPieces = version.Split('-');

			if (!Version.TryParse(versionPieces[0], out Version parsedVersion))
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled due to unrecognised MySQL version text. It was: " + version);
				return;
			}

			string variant = "base";

			if (versionPieces.Length > 2)
			{
				// It's a variant, like MariaDB
				variant = versionPieces[1];
			}

			Version minVersion = null;

			if (variant == "base")
			{
				minVersion = new Version(5, 7);
			}
			else if (variant == "mariadb")
			{
				minVersion = new Version(10, 1);
			}

			// Which version we got?
			if (minVersion == null)
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled. Unrecognised MySQL variant: " + version);
				return;
			}
			else if (parsedVersion < minVersion)
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled. You're using a version of MySQL that is too old. It's version: " + version);
				return;
			}
			
			List<DatabaseColumnDefinition> columns = null;
			
			try{
				// Collect all the existing table meta:
				var listQuery = Query.List<DatabaseColumnDefinition>();
				listQuery.SetRawQuery(
					"SELECT table_name as TableName, `column_name` as ColumnName, `data_type` as DataType, " +
					"`is_nullable` = 'YES' as IsNullable, IF(INSTR(extra, 'auto_increment')>0, TRUE, FALSE) as IsAutoIncrement, " +
					"IF(INSTR(column_type, 'unsigned')>0, TRUE, FALSE) as IsUnsigned, " +
					"IF(`character_maximum_length` IS NULL, `numeric_scale`, `character_maximum_length`) as MaxCharacters, " +
					"CAST(`numeric_precision` as SIGNED) as MaxCharacters2 " +
					"FROM information_schema.columns WHERE table_schema = DATABASE()"
				);

				columns = await _database.List(null, listQuery, null);
			}
			catch
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled. Unsupported version: " + dbVersion.Version);
				return;
			}
			// group them by table:
			var existingSchema = new Schema();
			existingSchema.Add(columns);

			var newSchema = new Schema();

			// For each content type:
			foreach (var kvp in ContentTypes.TypeMap)
			{
				var typeInfo = kvp.Value;

				// Get all its fields (including any sub fields).
				var fieldMap = new FieldMap(typeInfo);
				
				// Invoke an event which can e.g. add additional
				fieldMap = await Events.DatabaseDiffBeforeAdd.Dispatch(null, fieldMap, typeInfo, newSchema);

				if (fieldMap == null)
				{
					continue;
				}
				
				var targetTableName = typeInfo.TableName();

				foreach (var field in fieldMap.Fields)
				{
					// Create a column definition:
					var columnDefinition = new DatabaseColumnDefinition(field, targetTableName);

					// Add to target schema:
					newSchema.Add(columnDefinition);
				}
				
			}
			
			var tableDiff = existingSchema.Diff(newSchema);
			var altersToRun = new StringBuilder();


			foreach (var table in tableDiff.Added)
			{
				System.Console.WriteLine("Creating table " + table.TableName);
				altersToRun.Append(table.CreateTableSql());
				altersToRun.Append(";");
			}

			foreach (var tableDiffs in tableDiff.Changed)
			{
				// Handle added columns:
				foreach (var newColumn in tableDiffs.Added)
				{
					System.Console.WriteLine("Adding column " + newColumn.TableName + "." + newColumn.ColumnName);
					altersToRun.Append(newColumn.AlterTableSql());
					altersToRun.Append(";");
				}

				// Changed columns that can't be automatically upgraded must be handled via manually specified upgrade objects.
				foreach (var changedColumn in tableDiffs.Changed)
				{
					// (No support for those upgrade objects yet)
					System.Console.WriteLine("Manual column change required: '" + changedColumn.ToColumn.AlterTableSql() + "'");
				}

			}

			// Ignore table removes - database will often contain other tables that we don't need to care about.

			// Run now:
			var queryToRun = altersToRun.ToString();

			if (queryToRun.Length > 0)
			{
				await _database.Run(queryToRun);

				await Events.DatabaseDiffAfterAdd.Dispatch(null, tableDiff);
			}
			
		}
	}
}
