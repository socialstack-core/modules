namespace Api.Database
{
	public partial class DatabaseTableDefinition
	{

		/// <summary>
		/// Generates a create table SQL command to add this table.
		/// </summary>
		/// <returns></returns>
		public string CreateTableSql()
		{
			var result = "CREATE TABLE `" + TableName + "` (\r\n";

			foreach (var col in Columns)
			{
				result += (col.Value as MySQLDatabaseColumnDefinition).CreateTableSql() + ",\r\n";
			}

			return result + "PRIMARY KEY (`Id`))";
		}

	}

}