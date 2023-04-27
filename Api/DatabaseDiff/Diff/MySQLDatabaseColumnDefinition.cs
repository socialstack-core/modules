using System;
using System.Collections.Generic;
using Api.Database;

namespace Api.Database
{
	/// <summary>
	/// A column definition within a database.
	/// </summary>
	public partial class MySQLDatabaseColumnDefinition : DatabaseColumnDefinition
	{
		/// <summary>
		/// The format data type, e.g. "varchar".
		/// </summary>
		public string DataType;

		/// <summary>
		/// True if this is an auto-inc column. Usually only applies to Id.
		/// </summary>
		public bool IsAutoIncrement;

		/// <summary>
		/// Varchar and varbinary mainly - the max number of characters. varchar(x).
		/// </summary>
		public long? MaxCharacters;

		/// <summary>
		/// Typically used by e.g. decimal(MaxCharacters2, MaxCharacters). If you only specify one length, that'll be the amount of digits after the decimal point.
		/// </summary>
		public long? MaxCharacters2;
		
		/// <summary>
		/// True if this is a numeric field which is unsigned.
		/// </summary>
		public bool IsUnsigned;
		
		
		private static Dictionary<Type, DatabaseType> TypeMap;
		
		static MySQLDatabaseColumnDefinition()
		{
			TypeMap = new Dictionary<Type, DatabaseType>();
			
			TypeMap[typeof(string)] = new DatabaseType("text", "varchar", "longtext");
			TypeMap[typeof(byte[])] = new DatabaseType("blob", "varbinary", "longblob");
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
		/// Creates a new MySQL column def
		/// </summary>
		public MySQLDatabaseColumnDefinition() :base() {}

		/// <summary>
		/// Creates a new MySQL column def for the field
		/// </summary>
		public MySQLDatabaseColumnDefinition(Field fromField, string lowerCaseTableName):base(fromField, lowerCaseTableName)
		{
			var fieldType = FieldType;

			// Get metadata attributes:
			var metaAttribs = fromField.TargetField.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);
			DatabaseFieldAttribute fieldMeta = null;

			if (metaAttribs.Length > 0)
			{
				fieldMeta = (DatabaseFieldAttribute)metaAttribs[0];
			}

			if (fieldMeta != null)
			{
				if (fieldMeta.AutoIncWasSet)
				{
					IsAutoIncrement = fieldMeta.AutoIncrement;
				}

				if (fieldMeta.PreviousNames != null)
				{
					PreviousNames = fieldMeta.PreviousNames;
				}

				if (fieldMeta.Ignore)
				{
					Ignore = true;
					return;
				}
			}

			if (ColumnName == "Id")
			{
				// Special case for the ID column - check the main type if we're overriding the above auto-inc.
				metaAttribs = fromField.OwningType.GetCustomAttributes(typeof(DatabaseFieldAttribute), true);

				if (metaAttribs.Length > 0)
				{
					var classMeta = (DatabaseFieldAttribute)metaAttribs[0];

					// Got meta on the class itself - override now:
					if (classMeta.AutoIncWasSet)
					{
						IsAutoIncrement = classMeta.AutoIncrement;
					}
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

			if (TypeMap.TryGetValue(fieldType, out DatabaseType dbType))
			{
				if (MaxCharacters.HasValue)
				{
					if (MaxCharacters.Value >= dbType.LargeLengthThreshold)
					{
						DataType = dbType.TypeNameWithLargeLength;
					}
					else
					{
						DataType = dbType.TypeNameWithLength;
					}
				}
				else
				{
					DataType = dbType.TypeName;
				}
				IsUnsigned = dbType.IsUnsigned;
			}
			else
			{
				throw new Exception(
					"Field '" + fromField.Name + "' on '" + fromField.TargetField.DeclaringType +
					"' is a " + fromField.Type.Name + " which isn't currently supported as a databse field type."
				);
			}
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
		public string AlterTableSql(bool isChange = false, string prevName = null)
		{
			if (isChange)
			{
				return "ALTER TABLE `" + TableName + "` CHANGE COLUMN `" + (prevName == null ? ColumnName : prevName) + "` `" + ColumnName + "` " + TypeAsSql();
			}
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

		/// <summary>
		/// True if this columns definition has changed from the given newer one.
		/// </summary>
		/// <param name="dcd"></param>
		/// <returns></returns>
		public override bool HasChanged(DatabaseColumnDefinition dcd)
		{
			var newColumn = dcd as MySQLDatabaseColumnDefinition;

			if (newColumn == null)
			{
				return false;
			}

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
	}
	
}