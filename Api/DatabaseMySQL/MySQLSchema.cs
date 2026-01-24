using Api.Configuration;
using Api.Database;
using System;
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
		/// The table name to use for a particular type.
		/// This is generally used on types which are DatabaseRow instances.
		/// </summary>
		public static string TableName(string entityName)
		{
			// Just prefixed (e.g. sstack_Product by default):
			var name = AppSettings.DatabaseTablePrefix + entityName.ToLower();

			if (name.Length > 64)
			{
				name = name.Substring(0, 64);
			}

			return name;
		}
		
		/// <summary>
		/// Starts creating a specialised schema specific column object.
		/// </summary>
		/// <param name="field"></param>
		/// <param name="table"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public override DatabaseColumnDefinition StartColumn(Field field, string table)
		{
			return new MySQLDatabaseColumnDefinition(field, table);
		}
		
		/// <summary>
		/// Gets the table name to use for a given type, with an optional extension.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="extension"></param>
		/// <returns></returns>
		public override string GetTableName(Type type, string extension = null)
		{
			if(string.IsNullOrEmpty(extension))
			{
				return TableName(type.Name);
			}
			
			return TableName(type.Name) + "_" + extension;
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

		/// <summary>
		/// Add a column to the schema. Returns null if the column was ignored due to the dbfield attribute.
		/// </summary>
		/// <returns></returns>
		public override DatabaseColumnDefinition AddColumn(Field fromField)		{
			// Create a column definition:
			var columnDefinition = new MySQLDatabaseColumnDefinition(fromField, MySQLSchema.TableName(fromField.OwningTypeName));
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