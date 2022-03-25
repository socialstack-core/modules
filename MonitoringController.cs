using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;


namespace Api.Startup
{
	public partial class StdOutController : ControllerBase
	{
		
		/// <summary>
		/// Runs a query, returning the result set(s) as streaming JSON.
		/// Note that this will currently only work for MySQL database engines.
		/// </summary>
		/// <returns></returns>
		[HttpPost("query")]
		public async ValueTask RunQuery([FromBody] MonitoringQueryModel queryBody)
		{
			// Super admin (developer role) only.
			var context = await Request.GetContext();

			if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
			{
				throw PermissionException.Create("monitoring_query", context);
			}

			var db = Services.Get<MySQLDatabaseService>();

			using var connection = db.GetConnection();
			await connection.OpenAsync();
			var cmd = new MySqlCommand(queryBody.Query, connection);

			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{\"dbengine\":\"MySQL\"");
			
			try
			{
				var reader = await cmd.ExecuteReaderAsync();

				writer.WriteASCII(",\"sets\":[");

				var firstSet = true;
				var hasSet = true;

				while (hasSet)
				{
					if (firstSet)
					{
						firstSet = false;
					}
					else
					{
						// between result sets
						writer.Write((byte)',');
					}

					writer.WriteASCII("{\"affected\":");
					writer.WriteS(reader.RecordsAffected);
					writer.WriteASCII(",\"id\":");
					writer.WriteS(cmd.LastInsertedId);
					
					var firstInSet = true;

					while (await reader.ReadAsync())
					{
						// Got a record in this set.

						if (firstInSet)
						{
							firstInSet = false;

							writer.WriteASCII(",\"fields\":[");

							for (var i = 0; i < reader.FieldCount; i++)
							{
								if (i != 0)
								{
									writer.Write((byte)',');
								}

								writer.WriteEscaped(reader.GetName(i));
							}
							
							writer.WriteASCII("],\"results\":[");
						}
						else
						{
							writer.Write((byte)',');
						}

						writer.Write((byte)'[');

						for (var i = 0; i < reader.FieldCount; i++)
						{
							if (i != 0)
							{
								writer.Write((byte)',');
							}

							var fieldValue = reader.GetValue(i);


							if (fieldValue == null || fieldValue is System.DBNull )
							{
								writer.WriteASCII("null");
							}
							else
							{
								writer.WriteEscaped(fieldValue.ToString());
							}
						}

						writer.Write((byte)']');

						// Flush:
						await writer.CopyToAsync(Response.Body);
						writer.Reset(null);
					}

					if (firstInSet)
					{
						// No records at all. For compatibility, we'll return empty arrays, plus the set end bracket.
						writer.WriteASCII(",\"fields\":[],\"results\":[]}");
					}
					else
					{
						writer.WriteASCII("]}");
					}

					hasSet = await reader.NextResultAsync();
				}

				// End of sets
				writer.WriteASCII("]");
			}
			catch (MySqlException e)
			{
				// Special case for MySQL exception such that we can provide a clean readout.
				writer.WriteASCII(",\"error\":");
				writer.WriteEscaped("(" + e.Code + ") " + e.Message);
			}
			catch (Exception e)
			{
				writer.WriteASCII(",\"error\":");
				writer.WriteEscaped(e.ToString());
			}

			writer.Write((byte)'}');

			// Flush after each one:
			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}

	}
}