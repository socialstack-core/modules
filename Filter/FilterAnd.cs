using System;
using System.Text;
using Api.Contexts;


namespace Api.Permissions
{
	public partial class FilterAnd : FilterTwoInput
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset)
		{
			builder.Append('(');
			Input0.BuildQuery(builder, paramOffset);
			builder.Append(") AND (");
			Input1.BuildQuery(builder, paramOffset);
			builder.Append(')');
		}
	}

}