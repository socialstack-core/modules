using Api.Contexts;
using Api.Permissions;
using Api.SocketServerLibrary;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Api.ErrorLogging;
using Microsoft.AspNetCore.Http;

namespace Api.Startup;

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class StdOutController : AutoController
{
	
	/// <summary>
	/// Gets the latest block of text from the stdout.
	/// </summary>
	[HttpPost("log")]
	public async ValueTask GetLog(HttpContext httpContext, Context context, [FromBody] LogFilteringModel filtering)
	{
		var response = httpContext.Response;
		response.ContentType = _applicationJson;

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
			
			// the row count now takes the page size into account
			// use this at your own risk. (Defaults to 1k)
			if (rowCount > filtering.PageSize)
			{
				reader.Halt = true;
				return;
			}

			if (filtering.Levels is not null)
			{
				// if the log level isn't enabled, skip it. 
				switch (reader.Definition.Id)
				{
					case Schema.OkId:
						if (!filtering.Levels.Contains("ok"))
						{
							return;
						}
						break;
					case Schema.InfoId:
						if (!filtering.Levels.Contains("info"))
						{
							return;
						}
						break;
					case Schema.WarnId:
						if (!filtering.Levels.Contains("warn"))
						{
							return;
						}
						break;
					case Schema.ErrorId:
						if (!filtering.Levels.Contains("error"))
						{
							return;
						}
						break;
				}
			}
			
			// check if the exceptions only bool is true, if so and 
			// no StackTraceFieldDefId exists, we skip it.
			if (filtering.ExceptionsOnly && !reader.Fields.Any(field =>
				    field.Field is not null && field.Field.Id == Schema.StackTraceFieldDefId))
			{
				return;
			}
			
			// react warnings can be pesky when looking for errors
			// as eslint gives a "error" when rules of hooks are 
			// broken, unfortunately this happens a lot. 
			// my solution you ask? Ignorance is bliss. Nuke em.
			if (filtering.DisableReactWarnings && FieldContainsMessage("react", reader.Fields))
			{
				return;
			}
			
			// The console can contain a lot of "TSParenthesizedType" or "A type was ignored"
			// so we can filter out this noise.
			if (filtering.DisableTypeScriptInfo && (FieldContainsMessage("typescript", reader.Fields) || FieldContainsMessage("TSParenthesizedType", reader.Fields)))
			{
				return;
			}
			
			// allows a fulltext search
			if (!string.IsNullOrEmpty(filtering.QueryFilter) &&
			    !FieldContainsMessage(filtering.QueryFilter, reader.Fields))
			{
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
		await writer.CopyToAsync(response.Body);
		writer.Release();
	}

	/// <summary>
	/// Used to perform a search on the fields for certain keywords
	/// (Case-Insensitive)
	/// </summary>
	/// <param name="compare"></param>
	/// <param name="fields"></param>
	/// <returns></returns>
	private static bool FieldContainsMessage(string compare, FieldData[] fields)
	{
		foreach (var field in fields)
		{
			if (field.Field is not null && field.Field.Id == Schema.MessageFieldDefId)
			{
				try
				{
					var messageText = field.GetNativeString()?.ToLower();

					if (messageText is not null && messageText.Contains(compare, StringComparison.OrdinalIgnoreCase))
					{
						return true; // Skip this log entry
					}
				}
				catch (NullReferenceException) {}
			}
		}

		return false;
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
	/// Allow a range
	/// </summary>
	public long OlderThan;

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
	
	/// <summary>
	/// Filter by the log levels.
	/// </summary>
	public string[] Levels = ["error", "warn", "info", "ok"];
	
	/// <summary>
	/// Disable ESLint's react warnings
	/// such as rules of hooks, and
	/// incorrect hook usage. 
	/// </summary>
	public bool DisableReactWarnings = false;
	
	/// <summary>
	/// A textual filter to apply.
	/// </summary>
	public string QueryFilter;
	
	/// <summary>
	/// Toggles Exceptions only.
	/// </summary>
	public bool ExceptionsOnly = false;
	
	/// <summary>
	/// Disables typescript info such as:
	/// "A typescript type annotation was ignored"
	/// "TSParenthesizedType"
	/// </summary>
	public bool DisableTypeScriptInfo = false;
}