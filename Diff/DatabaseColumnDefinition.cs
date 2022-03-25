using System;
using System.Collections.Generic;
using Api.Database;

namespace Api.Database
{
	/// <summary>
	/// A column definition within a database.
	/// </summary>
	public partial class DatabaseColumnDefinition
	{
		/// <summary>
		/// The name of the table this column is in.
		/// </summary>
		public string TableName;

		/// <summary>
		/// The columns name.
		/// </summary>
		public string ColumnName;
		
		/// <summary>
		/// True if it's nullable.
		/// </summary>
		public bool IsNullable;
		
		/// <summary>
		/// True if this field should just be ignored.
		/// </summary>
		public bool Ignore { get; set; }

		/// <summary>
		/// C# type of the field value.
		/// </summary>
		public Type FieldType { get; set; }

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
			
			FieldType = fieldType;
		}

		/// <summary>
		/// True if this columns definition has changed from the given newer one.
		/// </summary>
		/// <param name="newColumn"></param>
		/// <returns></returns>
		public virtual bool HasChanged(DatabaseColumnDefinition newColumn)
		{
			throw new NotImplementedException();
		}
	}
	
}