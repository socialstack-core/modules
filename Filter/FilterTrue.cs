using System;
using System.Text;
using Api.Contexts;


namespace Api.Permissions
{

	/// <summary>
	/// A filter node which just always returns true.
	/// </summary>
	public partial class FilterTrue : FilterNode
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset)
		{
			// Explicitly do nothing
		}
	}

}