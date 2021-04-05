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


namespace Api.Database
{
	/// <summary>
	/// A reusable database service core, enabling multiple database connections.
	/// Connects to a database with the given connection string.
	/// </summary>
	public partial class DatabaseServiceCore
	{
		/// <summary>
		/// The connection string to use.
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// The supported locale set, indexed by locale ID-1. Can be null.
		/// </summary>
		public Locale[] Locales { get; set; }
		
		/// <summary>
		/// Create a new database connector with the given connection string.
		/// </summary>
		/// <param name="connectionString"></param>
		public DatabaseServiceCore(string connectionString) {
			ConnectionString = connectionString;
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
		/// Gets the index information for the given type. Does not cache internally.
		/// </summary>
		public List<DatabaseIndexInfo> GetIndices(Type contentType){

			var indexSet = new List<DatabaseIndexInfo>();

			// For each public field, including inherited ones, look out for [DatabaseIndex]
			// It can also be used on the type to declare multi-field indices.
			var attributes = contentType.GetCustomAttributes(true);

			// Get the field set:
			var fields = contentType.GetFields();

			foreach (var attrib in attributes)
			{
				if (attrib is DatabaseIndexAttribute attribute)
				{
					indexSet.Add(new DatabaseIndexInfo(attribute, contentType));
				}
			}

			// For each field, get the db index attributes:
			foreach (var field in fields)
			{
				var attribs = field.GetCustomAttributes(true);

				foreach (var attrib in attribs)
				{
					if (attrib is DatabaseIndexAttribute attribute)
					{
						indexSet.Add(new DatabaseIndexInfo(attribute, field));
					}
				}

			}

			return indexSet;
		}
		
		/// <summary>
		/// A helper to generate a command object with the given arguments.
		/// </summary>
		/// <param name="connection">The connection to use.</param>
		/// <param name="query">The query to run.</param>
		/// <param name="args">The set of args to add.</param>
		/// <returns></returns>
		private static MySqlCommand CreateCommand(MySqlConnection connection, string query, object[] args)
		{
			var cmd = new MySqlCommand(query, connection);
			MySqlParameter parameter;

			if (args != null)
			{
				for (var i = 0; i < args.Length; i++)
				{
					parameter = cmd.CreateParameter();
					parameter.ParameterName = "p" + i;
					parameter.Value = args[i];
					cmd.Parameters.Add(parameter);
				}
			}
			return cmd;
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
			var cmd = CreateCommand(connection, query, null);
			return await cmd.ExecuteNonQueryAsync() > 0;
		}

		/// <summary>
		/// Usually used for bulk deletes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="idsToDelete"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<bool> Run<T>(Context context, Query<T> q, IEnumerable<uint> idsToDelete, params object[] args)
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
				var gl = Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null;
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

			var queryText = q.GetQuery(null, true, localeId, localeCode);
			var builder = new System.Text.StringBuilder();
			builder.Append(queryText);
			BuildInString(builder, idsToDelete);
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = CreateCommand(connection, builder.ToString(), args);
			return await cmd.ExecuteNonQueryAsync() > 0;
		}

		/// <summary>
		/// Used for bulk inserts.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="toInsertSet"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<bool> Run<T>(Context context, Query<T> q, List<T> toInsertSet, params object[] args)
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
				var locale = (Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			var queryText = q.GetQuery(null, true, localeId, localeCode);
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
					builder.Append('\"');
					builder.Append(Escape(fieldValue.ToString()));
					builder.Append('\"');
				}
			}

			builder.Append(')');
			var builtQuery = builder.ToString();

			// Result is the ID.
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = CreateCommand(connection, builtQuery, args);
			if (await cmd.ExecuteNonQueryAsync() > 0)
			{
				var id = cmd.LastInsertedId;

				if (q.IdField != null)
				{
					// Set the IDs now. The lastInsertedId is the *first* one.
					for (var x = 0; x < toInsertSet.Count; x++)
					{
						q.IdField.SetValue(toInsertSet[x], (int)(id + x));
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
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<bool> Run<T>(Context context, Query<T> q, T srcObject, params object[] args) where T:class, new()
		{
			// UPDATE, DELETE and INSERT - Loop through each field in the query:
			var fieldCount = q.Fields.Count;

			// Auto edited/ created dates.
			// Applying to the actual entity so the object is up to date too.
			if (q.IsUpdate)
			{
				if (srcObject is IHaveTimestamps revRow && context.PermitEditedUtcChange)
				{
					revRow.SetEditedUtc(DateTime.UtcNow);
				}
			}
			else if(q.IsInsert)
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
			
			// Note that the additional args are for custom WHERE params.
			var argSet = new object[fieldCount + args.Length];

			for (var i = 0; i < fieldCount; i++)
			{
				// Bind the field value:
				argSet[i] = q.Fields[i].TargetField.GetValue(srcObject);
			}
			
			// And copy in any other fields (the where args for updates):
			Array.Copy(args, 0, argSet, fieldCount, args.Length);

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			// Run update:
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = CreateCommand(connection, q.GetQuery(null, false, localeId, localeCode), argSet);
			if (await cmd.ExecuteNonQueryAsync() > 0)
			{
				if (q.IdField != null)
				{
					// Set the ID now:
					q.IdField.SetValue(srcObject, (int)cmd.LastInsertedId);
				}

				return true;
			}
			else
			{
				return false;
			}

		}

		/// <summary>
		/// Runs the given query using the given arguments to bind.
		/// Does not return any values other than a true/ false if it succeeded.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<bool> Run(Context context, Query q, params object[] args)
		{
			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			// Run update:
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = CreateCommand(connection, q.GetQuery(null, false, localeId, localeCode), args);
			return (await cmd.ExecuteNonQueryAsync() > 0);
		}

		/// <summary>
		/// Runs the given query using the given arguments to bind.
		/// Does not return any values other than a true/ false if it succeeded.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="q"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<bool> Run<T>(Context context, Query<T> q, params object[] args)
		{
			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			// Run update:
			using var connection = GetConnection();
			await connection.OpenAsync();
			var cmd = CreateCommand(connection, q.GetQuery(null, false, localeId, localeCode), args);
			return (await cmd.ExecuteNonQueryAsync() > 0);
		}

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as the given object.
		/// </summary>
		/// <param name="context"></param>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<T> Select<T>(Context context, Query<T> q, params object[] args) where T:new()
		{
			// Only SELECT comes through here.
			// This is almost exactly the same as GetRow 
			// except it operates using the field map in the query.

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			using var connection = GetConnection();
			await connection.OpenAsync();
			var qryString = q.GetQuery(null, false, localeId, localeCode);
			var cmd = CreateCommand(connection, qryString, args);

			using var reader = await cmd.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
			{
				return default;
			}

			// Create the object: 
			var result = new T();

			// For each field..
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var value = reader.GetValue(i);

				if (value is System.DBNull)
				{
					continue;
				}

				var field = q.Fields[i];

				if (field.Type == typeof(bool))
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

			return result;
		}

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as the given object, along with the total number of results.
		/// </summary>
		/// <param name="context"></param>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="filter">
		/// A runtime filter to apply to the query. It's the same as WHERE.
		/// These filters often handle permission based filtering. 
		/// Pass it to Capability.IsGranted to have that happen automatically.</param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<ListWithTotal<T>> ListWithTotal<T>(Context context, Query<T> q, Filter filter, params object[] args) where T : new()
		{
			var results = new List<T>();

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;
				var locale = (Locales != null && localeId <= Locales.Length ? Locales[localeId - 1] : null);
				localeCode = locale?.Code;
			}

			bool paginationActive = filter != null && filter.PageSize != 0;
			int? total = 0;

			using (var connection = GetConnection())
			{
				await connection.OpenAsync();

				// When pagination is active and a total has to be calculated separately, qryText is actually a comma separated double query.
				var qryText = q.GetQuery(filter, false, localeId, localeCode, paginationActive);

				MySqlCommand cmd;

				// If the filter has additional value resolvers..
				if (filter != null && filter.ParamValueResolvers != null)
				{
					// This means the filter wants some additional values to be calculated when it's called.
					// So, go ahead and invoke that now:
					cmd = new MySqlCommand(qryText, connection);
					MySqlParameter parameter;

					for (var i = 0; i < filter.ParamValueResolvers.Count; i++)
					{
						var valueResolver = filter.ParamValueResolvers[i];

						parameter = cmd.CreateParameter();
						parameter.ParameterName = "v" + valueResolver.ParamId;
						cmd.Parameters.Add(parameter);

						if (valueResolver != null)
						{
							parameter.Value = await valueResolver.Method(context);
						}
					}

					if (args != null)
					{
						for (var i = 0; i < args.Length; i++)
						{
							parameter = cmd.CreateParameter();
							parameter.ParameterName = "p" + i;
							parameter.Value = args[i];
							cmd.Parameters.Add(parameter);
						}
					}
				}
				else
				{
					cmd = CreateCommand(connection, qryText, args);
				}

				using var reader = await cmd.ExecuteReaderAsync();
				if (paginationActive)
				{
					await reader.ReadAsync();
					var count = (long)reader.GetValue(0);
					total = (int)count;

					await reader.NextResultAsync();
				}

				while (await reader.ReadAsync())
				{
					// Create the object: 
					var result = new T();

					// For each field..
					for (var i = 0; i < reader.FieldCount; i++)
					{
						var value = reader.GetValue(i);

						if (value is System.DBNull)
						{
							continue;
						}

						var field = q.Fields[i];

						if (field.Type == typeof(bool))
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

					results.Add(result);
				}
			}

			if (!paginationActive)
			{
				total = results.Count;
			}

			return new ListWithTotal<T>() {
				Results = results,
				Total = total
			};
		}

		/// <summary>
		/// Runs the given query with the given args to bind. Returns the results mapped as a list of the given type.
		/// </summary>
		/// <param name="context"></param>
		/// <typeparam name="T"></typeparam>
		/// <param name="q"></param>
		/// <param name="filter">
		/// A runtime filter to apply to the query. It's the same as WHERE.
		/// These filters often handle permission based filtering. 
		/// Pass it to Capability.IsGranted to have that happen automatically.</param>
		/// <param name="args"></param>
		/// <returns></returns>
		public async Task<List<T>> List<T>(Context context, Query<T> q, Filter filter, params object[] args) where T : new()
		{
			var results = new List<T>();

			uint localeId = 0;
			string localeCode = null;
			if (context != null && context.LocaleId > 1)
			{
				localeId = context.LocaleId;

				if (Locales != null && localeId > 0 && localeId <= Locales.Length)
				{
					var locale = Locales[localeId - 1];
					if (locale != null)
					{
						localeCode = locale.Code;
					}
				}
			}

			using (var connection = GetConnection())
			{
				await connection.OpenAsync();
				var qryText = q.GetQuery(filter, false, localeId, localeCode);

				MySqlCommand cmd;

				// If the filter has additional value resolvers..
				if (filter != null && filter.ParamValueResolvers != null)
				{
					// This means the filter wants some additional values to be calculated when it's called.
					// So, go ahead and invoke that now:
					cmd = new MySqlCommand(qryText, connection);
					MySqlParameter parameter;

					for (var i = 0; i < filter.ParamValueResolvers.Count; i++)
					{
						var valueResolver = filter.ParamValueResolvers[i];
						
						parameter = cmd.CreateParameter();
						parameter.ParameterName = "v" + valueResolver.ParamId;
						cmd.Parameters.Add(parameter);

						if (valueResolver != null)
						{
							parameter.Value = await valueResolver.Method(context);
						}
					}

					if (args != null)
					{
						for (var i = 0; i < args.Length; i++)
						{
							parameter = cmd.CreateParameter();
							parameter.ParameterName = "p" + i;
							parameter.Value = args[i];
							cmd.Parameters.Add(parameter);
						}
					}
				}
				else
				{
					cmd = CreateCommand(connection, qryText, args);
				}

				using var reader = await cmd.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					// Create the object: 
					var result = new T();

					// For each field..
					for (var i = 0; i < reader.FieldCount; i++)
					{
						var value = reader.GetValue(i);

						if (value is System.DBNull)
						{
							continue;
						}

						var field = q.Fields[i];

						if (field.Type == typeof(bool))
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

					results.Add(result);
				}
			}

			return results;
		}
		
    }
}
