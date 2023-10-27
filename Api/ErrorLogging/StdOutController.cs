using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Api.ErrorLogging;

namespace Api.Startup;

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class StdOutController : ControllerBase
{
	
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

		writer.WriteASCII("{\"log\":\"Obsolete endpoint. Use the /v1/monitoring/log endpoint instead of this stdout one.\"");

		writer.Write((byte)'}');

		// Flush after each one:
		await writer.CopyToAsync(Response.Body);
		writer.Release();
	}

	/// <summary>
	/// Gets the latest block of text from the stdout.
	/// </summary>
	[HttpPost("log")]
	public async ValueTask GetLog([FromBody] LogFilteringModel filtering)
	{
		var context = await Request.GetContext();

		Response.ContentType = _applicationJson;

		if (context.Role == null || !context.Role.CanViewAdmin)
		{
			throw PermissionException.Create("monitoring_log", context);
		}

		// Row limiter.
		var rowCount = 0;

		var writer = Writer.GetPooled();
		writer.Start(null);

		writer.WriteASCII("{\"results\":[");

		bool first = true;

		await Log.ReadSelfBackwards((LogTransactionReader reader) => {

			if (rowCount > 300)
			{
				reader.Halt = true;
				return;
			}

			rowCount++;

			if (first)
			{
				first = false;
			}
			else
			{
				writer.Write((byte)',');
			}

			writer.WriteASCII("{\"type\":\"");

			// ok, info, warn, error, fatal
			switch (reader.Definition.Id)
			{
				case Api.ErrorLogging.Schema.OkId:
					writer.WriteASCII("ok");
					break;
				case Api.ErrorLogging.Schema.InfoId:
					writer.WriteASCII("info");
					break;
				case Api.ErrorLogging.Schema.WarnId:
					writer.WriteASCII("warn");
					break;
				case Api.ErrorLogging.Schema.ErrorId:
					writer.WriteASCII("error");
					break;
				case Api.ErrorLogging.Schema.FatalId:
					writer.WriteASCII("fatal");
					break;
			}

			ulong timestamp = 0;
			string tag = "";
			int found = 0;

			// Timestamp is usually field 0 and tag is field 1. We'll keep it generic though with a loop:
			// (NB: This is not currently the case as the fields are incorrectly sorted backwards in the field array).
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var dataField = reader.Fields[i];

				if (dataField.Field.Id == Api.ErrorLogging.Schema.TimestampFieldDefId)
				{
					timestamp = dataField.NumericValue;
					found++;
				}
				else if (dataField.Field.Id == Api.ErrorLogging.Schema.TagFieldDefId)
				{
					tag = dataField.GetNativeString();
					found++;
				}

				if (found == 2)
				{
					break;
				}
			}

			writer.WriteASCII("\",\"createdUtc\":");
			writer.WriteS(timestamp/ 10000);
			writer.WriteASCII(",\"tag\":");
			writer.WriteEscaped(tag);
			writer.WriteASCII(",\"messages\":[");

			var open = false;

			// N message fields and a message optionally has a stack trace. Fields MUST be of the order Message then StackTrace.
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var dataField = reader.Fields[i];

				if (dataField.Field.Id == Api.ErrorLogging.Schema.MessageFieldDefId)
				{
					if (open)
					{
						writer.WriteASCII("},");
					}
					else
					{
						open = true;
					}

					writer.WriteASCII("{\"entry\":");
					writer.WriteEscaped(dataField.GetNativeString());

					// If prev field is a stack trace, put it in too.
					if (i > 0 && reader.Fields[i - 1].Field.Id == Api.ErrorLogging.Schema.StackTraceFieldDefId)
					{
						writer.WriteASCII(",\"trace\":");
						writer.WriteEscaped(reader.Fields[i - 1].GetNativeString());
					}
				}
			}

			if (open)
			{
				writer.WriteASCII("}");
			}

			writer.WriteASCII("]}");

		});

		writer.WriteASCII("]}");

		// Flush after each one:
		await writer.CopyToAsync(Response.Body);
		writer.Release();
	}

}

/// <summary>
/// A filtering model used by the /monitoring/log endpoint.
/// </summary>
public class LogFilteringModel
{
	/// <summary>
	/// Will only return results with a timestamp greater than the specified one.
	/// Use this to get results created since a previous request.
	/// </summary>
	public long NewerThan;

	/// <summary>
	/// Starting offset (from the tail of the file).
	/// </summary>
	public uint Offset = 0;

	/// <summary>
	/// # of log entries to obtain.
	/// </summary>
	public uint PageSize = 1000;

	/// <summary>
	/// This node only.
	/// </summary>
	public bool LocalOnly = true;

	/// <summary>
	/// Basic filter by tag for the moment.
	/// </summary>
	public string Tag;
}