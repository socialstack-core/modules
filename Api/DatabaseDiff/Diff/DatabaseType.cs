using System;
using System.Collections.Generic;

namespace Api.Database
{
	/// <summary>
	/// Declares a field type within a database. varchar, int, bigint etc.
	/// </summary>
	public class DatabaseType
	{
		/// <summary>
		/// Optional - used to declare an alternate database type to use if a length is present.
		/// For example, TypeName is "text" and this field is "varchar" - indicating that we'll use varchar
		/// if a field length is specified.
		/// </summary>
		public string TypeNameWithLength;

		/// <summary>
		/// Lengths above this threshold will trigger the column type to be TypeNameWithLargeLength instead.
		/// </summary>
		public int LargeLengthThreshold = 64000;

		/// <summary>
		/// Optional - used to declare an alternate database type to use if a length is present.
		/// For example, TypeName is "text" and this field is "varchar" - indicating that we'll use varchar
		/// if a field length is specified.
		/// </summary>
		public string TypeNameWithLargeLength;

		/// <summary>
		/// The name of the type - int, bigint, text etc.
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
		/// <param name="typeNameWithLength"></param>
		/// <param name="typeWithLargeLength"></param>
		public DatabaseType(string typeName, string typeNameWithLength = null, string typeWithLargeLength = null)
		{
			TypeNameWithLargeLength = typeWithLargeLength;
			TypeNameWithLength = typeNameWithLength;
			TypeName = typeName;
		}

		/// <summary>
		/// Creates a new database type definition.
		/// </summary>
		public DatabaseType(string typeName, bool isUnsigned)
		{
			TypeName = typeName;
			IsUnsigned = isUnsigned;
		}
		
	}
	
}