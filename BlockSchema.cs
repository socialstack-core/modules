using Api.Database;
using System;

namespace Api.BlockDatabase;

/// <summary>
/// Used to declare a blockchain schema to SS
/// </summary>
public class BlockSchema : Api.Database.Schema
{
	/// <summary>
	/// Add a column to the schema. Returns null if the column was ignored due to the dbfield attribute.
	/// </summary>
	/// <returns></returns>
	public override DatabaseColumnDefinition AddColumn(Field fromField, Type parentType)
	{
		// Create a column definition:
		var columnDefinition = new BlockDatabaseColumnDefinition(fromField, parentType);

		if (columnDefinition.Ignore)
		{
			return null;
		}

		// Add:
		Add(columnDefinition);
		return columnDefinition;
	}

}