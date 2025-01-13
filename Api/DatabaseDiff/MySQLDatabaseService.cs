using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Permissions;
using Api.Translate;
using Api.Users;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Api.Startup;
using Api.Configuration;


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
		public MySQLDatabaseService() {
			var envString = System.Environment.GetEnvironmentVariable("DatabaseConnectionString");

			if (string.IsNullOrEmpty(envString))
			{
				// Load from appsettings and add a change handler.
				LoadFromAppSettings();

				AppSettings.OnChange += () => {
					LoadFromAppSettings();
				};
			}
			else
			{
				ConnectionString = envString;
			}

		}

		/// <summary>
		/// Indicates the connection string should be loaded or reloaded.
		/// </summary>
		private void LoadFromAppSettings()
		{
			var connectionStrings = AppSettings.GetSection("ConnectionStrings");

			if (connectionStrings == null)
			{
				throw new Exception("Your appsettings file is missing the 'ConnectionStrings' block.");
			}

			string cStringName;

			if (Services.BuildHost == "xunit")
			{
				cStringName = "TestingConnection";
			}
			else
			{
				cStringName = System.Environment.GetEnvironmentVariable("ConnectionStringName") ?? "DefaultConnection";
			}
			
			ConnectionString = connectionStrings[
				cStringName
			];

			if (ConnectionString == null)
			{
				throw new Exception("Your appsettings file declares a ConnectionString block but is missing a connection string with the key '" + cStringName  + "'");
			}
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

			foreach(var value in values)
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
			for (var x=0;x < toInsertSet.Count;x++)
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
					var fieldValue = q.Fields[i].TargetField.GetValue(toInsert);
					if (fieldValue is DateTime dt)
					{
						fieldValue = dt.ToString("yyyy-MM-dd HH:mm:ss");
					}

					if(fieldValue is bool)
					{
						if(fieldValue is true)
						{
							builder.Append("b'0'");
						}else{
							builder.Append("b'1'");
						}
					}else if(fieldValue != null)
					{
						builder.Append('\"');
						builder.Append(Escape(fieldValue.ToString()));
						builder.Append('\"');
					}else{
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
			where T:class, new()
			where ID : struct, IConvertible, IEquatable<ID>
		{
			// UPDATE, DELETE and INSERT - Loop through each field in the query:
			var fieldCount = q.Fields.Count;

			// Auto edited/ created dates.
			// Applying to the actual entity so the object is up to date too.
			if(q.IsInsert)
			{
				if (srcObject is IHaveTimestamps revRow)
				{
					if (revRow.GetEditedUtc() == DateTime.MinValue)
					{
						revRow.SetEditedUtc(DateTime.UtcNow);
					}

					if (revRow.GetCreatedUtc() == DateTime.MinValue)
					{
						revRow.SetCreatedUtc(DateTime.UtcNow);
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
				parameter.Value = q.Fields[i].TargetField.GetValue(srcObject);
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
		public async Task<T> Select<T, ID>(Context context, Query q, Type instanceType, ID id) where T:new()
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
					if (field.Type == typeof(bool) || field.Type == typeof(bool?))
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
							if (field.Type == typeof(bool) || field.Type == typeof(bool?))
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
							if (field.Type == typeof(bool))
							{
								// Set the value:
								field.TargetField.SetValue(result, Convert.ToBoolean(value));
							}
							else if (field.Type == typeof(bool?))
							{
								if(value == null)
								{
									field.TargetField.SetValue(result, null);
								}else{
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
		
    }
}
