using System;
using System.Reflection;
using System.Text;
using Api.Contexts;
using Api.Database;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field matches some other field.
	/// </summary>
	public partial class FilterFieldEqualsField : FilterNode
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
				builder.Append(TypeA.TableName());
			}
			else
			{
				builder.Append(TypeA.Name);
			}
			builder.Append("`.`");
			builder.Append(FieldA);
			// Equal type B:
			builder.Append("`=`");
			if (useTableNames)
			{
				builder.Append(TypeB.TableName());
			}
			else
			{
				builder.Append(TypeB.Name);
			}
			builder.Append("`.`");
			builder.Append(FieldB);
			builder.Append('`');
		}
	}

}