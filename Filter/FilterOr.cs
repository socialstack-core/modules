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
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset, string localeCode)
		{
			builder.Append('(');
			Input0.BuildQuery(builder, paramOffset, localeCode);
			builder.Append(") OR (");
			Input1.BuildQuery(builder, paramOffset, localeCode);
			builder.Append(')');
		}
	}

}