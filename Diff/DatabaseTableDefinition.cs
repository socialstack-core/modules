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

				if (existingColumn == null)
				{
					// Added.
					result.Added.Add(kvp.Value);
				}
				else
				{
					// Has it changed?
					if (existingColumn.HasChanged(kvp.Value))
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