using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Api.Contexts;
using Api.Users;
using Api.Database;


namespace Api.Permissions
{
	public partial class Filter
	{
		/// <summary>
		/// Builds this filter as an SQL query. Doesn't include any particular keyword.
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		/// <param name="useTableNames">True if table names should be used instead of type names.</param>
		public void BuildQuery(StringBuilder builder, int paramOffset, bool useTableNames)
		{
			var whereRoot = Construct();
			whereRoot.BuildQuery(builder, paramOffset, useTableNames);
		}

		/// <summary>
		/// Builds join where and limit as a query.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="paramOffset">A number to add to all emitted parameter @ refs.</param>
		/// <param name="useTableNames">True if table names should be used instead of type names.</param>
		public void BuildFullQuery(StringBuilder str, int paramOffset, bool useTableNames)
		{
			if (Joins != null)
			{
				for (var i = 0; i < Joins.Count; i++)
				{
					Joins[i].BuildQuery(str, paramOffset, useTableNames);
				}
			}



			if (HasContent)
			{
				str.Append(" WHERE ");
				var whereRoot = Construct();
				whereRoot.BuildQuery(str, paramOffset, useTableNames);
			}

            if (Sorts != null)
            {
                str.Append(" ORDER BY ");
                for (var i = 0; i < Sorts.Count; i++)
                {
                    Sorts[i].BuildQuery(str, paramOffset, useTableNames);
                }

            }

            if (PageSize != 0)
			{
				str.Append(" LIMIT ");
				str.Append(PageIndex * PageSize);
				str.Append(',');
				str.Append(PageSize);
			}
			else if (PageIndex != 0)
			{
				// Just being used as an offset.
				str.Append(" LIMIT ");
				str.Append(PageIndex);
			}

		}
	}

}