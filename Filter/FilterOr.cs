using System.Text;
using Api.Contexts;


namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if either of 2 inputs are active.
	/// </summary>
	public partial class FilterOr : FilterTwoInput
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
			builder.Append('(');
			Input0.BuildQuery(builder, paramOffset, useTableNames);
			builder.Append(") OR (");
			Input1.BuildQuery(builder, paramOffset, useTableNames);
			builder.Append(')');
		}
	}

}