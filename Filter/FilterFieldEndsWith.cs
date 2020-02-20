using System;
using System.Reflection;
using System.Text;
using Api.Contexts;
using Api.Database;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg ends with a given value.
	/// </summary>
	public partial class FilterFieldEndsWith : FilterNode
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		/// <param name="useTableNames">True if table names should be used instead of type names.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset, bool useTableNames)
		{
			builder.Append('`');
			if (useTableNames)
			{
				builder.Append(Type.TableName());
			}
			else
			{
				builder.Append(Type.Name);
			}
			builder.Append("`.`");
			builder.Append(Field);
			builder.Append('`');

			if (AlwaysArgMatch)
			{
				// Using a runtime arg value
				builder.Append(" LIKE '%' + @p");
				builder.Append(paramOffset + ArgIndex);
			}
			else if (Value == null)
			{
				// Use a constant null value
				builder.Append(" IS NULL");
			}
			else
			{
				// Use a constant non-null value
				builder.Append(" LIKE \"%");
				builder.Append(MySql.Data.MySqlClient.MySqlHelper.EscapeString(Value.ToString()));
				builder.Append("\"");
			}
		}
	}

}