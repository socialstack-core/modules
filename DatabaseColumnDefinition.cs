using System;
using System.Collections.Generic;
using Api.Database;

namespace Api.DatabaseDiff
{
	/// <summary>
	/// A column definition within a database.
	/// </summary>
	public class DatabaseColumnDefinition
	{
		private static Dictionary<Type, DatabaseType> TypeMap;
		
		static DatabaseColumnDefinition()
		{
			TypeMap = new Dictionary<Type, DatabaseType>();
			
			TypeMap[typeof(string)] = new DatabaseType("text", "varchar");
			TypeMap[typeof(byte[])] = new DatabaseType("blob", "varbinary");
			TypeMap[typeof(bool)] = new DatabaseType("bit");
			TypeMap[typeof(sbyte)] = new DatabaseType("tinyint");
			TypeMap[typeof(byte)] = new DatabaseType("tinyint", true);
			TypeMap[typeof(int)] = new DatabaseType("int");
			TypeMap[typeof(uint)] = new DatabaseType("int", true);
			TypeMap[typeof(short)] = new DatabaseType("smallint");
			TypeMap[typeof(ushort)] = new DatabaseType("smallint", true);
			TypeMap[typeof(long)] = new DatabaseType("bigint");
			TypeMap[typeof(ulong)] = new DatabaseType("bigint", true);
			TypeMap[typeof(float)] = new DatabaseType("float");
			TypeMap[typeof(DateTime)] = new DatabaseType("datetime");
			TypeMap[typeof(double)] = new DatabaseType("double");
			TypeMap[typeof(decimal)] = new DatabaseType("decimal", "decimal");
		}

		/// <summary>
		/// The name of the table this column is in.
		/// </summary>
		public string TableName;

		/// <summary>
		/// The columns name.
		/// </summary>
		public string ColumnName;
		/// <summary>
		/// The SQL format data type, e.g. "varchar".
		/// </summary>
		public string DataType;

		/// <summary>
		/// True if it's nullable.
		/// </summary>
		public bool IsNullable;

		/// <summary>
		/// True if this is an auto-inc column. Usually only applies to Id.
		/// </summary>
		public bool IsAutoIncrement;

		/// <summary>
		/// True if this is a numeric field which is unsigned.
		/// </summary>
		public bool IsUnsigned;

		/// <summary>
		/// Varchar and varbinary mainly - the max number of characters. varchar(x).
		/// </summary>
		public long? MaxCharacters;

		/// <summary>
		/// Typically used by e.g. decimal(MaxCharacters2, MaxCharacters). If you only specify one length, that'll be the amount of digits after the decimal point.
		/// </summary>
		public long? MaxCharacters2;


		/// <summary>
		/// Create a new database column definition.
		/// </summary>
		public DatabaseColumnDefinition() { }

		/// <summary>
		/// Create a new database column definition from a given field for a particular table.
		/// </summary>
		public DatabaseColumnDefinition(Field fromField, string lowerCaseTableName)
		{
			TableName = lowerCaseTableName;
			ColumnName = fromField.Name;
			var fieldType = fromField.Type;

			// fromField.TargetField.DeclaringType
			
			// Get metadata attributes:
			var metaAttribs = fromField.TargetField.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);
			DatabaseFieldAttribute fieldMeta = null;

			if (metaAttribs.Length > 0)
			{
				fieldMeta = (DatabaseFieldAttribute)metaAttribs[0];
			}

			if (fieldMeta != null)
			{
				IsAutoIncrement = fieldMeta.AutoIncrement;
			}

			if (ColumnName == "Id")
			{
				// Special case for the ID column - check the main type if we're overriding the above auto-inc.
				metaAttribs = fromField.OwningType.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);

				if (metaAttribs.Length > 0) {
					var classMeta = (DatabaseFieldAttribute)metaAttribs[0];

					// Got meta on the class itself - override now:
					IsAutoIncrement = classMeta.AutoIncrement;
				}
			}
			
			if (!fieldType.IsValueType)
			{
				IsNullable = true;
			}
			else
			{
				var underlyingNullableType = Nullable.GetUnderlyingType(fieldType);

				if (underlyingNullableType != null)
				{
					fieldType = underlyingNullableType;
					IsNullable = true;
				}
			}
			
			if (fieldType == typeof(string) || fieldType == typeof(byte[]))
			{
				// Length might be applied. Check for it now:
				if (fieldMeta != null && fieldMeta.Length != 0)
				{
					MaxCharacters = fieldMeta.Length;
				}
				
			}else if(fieldType == typeof(decimal)){
				
				// Lengths might be applied. Check for them now:
				if (fieldMeta != null && (fieldMeta.Length != 0 || fieldMeta.Length2 != 0))
				{
					// After DP:
					MaxCharacters = fieldMeta.Length;
					
					// Before DP:
					MaxCharacters2 = fieldMeta.Length2 == 0 ? 10 : fieldMeta.Length2;
				}
				else
				{
					// After DP defaults to 2.
					MaxCharacters = 2;
					MaxCharacters2 = 10;
				}
				
			}
			
			DatabaseType dbType;
			
			if(TypeMap.TryGetValue(fieldType, out dbType))
			{
				DataType = MaxCharacters.HasValue ? dbType.TypeNameWithLength : dbType.TypeName;
				IsUnsigned = dbType.IsUnsigned;
			}
			else
			{
				throw new Exception(
					"Field '"+ fromField.Name + "' on '" + fromField.TargetField.DeclaringType + 
					"' is a " + fromField.Type.Name + " which isn't currently supported as a databse field type."
				);
			}
		}

		/// <summary>
		/// True if this columns definition has changed from the given newer one.
		/// </summary>
		/// <param name="newColumn"></param>
		/// <returns></returns>
		public bool HasChanged(DatabaseColumnDefinition newColumn)
		{
			if (
				DataType != newColumn.DataType ||
				IsNullable != newColumn.IsNullable ||
				IsAutoIncrement != newColumn.IsAutoIncrement ||
				IsUnsigned != newColumn.IsUnsigned
			)
			{
				return true;
			}

			// Varchar or varbinary - check MaxChars too:
			if (DataType == "varchar" || DataType == "varbinary")
			{
				return (MaxCharacters != newColumn.MaxCharacters);
			}else if(DataType == "decimal"){
				// 2 is the full length of the decimal, which is represented by MaxChars:
				return (MaxCharacters2 != newColumn.MaxCharacters2 || MaxCharacters != newColumn.MaxCharacters);
			}
			
			return false;
		}

		/// <summary>
		/// Gets e.g. "varchar(200) not null" - the data type as universal SQL.
		/// </summary>
		/// <returns></returns>
		public string TypeAsSql()
		{
			return DataType.Trim() + (((DataType == "varchar" || DataType == "varbinary") && MaxCharacters.HasValue) ? "(" + MaxCharacters + ")" : "") +
				(((DataType == "decimal") && MaxCharacters.HasValue) ? "(" + MaxCharacters2 + ", " + MaxCharacters + ")" : "") +
				(IsUnsigned ? " unsigned" : "") + (IsNullable ? " null" : " not null") 
				+ ((DataType == "datetime" && !IsNullable) ? " DEFAULT '1970-01-01 00:00:00'":"")
				+ (IsAutoIncrement ? " auto_increment" : "");
		}

		/// <summary>
		/// Generates alter table SQL for this column.
		/// </summary>
		/// <returns></returns>
		public string AlterTableSql()
		{
			return "ALTER TABLE `" + TableName + "` ADD COLUMN `" + ColumnName + "` " + TypeAsSql();
		}

		/// <summary>
		/// Generates SQL for use in a create table command.
		/// </summary>
		/// <returns></returns>
		public string CreateTableSql()
		{
			return "`" + ColumnName + "` " + TypeAsSql();
		}
	}
	
}