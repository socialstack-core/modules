using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.DatabaseDiff
{
	/// <summary>
	/// Represents a changed database column.
	/// </summary>
	public class ChangedColumn
	{
		/// <summary>
		/// The column as it currently is in the database.
		/// </summary>
		public DatabaseColumnDefinition FromColumn;

		/// <summary>
		/// The column as it will be.
		/// </summary>
		public DatabaseColumnDefinition ToColumn;

	}
}
