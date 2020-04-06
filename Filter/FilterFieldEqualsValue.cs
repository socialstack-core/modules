using System;
using System.Reflection;
using System.Text;
using Api.Contexts;
using Api.Database;


namespace Api.Permissions
{

	/// <summary>
	/// Checks if a field matches some value in the context.
	/// </summary>
	public partial class FilterFieldEqualsValue
	{
		/// <summary>
		/// Builds this filter node as a query string, writing it into the given string builder.
		/// If a variable is outputted then a value reader is pushed in the given arg set.
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		/// <param name="useTableNames">True if table names should be used instead of type names.</param>
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset, bool useTableNames, string localeCode)
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

			if (Localise && localeCode != null)
			{
				builder.Append('_');
				builder.Append(localeCode);
			}
			
			// Equal the value:
			builder.Append("=@p");
			builder.Append(ArgIndex);
		}
	}

}