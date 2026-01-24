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
		
		
		private static Dictionary<Type, MySQLType> TypeMap;
		
		static MySQLDatabaseColumnDefinition()
		{
			TypeMap = new Dictionary<Type, MySQLType>();
			
			TypeMap[typeof(string)] = new MySQLType("text", "varchar", "longtext");
			TypeMap[typeof(JsonString)] = new MySQLType("json");
			TypeMap[typeof(MappingData)] = new MySQLType("json");
			TypeMap[typeof(byte[])] = new MySQLType("blob", "varbinary", "longblob");
			TypeMap[typeof(bool)] = new MySQLType("bit");
			TypeMap[typeof(sbyte)] = new MySQLType("tinyint");
			TypeMap[typeof(byte)] = new MySQLType("tinyint", true);
			TypeMap[typeof(int)] = new MySQLType("int");
			TypeMap[typeof(uint)] = new MySQLType("int", true);
			TypeMap[typeof(short)] = new MySQLType("smallint");
			TypeMap[typeof(ushort)] = new MySQLType("smallint", true);
			TypeMap[typeof(long)] = new MySQLType("bigint");
			TypeMap[typeof(ulong)] = new MySQLType("bigint", true);
			TypeMap[typeof(float)] = new MySQLType("float");
			TypeMap[typeof(DateTime)] = new MySQLType("datetime");
			TypeMap[typeof(double)] = new MySQLType("double");
			TypeMap[typeof(decimal)] = new MySQLType("decimal", "decimal");
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

			if (TypeMap.TryGetValue(fieldType, out MySQLType dbType))
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
		/// Sets if this column is auto-inc. Not supported on all DB engines.
		/// </summary>
		/// <param name="value"></param>
		public override void SetIsAutoIncrement(bool value)
		{
			IsAutoIncrement = value;
		}

		/// <summary>
		/// Gets the type description as an SQL string.
		/// </summary>
		/// <returns></returns>
		public string TypeAsSql()
		{
			string sqlType = DataType.Trim();
			if ((DataType == "varchar" || DataType == "varbinary") && MaxCharacters.HasValue)
			{
				sqlType += "(" + MaxCharacters + ")";
			}
			else if (DataType == "decimal" && MaxCharacters.HasValue && MaxCharacters2.HasValue)
			{
				sqlType += "(" + MaxCharacters2 + ", " + MaxCharacters + ")";
			}

			sqlType += IsUnsigned ? " unsigned" : "";
			sqlType += IsNullable ? " null" : " not null";

			if (!IsAutoIncrement)
			{
				sqlType += " DEFAULT " + GetDefaultValueForType();
			}

			sqlType += IsAutoIncrement ? " auto_increment" : "";

			return sqlType;
		}

		private string GetDefaultValueForType()
		{
			if (IsNullable)
			{
				// Includes the string and json types.
				return "null";
			}

			return DataType switch
			{
				"bit" => "0",
				"tinyint" => "0",
				"int" => "0",
				"smallint" => "0",
				"bigint" => "0",
				"float" => "0",
				"datetime" => "'1970-01-01 00:00:00'",
				"date" => "'1970-01-01'",
				"double" => "0",
				"decimal" => "0",
				_ => "0" // fallback
			};
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