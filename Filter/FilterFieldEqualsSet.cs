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
		/// <param name="localeCode">Optional localeCode used when a request is for e.g. French fields instead. 
		/// It would be e.g. "fr" and just matches whatever your Locale.Code is.</param>
		public override void BuildQuery(StringBuilder builder, int paramOffset, string localeCode)
		{
			builder.Append('`');
			builder.Append(Type.Name);
			builder.Append("`.`");
			builder.Append(Field);
			
			if(Localise && localeCode != null){
				builder.Append('_');
				builder.Append(localeCode);
			}
			
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
					else if (val is bool)
					{
						builder.Append("=");
						builder.Append(MySql.Data.MySqlClient.MySqlHelper.EscapeString(val.ToString()));
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