using System;
using System.Text;
using Api.Contexts;
using Api.Database;


namespace Api.Permissions
{

	/// <summary>
	/// A filter method which is active if the current user (or their ID) is provided.
	/// </summary>
	public partial class FilterIfSelf : FilterFieldEquals
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset)
		{
			builder.Append(Type.TableName());
			builder.Append('.');
			builder.Append(Field);
			builder.Append("=@user");
		}
	}

}