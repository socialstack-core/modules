using System.Text;
using Api.Contexts;
using Api.Database;

namespace Api.Permissions
{

	/// <summary>
	/// A filter method which does a Join to some other type.
	/// </summary>
	public partial class FilterJoin : FilterNode
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset)
		{
			builder.Append(" JOIN ");
			builder.Append(TargetType.TableName());
			builder.Append(" AS ");
			builder.Append(TargetType.Name);
			builder.Append(" ON (");
			On.Construct().BuildQuery(builder, paramOffset);
			builder.Append(')');
		}
	}

}