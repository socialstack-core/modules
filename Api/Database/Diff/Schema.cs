using Api.Database;
using System;
using System.Collections.Generic;

namespace Api.Database
{
	/// <summary>
	/// A database schema.
	/// Used during startup table sync.
	/// </summary>
	public partial class Schema
	{
		/// <summary>
		/// The tables in this schema.
		/// </summary>
		public Dictionary<string, DatabaseTableDefinition> Tables = new Dictionary<string, DatabaseTableDefinition>();

		/// <summary>
		/// Gets a column or null if it didn't exist.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public DatabaseColumnDefinition GetColumn(string table, string column)
		{
			var tableDef = GetTable(table);

			if (tableDef == null)
			{
				return null;
			}

			return tableDef.GetColumn(column);
		}

		/// <summary>
		/// Gets a table by its case insensitive name. Creates it if it doesn't exist.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="createIfNotExists"></param>
		/// <returns></returns>
		public DatabaseTableDefinition GetTable(string name, bool createIfNotExists = false)
		{
			var lowerName = name.ToLower();
			DatabaseTableDefinition result;
			if (!Tables.TryGetValue(lowerName, out result) && createIfNotExists)
			{
				result = new DatabaseTableDefinition()
				{
					TableName = name
				};
				Tables[lowerName] = result;
			}

			return result;
		}

		/// <summary>
		/// Add a column to the schema. Returns null if the column was ignored due to the dbfield attribute.
		/// </summary>
		/// <returns></returns>
		public virtual DatabaseColumnDefinition AddColumn(Field fromField)		{
			var dcd = new DatabaseColumnDefinition(fromField, fromField.OwningTypeName);
			if (dcd.Ignore)
			{
				return null;
			}

			Add(dcd);
			return dcd;
		}
			
		/// <summary>
		/// Compares this schema with the given "newer" one to declare tables that have been added etc.
		/// </summary>
		/// <param name="newSchema"></param>
		/// <returns></returns>
		public DiffSet<DatabaseTableDefinition, DiffSet<DatabaseColumnDefinition, ChangedColumn>> Diff(Schema newSchema)
		{
			var result = new DiffSet<DatabaseTableDefinition, DiffSet<DatabaseColumnDefinition, ChangedColumn>>();

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

					if (tableDiff.Added.Count > 0 || tableDiff.Changed.Count > 0) {
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
			table.AddColumn(column);
		}

		/// <summary>
		/// Removes the given column definition from this schema.
		/// </summary>
		/// <param name="column"></param>
		public void Remove(DatabaseColumnDefinition column)
		{
			var table = GetTable(column.TableName, false);

			if (table == null)
			{
				return;
			}

			table.RemoveColumn(column);
		}
	}
	
}