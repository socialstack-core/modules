using System;
using System.Reflection;
using System.Text;
using Api.Contexts;
using Api.Database;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg matches a given value.
	/// </summary>
	public partial class FilterFieldEquals : FilterNode
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset)
		{
			builder.Append('`');
			builder.Append(Type.Name);
			builder.Append("`.`");
			builder.Append(Field);
			builder.Append('`');

			if (AlwaysArgMatch)
			{
				// Using a runtime arg value
				builder.Append("=@p");
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
				builder.Append("=\"");
				builder.Append(MySql.Data.MySqlClient.MySqlHelper.EscapeString(Value.ToString()));
				builder.Append("\"");
			}
		}
	}

}