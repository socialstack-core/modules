using Api.Configuration;
using Api.Contexts;
using Api.Permissions;
using Api.Startup;
using Api.Translate;
using Api.Users;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Api.Database
{
	/// <summary>
	/// MySQL database service.
	/// Connects to a database with the given connection string.
	/// </summary>
	[LoadPriority(1)]
	public partial class MySQLDatabaseService : AutoService
	{
		/// <summary>
		/// The connection string to use.
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// The latest DB schema.
		/// </summary>
		public Schema Schema { get; set; }

		/// <summary>
		/// Create a new database connector with the given connection string.
		/// </summary>
		public MySQLDatabaseService()
		{
			// Load from appsettings and add a change handler.
			LoadFromAppSettings();

			AppSettings.OnChange += () =>
			{
				LoadFromAppSettings();
			};
		}

		/// <summary>
		/// Indicates the connection string should be loaded or reloaded.
		/// </summary>
		private void LoadFromAppSettings()
		{
			var cs = GetConfiguredConnectionString();
			ConnectionString = cs == null ? null : cs.ConnectionConfig;
		}

		/// <summary>
		/// Returns a connection string, or null, if it isn't configured.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public static ConnectionString GetConfiguredConnectionString()
		{
			// MySQL has no prefix:
			return Api.Database.ConnectionString.Get("");
		}

		/// <summary>
		/// Gets a new database connection. Pools internally.
		/// </summary>
		/// <returns></returns>
		internal MySqlConnection GetConnection()
		{
			return new MySqlConnection(ConnectionString);
		}

		/// <summary>
		/// Database text escape. You should instead be using the args set (and ? placeholders).
		/// </summary>
		/// <param name="text">The text to escape.</param>
		/// <returns></returns>
		public string Escape(string text)
		{
			return MySql.Data.MySqlClient.MySqlHelper.EscapeString(text);
		}

		/// <summary>
		/// Builds an IN(x,y,z) string using the given value enumerator.
		/// </summary>
		/// <param name="intoBuilder"></param>
		/// <param name="values"></param>
		private static void BuildInString(System.Text.StringBuilder intoBuilder, IEnumerable<uint> values)
		{
			if (values == null)
			{
				return;
			}

			intoBuilder.Append("IN(");

			bool first = true;

			foreach (var value in values)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					intoBuilder.Append(',');
				}
				intoBuilder.Append(value);
			}

			intoBuilder.Append(')');
		}

		/// <summary>
		/// Run a raw query with no arguments. Avoid when possible.
		/// </summary>
		/// <param name="query">The query to run.</param>
		/// <returns></returns>
		public async Task<bool> Run(string query)
		{
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = new MySqlCommand(query, connection);
			return await cmd.ExecuteNonQueryAsync() > 0;
		}

		/// <summary>
		/// Run a raw query with no arguments. Avoid when possible.
		/// </summary>
		/// <param name="query">The query to run.</param>
		/// <param name="timeout">Optional timeout to use.</param>
		/// <returns></returns>
		public async Task<bool> Run(string query, int timeout)
		{

			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = new MySqlCommand(query, connection);
			cmd.CommandTimeout = timeout;

			return await cmd.ExecuteNonQueryAsync() > 0;
		}

		/// <summary>
		/// Usually used for bulk deletes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="idsToDelete"></param>
		/// <returns></returns>
		public async Task<bool> Run<T>(Context context, Query q, IEnumerable<uint> idsToDelete)
		{
			if (idsToDelete == null)
			{
				return false;
			}

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var gl = ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null;
				if (gl == null || gl.Id != localeId)
				{
					// The locale doesn't exist - we fell back to the site default one which doesn't have a column suffix.
					localeId = 0;
				}
				else
				{
					localeCode = gl.Code;
				}
			}

			var queryText = q.GetQuery(true, localeId, localeCode);
			var builder = new System.Text.StringBuilder();
			builder.Append(queryText);
			BuildInString(builder, idsToDelete);
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = new MySqlCommand(builder.ToString(), connection);
			return await cmd.ExecuteNonQueryAsync() > 0;
		}

		/// <summary>
		/// Used for bulk inserts.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="toInsertSet"></param>
		/// <returns></returns>
		public async Task<bool> Run<T>(Context context, Query q, List<T> toInsertSet)
		{
			if (toInsertSet == null || toInsertSet.Count == 0)
			{
				return false;
			}

			// Loop through each field in the query and then bind values from each toInsert.
			var fieldCount = q.Fields.Count;

			// Note that the additional args are for any more complex args in the query.

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			var queryText = q.GetQuery(true, localeId, localeCode);
			var builder = new System.Text.StringBuilder();
			builder.Append(queryText);

			// For each one..
			for (var x = 0; x < toInsertSet.Count; x++)
			{
				var toInsert = toInsertSet[x];

				if (x == 0)
				{
					builder.Append('(');
				}
				else
				{
					builder.Append("), (");
				}

				for (var i = 0; i < fieldCount; i++)
				{
					if (i != 0)
					{
						builder.Append(',');
					}
					// Bind the field value:
					var field = q.Fields[i];
					var fieldValue = field.TargetField.GetValue(toInsert);
					if (fieldValue is DateTime dt)
					{
						fieldValue = dt.ToString("yyyy-MM-dd HH:mm:ss");
					}

					if (fieldValue is bool)
					{
						if (fieldValue is true)
						{
							builder.Append("b'0'");
						}
						else
						{
							builder.Append("b'1'");
						}
					}
					else if (fieldValue != null)
					{
						builder.Append('\"');
						builder.Append(Escape(fieldValue.ToString()));
						builder.Append('\"');
					}
					else
					{
						builder.Append("NULL");
					}

				}
			}

			builder.Append(')');
			var builtQuery = builder.ToString();

			// Result is the ID.
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = new MySqlCommand(builtQuery, connection);
			if (await cmd.ExecuteNonQueryAsync() > 0)
			{
				var id = cmd.LastInsertedId;

				if (q.IdField != null)
				{
					// Set the IDs now. The lastInsertedId is the *first* one.
					for (var x = 0; x < toInsertSet.Count; x++)
					{
						q.IdField.SetValue(toInsertSet[x], (uint)(id + x));
					}

				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Runs the given query using the given arguments to bind.
		/// Does not return any values other than a true/ false if it succeeded.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="srcObject"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<bool> Run<T, ID>(Context context, Query q, T srcObject, ID? id = null)
			where T : class, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			// UPDATE, DELETE and INSERT - Loop through each field in the query:
			var fieldCount = q.Fields.Count;

			// Auto edited/ created dates.
			// Applying to the actual entity so the object is up to date too.
			if (q.IsInsert)
			{
				if (srcObject is IHaveTimestamps revRow)
				{
					var now = DateTime.UtcNow;

					if (revRow.GetEditedUtc() == DateTime.MinValue)
					{
						revRow.SetEditedUtc(now);
					}

					if (revRow.GetCreatedUtc() == DateTime.MinValue)
					{
						revRow.SetCreatedUtc(now);
					}
				}
			}

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			// Run update:
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = new MySqlCommand(q.GetQuery(false, localeId, localeCode), connection);

			for (var i = 0; i < fieldCount; i++)
			{
				var parameter = cmd.CreateParameter();
				parameter.ParameterName = "p" + i;
				var field = q.Fields[i];
				var val = field.TargetField.GetValue(srcObject);

				if (field.IsLocalized)
				{
					parameter.Value = val == null ? null : val.ToString();
					parameter.MySqlDbType = MySqlDbType.JSON;
				}
				else if (field.Type == typeof(JsonString))
				{
					parameter.Value = val == null ? null : ((JsonString)val).ValueOf();
					parameter.MySqlDbType = MySqlDbType.JSON;
				}
				else if (field.Type == typeof(MappingData))
				{
					parameter.Value = val == null ? null : ((MappingData)val).ToJson();
					parameter.MySqlDbType = MySqlDbType.JSON;
				}
				else
				{
					parameter.Value = val;
				}

				cmd.Parameters.Add(parameter);
			}

			if (id.HasValue)
			{
				var parameter = cmd.CreateParameter();
				parameter.ParameterName = "id";
				parameter.Value = id.Value;
				cmd.Parameters.Add(parameter);
			}

			if (await cmd.ExecuteNonQueryAsync() > 0)
			{
				if (q.IdField != null)
				{
					// Set the ID now:
					if (q.IdField.FieldType == typeof(ulong))
					{
						q.IdField.SetValue(srcObject, (ulong)cmd.LastInsertedId);
					}
					else
					{
						q.IdField.SetValue(srcObject, (uint)cmd.LastInsertedId);
					}
				}

				return true;
			}
			else
			{
				return false;
			}

		}

		/// <summary>
		/// Runs the given query using the given ID arg to bind.
		/// Does not return any values other than a true/ false if it succeeded.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<bool> RunWithId<ID>(Context context, Query q, ID id)
		{
			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			// Run update:
			using var connection = GetConnection();
			await connection.OpenAsync();
			var qry = q.GetQuery(false, localeId, localeCode);
			var cmd = new MySqlCommand(qry, connection);

			var parameter = cmd.CreateParameter();
			parameter.ParameterName = "id";
			parameter.Value = id;
			cmd.Parameters.Add(parameter);

			return (await cmd.ExecuteNonQueryAsync() > 0);
		}

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as the given object.
		/// </summary>
		/// <param name="context"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="ID"></typeparam>
		/// <param name="q"></param>
		/// <param name="instanceType">The type to instantiate</param>
		/// <param name="id"></param>
		/// <returns></returns>
		public async Task<T> Select<T, ID>(Context context, Query q, Type instanceType, ID id) where T : new()
		{
			// Only SELECT comes through here.
			// This is almost exactly the same as GetRow 
			// except it operates using the field map in the query.

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			using var connection = GetConnection();
			await connection.OpenAsync();
			var qryString = q.GetQuery(false, localeId, localeCode);
			var cmd = new MySqlCommand(qryString, connection);

			var parameter = cmd.CreateParameter();
			parameter.ParameterName = "id";
			parameter.Value = id;
			cmd.Parameters.Add(parameter);

			using var reader = await cmd.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
			{
				return default;
			}

			// Create the object: 
			var result = Activator.CreateInstance(instanceType);

			// For each field..
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var value = reader.GetValue(i);

				if (value is System.DBNull)
				{
					continue;
				}

				var field = q.Fields[i];

				try
				{
					if (field.IsLocalized)
					{
						field.TargetField.SetValue(result, field.ParseLocalized(value as string));
					}
					else if (field.Type == typeof(JsonString))
					{
						field.TargetField.SetValue(result, new JsonString(value as string));
					}
					else if (field.Type == typeof(MappingData))
					{
						field.TargetField.SetValue(result, MappingData.Parse(value as string));
					}
					else if (field.Type == typeof(bool) || field.Type == typeof(bool?))
					{
						// Set the value:
						field.TargetField.SetValue(result, Convert.ToBoolean(value));
					}
					else
					{
						// Set the value:
						field.TargetField.SetValue(result, value);
					}
				}
				catch (Exception e)
				{
					Log.Error(LogTag, e, "Failure setting field " + field.Name + " on type " + typeof(T));
					throw;
				}
			}

			return (T)result;
		}

		/// <summary>
		/// Gets a list of results from the cache, calling the given callback each time one is discovered.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="queryPair">Both filterA and filterB must have values.</param>
		/// <param name="onResult"></param>
		/// <param name="srcA"></param>
		/// <param name="srcB"></param>
		/// <param name="instanceType"></param>
		/// <param name="q"></param>
		public async ValueTask<int> GetResults<T, ID>(
			Context context, QueryPair<T, ID> queryPair, Func<Context, T, int, object, object, ValueTask> onResult,
			object srcA, object srcB, Type instanceType, Query q
		)
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
		{
			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (ContentTypes.Locales != null && localeId <= ContentTypes.Locales.Length ? ContentTypes.Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			if (localeCode == null)
			{
				localeCode = "en";
			}

			var includeTotal = queryPair.QueryA == null ? false : queryPair.QueryA.IncludeTotal;

			int total = 0;

			using (var connection = GetConnection())
			{
				await connection.OpenAsync();

				var cmd = new MySqlCommand();
				cmd.Connection = connection;

				// When pagination is active and a total has to be calculated separately, qryText is actually a comma separated double query.
				q.ApplyQuery(context, cmd, queryPair, false, localeId, localeCode, includeTotal);

				using var reader = await cmd.ExecuteReaderAsync();

				if (includeTotal)
				{
					await reader.ReadAsync();
					var count = (long)reader.GetValue(0);
					total = (int)count;

					await reader.NextResultAsync();
				}

				var index = 0;

				while (await reader.ReadAsync())
				{
					// Create the object: 
					var result = Activator.CreateInstance(instanceType);

					// For each field..
					for (var i = 0; i < reader.FieldCount; i++)
					{
						var value = reader.GetValue(i);

						if (value is System.DBNull)
						{
							continue;
						}

						var field = q.Fields[i];

						try
						{
							if (field.IsLocalized)
							{
								field.TargetField.SetValue(result, field.ParseLocalized(value as string));
							}
							else if (field.Type == typeof(JsonString))
							{
								field.TargetField.SetValue(result, new JsonString(value as string));
							}
							else if (field.Type == typeof(MappingData))
							{
								field.TargetField.SetValue(result, MappingData.Parse(value as string));
							}
							else if (field.Type == typeof(bool) || field.Type == typeof(bool?))
							{
								// Set the value:
								field.TargetField.SetValue(result, Convert.ToBoolean(value));
							}
							else
							{
								// Set the value:
								field.TargetField.SetValue(result, value);
							}
						}
						catch (Exception e)
						{
							Log.Error(LogTag, e, "Failure setting field " + field.Name + " on type " + typeof(T));
							throw;
						}
					}

					await onResult(context, (T)result, index, srcA, srcB);
					index++;
				}
			}

			return total;
		}

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as a list of the given type.
		/// </summary>
		/// <param name="context"></param>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="instanceType">The type to instantiate</param>
		/// <returns></returns>
		public async Task<List<T>> List<T>(Context context, Query q, Type instanceType) where T : new()
		{
			var results = new List<T>();
			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;

				if (ContentTypes.Locales != null && localeId > 0 && localeId <= ContentTypes.Locales.Length)
				{
					var locale = ContentTypes.Locales[localeId - 1];
					if (locale != null)
					{
						localeCode = locale.Code;
					}
				}
			}

			using (var connection = GetConnection())
			{
				await connection.OpenAsync();
				var qryText = q.GetQuery(false, localeId, localeCode);

				MySqlCommand cmd = new MySqlCommand(qryText, connection);

				using var reader = await cmd.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					// Create the object: 
					var result = Activator.CreateInstance(instanceType);

					// For each field..
					for (var i = 0; i < reader.FieldCount; i++)
					{
						// string fieldName = reader.GetName(i);
						// int fieldIndex = 
						var value = reader.GetValue(i);

						if (value is System.DBNull)
						{
							continue;
						}

						var field = q.Fields[i];

						try
						{
							if (field.IsLocalized)
							{
								field.TargetField.SetValue(result, field.ParseLocalized(value as string));
							}
							else if (field.Type == typeof(JsonString))
							{
								field.TargetField.SetValue(result, new JsonString(value as string));
							}
							else if (field.Type == typeof(MappingData))
							{
								field.TargetField.SetValue(result, MappingData.Parse(value as string));
							}
							else if (field.Type == typeof(bool))
							{
								// Set the value:
								field.TargetField.SetValue(result, Convert.ToBoolean(value));
							}
							else if (field.Type == typeof(bool?))
							{
								if (value == null)
								{
									field.TargetField.SetValue(result, null);
								}
								else
								{
									bool? newValue = Convert.ToBoolean(value);
									field.TargetField.SetValue(result, newValue);
								}
							}
							else
							{
								// Set the value:
								field.TargetField.SetValue(result, value);
							}
						}
						catch (Exception e)
						{
							Log.Error(LogTag, e, "Failure setting field " + field.Name + " on type " + typeof(T));
							throw;
						}
					}

					results.Add((T)result);
				}
			}

			return results;
		}

		/// <summary>
		/// Migrates _map_ tables to their targeted row as Mappings. Destructive: will replace any existing mapping data.
		/// </summary>
		public async Task MigrateMappingsToInterface(Dictionary<string, string> truncationReplacements)
		{
			// Structure: SourceTable -> (SourceId -> (MapName -> List<TargetId>))
			var migrationData = new Dictionary<string, Dictionary<ulong, Dictionary<string, List<ulong>>>>();

			using var connection = GetConnection();
			if (connection.State != ConnectionState.Open) connection.Open();

			// 1. Discover all mapping tables matching the pattern
			var mappingTables = await DiscoverMappingTables(connection, truncationReplacements);
			Console.WriteLine($"Found {mappingTables.Count} mapping tables to process.");

			// 2. Load all mapping data into memory
			foreach (var table in mappingTables)
			{
				Console.WriteLine($"Reading data from {table.RawTableName}...");

				string query = $"SELECT SourceId, TargetId FROM `{table.RawTableName}`";
				using var command = new MySqlCommand(query, connection);
				using var reader = command.ExecuteReader();

				if (!migrationData.ContainsKey(table.SourceTable))
					migrationData[table.SourceTable] = new Dictionary<ulong, Dictionary<string, List<ulong>>>();

				var sourceTableDict = migrationData[table.SourceTable];

				while (reader.Read())
				{
					var sourceId = reader.GetUInt64(0);
					var targetId = reader.GetUInt64(1);

					if (!sourceTableDict.ContainsKey(sourceId))
						sourceTableDict[sourceId] = new Dictionary<string, List<ulong>>();

					if (!sourceTableDict[sourceId].ContainsKey(table.MapName))
						sourceTableDict[sourceId][table.MapName] = new List<ulong>();

					sourceTableDict[sourceId][table.MapName].Add(targetId);
				}
			}

			// 3. Write JSON data back to the Source Tables
			Console.WriteLine("Writing JSON mappings back to source tables...");
			foreach (var sourceTableItem in migrationData)
			{
				string sourceTable = sourceTableItem.Key;
				var rowsToUpdate = sourceTableItem.Value;

				try
				{
					// Ensure the Mappings JSON column exists on the target table before updating
					await EnsureMappingsColumnExists(connection, sourceTable);
				}
				catch (MySqlException ex) when(ex.Message.Contains("doesn't exist") || ex.ErrorCode == -2147467259)
				{
					Console.WriteLine($"[Skipped] Source table '{sourceTable}' does not exist. Ignoring historical data.");
					continue;
				}

				// Use a transaction for performance and safety per source table
				using var transaction = connection.BeginTransaction();
				try
				{
					string updateQuery = $"UPDATE `{sourceTable}` SET Mappings = @json WHERE Id = @id";
					using var updateCmd = new MySqlCommand(updateQuery, connection, transaction);

					var jsonParam = updateCmd.Parameters.Add("@json", MySqlDbType.JSON);
					var idParam = updateCmd.Parameters.Add("@id", MySqlDbType.Int32);

					foreach (var row in rowsToUpdate)
					{
						var sourceId = row.Key;
						var mapPayload = row.Value; // e.g., {"tags": [1,2,3], "categories": [4,5]}

						string jsonString = JsonConvert.SerializeObject(mapPayload);

						jsonParam.Value = jsonString;
						idParam.Value = sourceId;

						updateCmd.ExecuteNonQuery();
					}
					transaction.Commit();
					Console.WriteLine($"Successfully updated {rowsToUpdate.Count} rows in {sourceTable}.");
				}
				catch (Exception ex)
				{
					transaction.Rollback();
					Console.WriteLine($"Error updating table {sourceTable}: {ex.Message}. Rolled back.");
					throw;
				}
			}
		}

		private async Task<List<MappingTableMetadata>> DiscoverMappingTables(MySqlConnection connection, Dictionary<string, string> truncationReplacements)
		{
			var list = new List<MappingTableMetadata>();

			// Regex to parse: site_{SourceType}_{TargetType}_map_{MapName}
			// Group 1 catches the Source Type, Group 2 catches the Map Name
			var pattern = new Regex(@"^site_(.+?)_(.+?)_map_(.+)$", RegexOptions.IgnoreCase);

			string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE()";
			using var command = new MySqlCommand(query, connection);
			using var reader = command.ExecuteReader();

			while (reader.Read())
			{
				string tableName = reader.GetString(0);
				string correctedTableName = tableName;

				if (tableName.Length == 64)
				{
					// Possibly truncated.
					// Check the truncation set.
					if (truncationReplacements == null || !truncationReplacements.TryGetValue(tableName, out string replacement))
					{
						throw new Exception(tableName + " was found but it likely has a truncated name which has not been provided.");
					}

					correctedTableName = replacement;
				}

				var match = pattern.Match(correctedTableName);

				if (match.Success)
				{
					list.Add(new MappingTableMetadata
					{
						RawTableName = tableName,
						CorrectedTableName = correctedTableName,
						SourceTable = $"site_{match.Groups[1].Value}", // reconstructs e.g. 'site_blog'
						MapName = match.Groups[3].Value              // extracts e.g. 'tags'.
					});
				}
			}

			return list;
		}

		private async Task EnsureMappingsColumnExists(MySqlConnection connection, string tableName)
		{
			// Dynamically appends the column if it isn't already there
			string checkQuery = $@"
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = 'Mappings'";

			using var command = new MySqlCommand(checkQuery, connection);
			long columnExists = (long)command.ExecuteScalar();

			if (columnExists == 0)
			{
				string alterQuery = $"ALTER TABLE `{tableName}` ADD COLUMN Mappings JSON NULL;";
				using var alterCmd = new MySqlCommand(alterQuery, connection);
				await alterCmd.ExecuteNonQueryAsync();
				Console.WriteLine($"Added 'Mappings' JSON column to {tableName}.");
			}
		}

		private class MappingTableMetadata
		{
			public string CorrectedTableName { get; set; }
			public string RawTableName { get; set; }
			public string SourceTable { get; set; }
			public string MapName { get; set; }
		}

		/// <summary>
		/// Migrates localised fields.
		/// </summary>
		/// <returns></returns>
		public async Task MigrateLocalizationAsync()
		{
			using var connection = GetConnection();
			if (connection.State != ConnectionState.Open) await connection.OpenAsync();

			// 1. Discover all tables and their respective localized columns
			var localizedTargets = await DiscoverLocalizedColumnsAsync(connection);
			Console.WriteLine($"Found {localizedTargets.Count} tables with localized columns.");

			foreach (var target in localizedTargets)
			{
				string tableName = target.Key;
				List<string> baseFields = target.Value; // e.g., ["Name", "Description"]

				Console.WriteLine($"Processing table '{tableName}' for fields: {string.Join(", ", baseFields)}...");

				// 2. Build a dynamic SELECT query to pull the IDs, original fields, and localized fields
				var selectColumns = new List<string> { "Id" };
				foreach (var field in baseFields)
				{
					selectColumns.Add($"`{field}`");
					selectColumns.Add($"`{field}_en-US`");
				}

				string selectQuery = $"SELECT {string.Join(", ", selectColumns)} FROM `{tableName}`";

				// In-memory store for updates: Id -> Dictionary<FieldName, JsonPayloadString>
				var updatesToApply = new Dictionary<int, Dictionary<string, string>>();

				using (var selectCmd = new MySqlCommand(selectQuery, connection))
				using (var reader = await selectCmd.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						int id = reader.GetInt32("Id");
						var fieldJsonUpdates = new Dictionary<string, string>();

						foreach (var field in baseFields)
						{
							int enOrdinal = reader.GetOrdinal(field);
							int enUsOrdinal = reader.GetOrdinal($"{field}_en-US");

							// Read the raw values as native objects (int, decimal, string, etc.)
							object enValue = reader.IsDBNull(enOrdinal) ? null : reader.GetValue(enOrdinal);
							object enUsValue = reader.IsDBNull(enUsOrdinal) ? null : reader.GetValue(enUsOrdinal);

							// If the data driver returns custom MySQL numeric types, 
							// we can explicitly normalize them to standard C# types if needed, 
							// though System.Text.Json handles most primitives (like uint, int, decimal) out of the box.

							// Build the localized object using 'object' values
							var localizedObj = new Dictionary<string, object>
							{
								{ "en", enValue },
								{ "en-US", enUsValue }
							};

							// Serialize to JSON string
							fieldJsonUpdates[field] = JsonConvert.SerializeObject(localizedObj);
						}

						updatesToApply[id] = fieldJsonUpdates;
					}
				}

				// 3. Drop the localised columns completely such that they are out of the way, avoiding cast issues.
				await DropLocalizedColumnsAsync(connection, tableName, baseFields);

				// 4. Stream the updates back down to the database inside a transaction
				using var transaction = await connection.BeginTransactionAsync();
				try
				{
					foreach (var rowUpdate in updatesToApply)
					{
						int id = rowUpdate.Key;

						// Build a dynamic UPDATE statement accommodating all transformed fields for this row
						var setClauses = new List<string>();
						using var updateCmd = new MySqlCommand { Connection = connection, Transaction = transaction };

						int paramIndex = 0;
						foreach (var fieldUpdate in rowUpdate.Value)
						{
							string fieldName = fieldUpdate.Key;
							string jsonString = fieldUpdate.Value;
							string paramName = $"@json_{paramIndex}";

							setClauses.Add($"`{fieldName}` = {paramName}");
							updateCmd.Parameters.AddWithValue(paramName, jsonString);
							paramIndex++;
						}

						updateCmd.CommandText = $"UPDATE `{tableName}` SET {string.Join(", ", setClauses)} WHERE Id = @id";
						updateCmd.Parameters.AddWithValue("@id", id);

						await updateCmd.ExecuteNonQueryAsync();
					}

					await transaction.CommitAsync();
					Console.WriteLine($"Successfully localized data for table '{tableName}'.");
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					Console.WriteLine($"Error updating table {tableName}: {ex.Message}. Rolled back.");
					throw;
				}
			}
		}

		private async Task<Dictionary<string, List<string>>> DiscoverLocalizedColumnsAsync(MySqlConnection connection)
		{
			// Structure: TableName -> List of Base Field Names (e.g., "Name" derived from "Name_en-US")
			var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			string query = @"
            SELECT TABLE_NAME, COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE() 
              AND COLUMN_NAME LIKE '%\_en-US';";

			using var command = new MySqlCommand(query, connection);
			using var reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				string tableName = reader.GetString(0);
				string columnName = reader.GetString(1);

				// Extract the base column name by removing "_en-US"
				string baseColumnName = columnName.Substring(0, columnName.Length - 6);

				if (!result.ContainsKey(tableName))
					result[tableName] = new List<string>();

				result[tableName].Add(baseColumnName);
			}

			return result;
		}

		private async Task DropLocalizedColumnsAsync(MySqlConnection connection, string tableName, List<string> baseFields)
		{
			foreach (var field in baseFields)
			{
				try
				{
					string dropQuery = $"ALTER TABLE `{tableName}` DROP COLUMN `{field}_en-US`, DROP COLUMN `{field}`, ADD COLUMN `{field}` JSON NULL;";
					using var command = new MySqlCommand(dropQuery, connection);
					await command.ExecuteNonQueryAsync();
					Console.WriteLine($"Dropped obsolete column `{field}_en-US` from table '{tableName}'.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Warning: Could not drop `{field}_en-US` from '{tableName}': {ex.Message}");
				}
			}
		}
	}
}
