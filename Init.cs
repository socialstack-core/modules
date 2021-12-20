using System;
using Api.Startup;
using Api.Eventing;
using Api.Contexts;
using System.Threading.Tasks;
using Api.Database;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace Api.DatabaseDiff
{

	/// <summary>
	/// Instances capabilities during the very earliest phases of startup.
	/// </summary>
	[EventListener]
	public class Init
	{
		/// <summary>
		/// True if the DB version has been checked.
		/// </summary>
		private bool? VersionCheckResult;

		/// <summary>
		/// The current schema for the database.
		/// </summary>
		// TODO: In a cluster this will gradually diverge.
		private Schema CurrentDbSchema;

		/// <summary>
		/// Database version text.
		/// </summary>
		private string VersionText;

		private DatabaseService _database;

		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public Init()
		{

			Events.Service.AfterCreate.AddEventListener(async (Context context, AutoService service) => {

				if (service == null || service.ServicedType == null)
				{
					return service;
				}

				// If type derives from DatabaseRow, we have a thing we'll potentially need to reconfigure.
				if (ContentTypes.IsAssignableToGenericType(service.ServicedType, typeof(Content<>)))
				{
					if (_database == null)
					{
						_database = Services.Get<DatabaseService>();
					}

					if (_database == null)
					{
						Console.WriteLine("[WARN] The type '" + service.ServicedType.Name  + "' did not have its database schema updated because the database service was not up in time.");
						return service;
					}

					await HandleDatabaseType(service);
				}

				// Service can now attempt to load its cache:
				await service.SetupCacheIfNeeded();

				return service;
			}, 2);
			
		}

		/// <summary>
		/// Checks the DB version to see if we can auto handle schemas.
		/// </summary>
		/// <returns></returns>
		private async Task<bool> TryCheckVersion()
		{
			if (VersionCheckResult.HasValue)
			{
				return VersionCheckResult.Value;
			}

			// Get MySQL version:
			var versionQuery = Query.List(typeof(DatabaseVersion));
			versionQuery.SetRawQuery("SELECT VERSION() as Version");

			var dbVersion = await _database.Select<DatabaseVersion, uint>(null, versionQuery, typeof(DatabaseVersion), 0);

			// Get DB version:
			VersionText = dbVersion.Version;
			var version = VersionText.ToLower().Trim();

			var versionPieces = version.Split('-');

			if (!Version.TryParse(versionPieces[0], out Version parsedVersion))
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled due to unrecognised MySQL version text. It was: " + version);
				VersionCheckResult = false;
				return false;
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
				VersionCheckResult = false;
				return false;
			}
			else if (parsedVersion < minVersion)
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled. You're using a version of MySQL that is too old. It's version: " + version);
				VersionCheckResult = false;
				return false;
			}
			
			VersionCheckResult = true;
			return true;
		}

		/// <summary>
		/// Loads the complete DB schema.
		/// </summary>
		/// <returns></returns>
		private async Task<Schema> LoadSchema()
		{
			if (CurrentDbSchema != null)
			{
				return CurrentDbSchema;
			}

			var existingSchema = new Schema();
			CurrentDbSchema = existingSchema;
			_database.Schema = CurrentDbSchema;

			List<DatabaseColumnDefinition> columns;

			try
			{
				// Collect all the existing table meta:
				var listQuery = Query.List(typeof(DatabaseColumnDefinition));
				listQuery.SetRawQuery(
					"SELECT table_name as TableName, `column_name` as ColumnName, `data_type` as DataType, " +
					"`is_nullable` = 'YES' as IsNullable, IF(INSTR(extra, 'auto_increment')>0, TRUE, FALSE) as IsAutoIncrement, " +
					"IF(INSTR(column_type, 'unsigned')>0, TRUE, FALSE) as IsUnsigned, " +
					"CAST(IF(`character_maximum_length` IS NULL, `numeric_scale`, `character_maximum_length`) as SIGNED) as MaxCharacters, " +
					"CAST(`numeric_precision` as SIGNED) as MaxCharacters2 " +
					"FROM information_schema.columns WHERE table_schema = DATABASE()"
				);

				columns = await _database.List<DatabaseColumnDefinition>(null, listQuery, typeof(DatabaseColumnDefinition));
			}
			catch(Exception e)
			{
				System.Console.WriteLine("[WARNING] DatabaseDiff module disabled due to a failure during query execution. The version (which is supported) was: " + VersionText + ". The complete error that occurred follows.");
				System.Console.WriteLine(e.ToString());
				VersionCheckResult = false;
				return null;
			}

			// group them by table:
			existingSchema.Add(columns);
			
			return existingSchema;
		}

		/// <summary>
		/// Sets up the table(s) for the given type.
		/// </summary>
		/// <param name="service"></param>
		/// <returns></returns>
		private async Task HandleDatabaseType(AutoService service)
		{

			if (!await TryCheckVersion())
			{
				return;
			}

			var existingSchema = await LoadSchema();
			
			if(existingSchema == null)
			{
				return;
			}
			
			var type = service.InstanceType;

			// New schema for this type:
			var newSchema = new Schema();

			// Get all its fields (including any sub fields).
			var fieldMap = new FieldMap(type);

			// Invoke an event which can e.g. add additional columns or whole tables.
			fieldMap = await Events.DatabaseDiffBeforeAdd.Dispatch(new Context(), fieldMap, type, newSchema);

			if (fieldMap == null)
			{
				// Event handlers don't want this type to update.
				return;
			}

			var targetTableName = type.TableName();

			foreach (var field in fieldMap.Fields)
			{
				// Create a column definition:
				var columnDefinition = new DatabaseColumnDefinition(field, targetTableName);

				if (!columnDefinition.Ignore)
				{
					// Add to target schema:
					newSchema.Add(columnDefinition);
				}
			}

			service.DatabaseSchema = newSchema;

			var tableDiff = existingSchema.Diff(newSchema);
			var altersToRun = new StringBuilder();


			foreach (var table in tableDiff.Added)
			{
				System.Console.WriteLine("Creating table " + table.TableName);
				altersToRun.Append(table.CreateTableSql());
				altersToRun.Append(';');
			}

			foreach (var tableDiffs in tableDiff.Changed)
			{
				// Handle added columns:
				foreach (var newColumn in tableDiffs.Added)
				{
					System.Console.WriteLine("Adding column " + newColumn.TableName + "." + newColumn.ColumnName);
					altersToRun.Append(newColumn.AlterTableSql());
					altersToRun.Append(';');
				}

				// Changed columns that can't be automatically upgraded must be handled via manually specified upgrade objects.
				foreach (var changedColumn in tableDiffs.Changed)
				{
					// We'll attempt an auto upgrade of anything changing meta values (i.e. anything that is not actually changing type).
					if (changedColumn.FromColumn.DataType == changedColumn.ToColumn.DataType)
					{
						// This will fail (expectedly) if the change would result in data loss.
						System.Console.WriteLine("Attempting to alter column  " + changedColumn.ToColumn.TableName + "." + changedColumn.ToColumn.ColumnName + ".");

						altersToRun.Append(changedColumn.ToColumn.AlterTableSql(true));
						altersToRun.Append(';');
					}
					else
					{
						// (No support for those upgrade objects yet)
						System.Console.WriteLine("Manual column change required: '" + changedColumn.ToColumn.AlterTableSql(true) + "'");
					}
				}

			}

			// Merge schema changes into existingSchema. Simply all tables in newSchema replace existing:
			foreach (var kvp in newSchema.Tables)
			{
				existingSchema.Tables[kvp.Key] = kvp.Value;
			}

			// Run now:
			var queryToRun = altersToRun.ToString();

			if (queryToRun.Length > 0)
			{
				try
				{
					await _database.Run(queryToRun);
				}
				catch(MySqlException e)
				{
					// Skipping all MySQL errors - the ones here are "it already exists" errors.
					Console.WriteLine("Skipping a MySQL error during diff: " + e.ToString());
				}
				await Events.DatabaseDiffAfterAdd.Dispatch(new Context(), tableDiff);
			}

		}

	}
}
