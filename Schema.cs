using System.Collections.Generic;

namespace Api.DatabaseDiff
{
	/// <summary>
	/// A database schema.
	/// Used during startup table sync.
	/// </summary>
	public class Schema
	{
		/// <summary>
		/// The tables in this schema.
		/// </summary>
		private Dictionary<string, DatabaseTableDefinition> Tables = new Dictionary<string, DatabaseTableDefinition>();

		/// <summary>
		/// Gets a table by its case insensitive name. Creates it if it doesn't exist.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="createIfNotExists"></param>
		/// <returns></returns>
		public DatabaseTableDefinition GetTable(string name, bool createIfNotExists = false)
		{
			name = name.ToLower();
			DatabaseTableDefinition result;
			if (!Tables.TryGetValue(name, out result) && createIfNotExists)
			{
				result = new DatabaseTableDefinition()
				{
					TableName = name
				};
				Tables[name] = result;
			}

			return result;
		}

		/// <summary>
		/// Compares this schema with the given "newer" one to declare tables that have been added etc.
		/// </summary>
		/// <param name="newSchema"></param>
		/// <returns></returns>
		public DiffSet<DatabaseTableDefinition, DiffSet<DatabaseColumnDefinition, ChangedColumn>> Diff(Schema newSchema)
		{
			var result = new DiffSet<DatabaseTableDefinition, DiffSet<DatabaseColumnDefinition, ChangedColumn>>();

			// Any tables that have been completely removed in the new schema
			foreach (var kvp in Tables)
			{
				if (newSchema.GetTable(kvp.Key) == null)
				{
					// Removed.
					result.Removed.Add(kvp.Value);
				}
			}

			// Any tables that are completely new or changed
			foreach (var kvp in newSchema.Tables)
			{
				var existingTable = GetTable(kvp.Key);

				if (existingTable == null)
				{
					// Added.
					result.Added.Add(kvp.Value);
				}
				else
				{
					// Has it changed?
					var tableDiff = existingTable.Diff(kvp.Value);

					if (tableDiff.Added.Count > 0 || tableDiff.Changed.Count > 0 || tableDiff.Removed.Count > 0) {
						result.Changed.Add(tableDiff);
					}
				}
			}
			
			return result;
		}

		/// <summary>
		/// Adds the given columns to this schema, building up the individual tables as needed.
		/// </summary>
		/// <param name="columns"></param>
		public void Add(List<DatabaseColumnDefinition> columns)
		{
			foreach (var column in columns)
			{
				Add(column);
			}
		}

		/// <summary>
		/// Adds the given column definition to this schema, setting it into the correct table.
		/// </summary>
		/// <param name="column"></param>
		public void Add(DatabaseColumnDefinition column)
		{
			var table = GetTable(column.TableName, true);
			table.Columns[column.ColumnName.ToLower()] = column;
		}

		/// <summary>
		/// Generates SQL which will add all the tables in this schema. Requires multi-command capability.
		/// </summary>
		/// <returns></returns>
		public string CreateAllSql()
		{
			var result = "";
			foreach (var kvp in Tables)
			{
				if (result != "")
				{
					result += "\r\n\r\n";
				}
				
				result += kvp.Value.CreateTableSql();
			}

			return result;
		}
	}
	
}