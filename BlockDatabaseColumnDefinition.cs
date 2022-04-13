using System;
using System.Collections.Generic;
using Api.Database;
using System.Reflection;


namespace Api.BlockDatabase
{
	/// <summary>
	/// A column definition within a database.
	/// </summary>
	public partial class BlockDatabaseColumnDefinition : DatabaseColumnDefinition
	{
		/// <summary>
		/// The format data type, e.g. "varchar".
		/// </summary>
		public string DataType;

		/// <summary>
		/// True if this is a numeric field which is unsigned.
		/// </summary>
		public bool IsUnsigned;
		
		
		private static Dictionary<Type, BlockDatabaseType> TypeMap;
		
		static BlockDatabaseColumnDefinition()
		{
			TypeMap = new Dictionary<Type, BlockDatabaseType>();
			
			TypeMap[typeof(string)] = new BlockDatabaseType("string");
			TypeMap[typeof(byte[])] = new BlockDatabaseType("bytes");
			TypeMap[typeof(bool)] = new BlockDatabaseType("uint", true);
			TypeMap[typeof(sbyte)] = new BlockDatabaseType("int");
			TypeMap[typeof(byte)] = new BlockDatabaseType("uint", true);
			TypeMap[typeof(int)] = new BlockDatabaseType("int");
			TypeMap[typeof(uint)] = new BlockDatabaseType("uint", true);
			TypeMap[typeof(short)] = new BlockDatabaseType("int");
			TypeMap[typeof(ushort)] = new BlockDatabaseType("uint", true);
			TypeMap[typeof(long)] = new BlockDatabaseType("int");
			TypeMap[typeof(ulong)] = new BlockDatabaseType("uint", true);
			TypeMap[typeof(float)] = new BlockDatabaseType("float4");
			TypeMap[typeof(DateTime)] = new BlockDatabaseType("uint", true);
			TypeMap[typeof(double)] = new BlockDatabaseType("float8");
		}

		/// <summary>
		/// The type used by the table this is in.
		/// </summary>
		public Type TableType;

		/// <summary>
		/// The field on the type to write to/ read from.
		/// </summary>
		public Field Field;

		/// <summary>
		/// True if this is a private chain field.
		/// </summary>
		public bool IsPrivate;

		/// <summary>
		/// Creates a new column def
		/// </summary>
		public BlockDatabaseColumnDefinition(Field fromField, Type parentType) :this(fromField, parentType.Name.ToLower()) {
			TableType = parentType;
			Field = fromField;
		}

		/// <summary>
		/// Creates a new column def for the field
		/// </summary>
		public BlockDatabaseColumnDefinition(Field fromField, string lowerCaseTableName):base(fromField, lowerCaseTableName)
		{
			Field = fromField;
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
				if (fieldMeta.Ignore)
				{
					Ignore = true;
					return;
				}
			}

			// Next, which chain is this field intended for?
			// This depends on if the field has JsonIgnore or is "hidden" by default by the permission system fields.
			var jsonIgnoreAttrib = fromField.TargetField.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>();

			if (jsonIgnoreAttrib != null)
			{
				// Private chain field. Values for this field will be stored on the private chain only.
				IsPrivate = true;
			}
			else
			{
				var permsAttrib = fromField.TargetField.DeclaringType.GetCustomAttribute<Permissions.PermissionsAttribute>();

				if (permsAttrib != null && permsAttrib.HideFieldByDefault)
				{
					IsPrivate = true;
				}

				permsAttrib = fromField.TargetField.GetCustomAttribute<Permissions.PermissionsAttribute>();

				if (permsAttrib != null)
				{
					// This may override the declaration on the type:
					IsPrivate = permsAttrib.HideFieldByDefault;
				}
			}

			if (TypeMap.TryGetValue(fieldType, out BlockDatabaseType dbType))
			{
				DataType = dbType.TypeName;
				IsUnsigned = dbType.IsUnsigned;
			}
			else
			{
				throw new Exception(
					"Field '" + fromField.Name + "' on '" + fromField.TargetField.DeclaringType +
					"' is a " + fromField.Type.Name + " which isn't currently supported as a field type."
				);
			}
		}
		
		/// <summary>
		/// True if this columns definition has changed from the given newer one.
		/// </summary>
		/// <param name="dcd"></param>
		/// <returns></returns>
		public override bool HasChanged(DatabaseColumnDefinition dcd)
		{
			var newColumn = dcd as BlockDatabaseColumnDefinition;

			if (newColumn == null)
			{
				return false;
			}

			if (
				DataType != newColumn.DataType ||
				IsNullable != newColumn.IsNullable ||
				IsUnsigned != newColumn.IsUnsigned
			)
			{
				return true;
			}

			return false;
		}
	}
	
}