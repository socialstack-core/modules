using System;
using System.Reflection;
using System.Text;
using Api.Contexts;
using Api.Database;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field in an arg matches a set of values.
	/// </summary>
	public partial class FilterFieldEqualsSet : FilterNode
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
			builder.Append("` IN (");
			
			if (Values != null)
			{
				var first = true;

				foreach (var val in Values)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						builder.Append(',');
					}

					if (val == null)
					{
						builder.Append("NULL");
					}
					else
					{
						builder.Append('"');
						builder.Append(MySql.Data.MySqlClient.MySqlHelper.EscapeString(val.ToString()));
						builder.Append('"');
					}

				}
			}

			builder.Append(')');
		}
	}

}