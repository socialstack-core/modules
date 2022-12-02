using System;
using System.Reflection;
using System.Collections.Generic;


namespace Api.Database
{
	/// <summary>
	/// A database table definition.
	/// Used during startup table sync.
	/// </summary>
	public partial class DatabaseTableDefinition
	{
		/// <summary>
		/// The name of this table.
		/// </summary>
		public string TableName;

		/// <summary>
		/// The system type that this table relates to. I.e. if it's the user table, this is typeof(User)
		/// </summary>
		public Type OwningType;

		/// <summary>
		/// Group name from the defining type, if there is one.
		/// </summary>
		public string GetGroupName()
		{
			if (OwningType != null)
			{
				var dbf = OwningType.GetCustomAttribute<DatabaseFieldAttribute>();

				if (dbf != null)
				{
					return dbf.Group;
				}
			}
			return null;
		}

		/// <summary>
		/// Adds the given column to this table definition.
		/// </summary>
		/// <param name="column"></param>
		public void AddColumn(DatabaseColumnDefinition column)
		{
			if (OwningType == null)
			{
				OwningType = column.OwningType;
			}

			Columns[column.ColumnName.ToLower()] = column;
		}

		/// <summary>
		/// The columns in this table.
		/// </summary>
		public Dictionary<string, DatabaseColumnDefinition> Columns = new Dictionary<string, DatabaseColumnDefinition>();

		/// <summary>
		/// Gets a column by its case insensitive name. Null if it doesn't exist.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public DatabaseColumnDefinition GetColumn(string name)
		{
			name = name.ToLower();
			Columns.TryGetValue(name, out DatabaseColumnDefinition result);
			return result;
		}

		/// <summary>
		/// Compares this table to the given 'newer' definition.
		/// </summary>
		/// <param name="newTable"></param>
		/// <returns>Null if no changes have been detected.</returns>
		public DiffSet<DatabaseColumnDefinition, ChangedColumn> Diff(DatabaseTableDefinition newTable)
		{
			var result = new DiffSet<DatabaseColumnDefinition, ChangedColumn>();

			// Any tables that are completely new or changed
			foreach (var kvp in newTable.Columns)
			{
				var existingColumn = GetColumn(kvp.Key);
				var previousName = false;

				if (existingColumn == null)
				{
					// Does it have any previous column names?
					// If so, check if any of those exist.
					var prevNames = kvp.Value.PreviousNames;

					if (prevNames != null)
					{
						for (var i = 0; i < prevNames.Length; i++)
						{
							existingColumn = GetColumn(prevNames[i]);

							if (existingColumn != null)
							{
								previousName = true;
								break;
							}

						}
					}
				}
				
				if (existingColumn == null)
				{
					// Added.
					result.Added.Add(kvp.Value);
				}
				else
				{
					// Has it changed?
					if (previousName || existingColumn.HasChanged(kvp.Value))
					{
						result.Changed.Add(new ChangedColumn() {
							ToColumn = kvp.Value,
							FromColumn = existingColumn
						});
					}
				}
			}

			return result;
		}
	}
}