using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using System;
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