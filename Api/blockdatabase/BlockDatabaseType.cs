using System;
using System.Collections.Generic;

namespace Api.BlockDatabase
{
	/// <summary>
	/// Declares a field type within a blockchain. uint, string etc
	/// </summary>
	public class BlockDatabaseType
	{
		/// <summary>
		/// The name of the type - uint, string etc.
		/// </summary>
		public string TypeName;

		/// <summary>
		/// True if this type is unsigned.
		/// </summary>
		public bool IsUnsigned;
		
		/// <summary>
		/// Creates a new database type definition.
		/// </summary>
		/// <param name="typeName"></param>
		public BlockDatabaseType(string typeName)
		{
			TypeName = typeName;
		}

		/// <summary>
		/// Creates a new database type definition.
		/// </summary>
		public BlockDatabaseType(string typeName, bool isUnsigned)
		{
			TypeName = typeName;
			IsUnsigned = isUnsigned;
		}
		
	}
	
}