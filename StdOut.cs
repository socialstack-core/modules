using Api.Contexts;
using Api.Database;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Api.Startup
{
	
	/// <summary>
	/// Handles cloned console data such that it can be accessed more easily by admins.
	/// </summary>
	public static class StdOut
	{
		/// <summary>
		/// Underlying console writer.
		/// </summary>
		public static ConsoleWriter Writer;
	}
	
	/// <summary>
	/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
	/// Not required to use these - you can also just directly use ControllerBase if you want.
	/// Like AutoService this isn't in a namespace due to the frequency it's used.
	/// </summary>
	[Route("v1/monitoring")]
	[ApiController]
	public partial class StdOutController : ControllerBase
	{
		
		/// <summary>
		/// Json header
		/// </summary>
		private readonly static string _applicationJson = "application/json";

		/// <summary>
		/// Forces a GC run. Convenience for testing for memory leaks.
		/// </summary>
		[HttpGet("gc")]
		public async ValueTask GC()
		{
			var context = await Request.GetContext();

			if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
			{
				throw PermissionException.Create("monitoring_query", context);
			}
			
			System.GC.Collect();
		}

		/// <summary>
		/// Forces an application halt. On a deployed server, the service runner (usually systemd) will then start it again.
		/// </summary>
		[HttpGet("halt")]
		public async ValueTask Halt()
		{
			var context = await Request.GetContext();

			if (context.Role == null || !context.Role.CanViewAdmin || context.Role.Id != 1)
			{
				throw PermissionException.Create("monitoring_query", context);
			}

			Console.WriteLine("Halting immediately by developer user request (user #" + context.UserId + ")");
			Environment.Exit(100);
		}

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

			var db = Services.Get<DatabaseService>();

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

		/// <summary>
		/// Gets the latest block of text from the stdout.
		/// </summary>
		[HttpGet("stdout")]
		public async ValueTask GetStdOut()
		{
			var context = await Request.GetContext();
			
			Response.ContentType = _applicationJson;
			
			if(context.Role == null || !context.Role.CanViewAdmin)
			{
				throw PermissionException.Create("monitoring_stdout", context);
			}
			
			var writer = Writer.GetPooled();
			writer.Start(null);

			writer.WriteASCII("{\"log\":");

			writer.WriteEscaped(StdOut.Writer.GetLatest());

			writer.Write((byte)'}');

			// Flush after each one:
			await writer.CopyToAsync(Response.Body);
			writer.Release();
		}
		
	}

	/// <summary>
	/// A query model used by the /monitoring/query endpoint.
	/// </summary>
	public class MonitoringQueryModel
	{

		/// <summary>
		/// The database query to run.
		/// </summary>
		public string Query;

	}

}