using Api.Database;
using System.Collections.Generic;

namespace Api.Database
{
	/// <summary>
	/// A database schema.
	/// Used during startup table sync.
	/// </summary>
	public class MySQLSchema : Schema
	{
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

		/// <summary>
		/// Add a column to the schema. Returns null if the column was ignored due to the dbfield attribute.
		/// </summary>
		/// <returns></returns>
		public override DatabaseColumnDefinition AddColumn(Field fromField, string lowerCaseTableName)
		{
			// Create a column definition:
			var columnDefinition = new MySQLDatabaseColumnDefinition(fromField, lowerCaseTableName);

			if (columnDefinition.Ignore)
			{
				return null;
			}
			
			// Add:
			Add(columnDefinition);
			return columnDefinition;
		}
	}
	
}