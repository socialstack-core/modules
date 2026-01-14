using Api.Contexts;
using Api.CsvExport;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Startup.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class AutoController<T,ID>
{
	/// <summary>
	/// GET /v1/entityTypeName/list.csv
	/// Lists filtered entities available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list.csv")]
	public virtual async ValueTask ListCSV(
		Context context, HttpResponse response, 
		[FromQuery] string query = null,
		[FromQuery] string args = null,
		[FromQuery] string includes = null, 
		[FromQuery] string fileName = null)
	{
		ListFilter filters = null;

		if (!string.IsNullOrEmpty(query))
		{
			filters = new ListFilter()
			{
				Query = query,
				Args = JsonConvert.DeserializeObject<List<object>>(args)
			};
		}

		var filter = _service.LoadFilter(filters) as Filter<T, ID>;
		
		if (filter == null)
		{
			response.StatusCode = 404;
			return;
		}

		var results = await filter.ListAll(context);

		var csvMapping = await _service.GetCsvMapping(context);

		string name;

		if (!string.IsNullOrEmpty(fileName))
		{
			fileName = fileName.Trim();

			if (!IsValidCsvFileNameRegex(fileName))
			{
				throw new PublicException("Invalid file name", "filename/requires_csv");
			}

			// Ok:
			name = fileName;
		}
		else
		{
			name = typeof(T).Name + ".csv";
		}

		response.ContentType = "text/csv";
		response.Headers.ContentDisposition = "attachment; filename=" + name;
		await csvMapping.OutputStream(results, response.Body);
	}

	// Regex pattern: 
	// ^                   - Start of the string
	// [a-zA-Z0-9_-]+      - One or more allowed characters (letters, numbers, hyphen, underscore)
	// (\.csv)             - Exactly one dot followed by 'csv'
	// $                   - End of the string
	// RegexOptions.IgnoreCase - Makes the check case-insensitive (e.g., works for .CSV)
	private static readonly Regex CsvNameRegex = new Regex(
		@"^[a-zA-Z0-9_-]+(\.csv)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	/// <summary>
	/// Validates if a string is a safe and correctly formatted CSV file name 
	/// using a whitelist Regular Expression.
	/// </summary>
	/// <param name="fileName">The file name provided by the user.</param>
	/// <returns>True if the name is valid; otherwise, false.</returns>
	private static bool IsValidCsvFileNameRegex(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return false;
		}

		// We use Trim() here to allow users to accidentally include leading/trailing spaces
		// but the core Regex must match the content *after* trimming.
		return CsvNameRegex.IsMatch(fileName.Trim());
	}
}

public partial class AutoService<T, ID> {

	/// <summary>
	/// CSV file mappings
	/// </summary>
	private CsvMapping<T, ID>[] _csvMappings = null;
	private Type _csvMappingIT = null;

	/// <summary>
	/// Gets a CSV file mapping
	/// </summary>
	/// <returns></returns>
	public async ValueTask<CsvMapping<T, ID>> GetCsvMapping(Context ctx)
	{
		var roleId = ctx.RoleId;

		if (_csvMappingIT != InstanceType)
		{
			// Instance type has changed or is being set for the first time. Clear all cached CSV mappings implicitly.
			_csvMappings = null;
			_csvMappingIT = InstanceType;
		}

		var size = _csvMappings == null ? 0 : _csvMappings.Length;

		if (size < roleId)
		{
			lock (structureLock)
			{
				// Check again, just in case a thread we were waiting for has already done what we need.
				if (size < roleId)
				{
					if (_csvMappings == null)
					{
						_csvMappings = new CsvMapping<T, ID>[roleId];
					}
					else if (roleId > _csvMappings.Length)
					{
						Array.Resize(ref _csvMappings, (int)roleId);
					}
				}
			}
		}

		var index = roleId - 1;
		var structure = _csvMappings[index];

		if (structure == null)
		{
			// Note that multiple threads can build the structure simultaneously because we apply the created structure set afterwards.
			// It has no other side effects though so its a non-issue if it happens.

			// Get the json structure:
			var jsonStructure = await GetTypedJsonStructure(ctx);

			structure = new CsvMapping<T, ID>();

			await structure.BuildFrom(ctx, jsonStructure, EventGroup.BeforeCsvGettable);

			lock (structureLock)
			{
				// In the event that multiple threads have been making it at the same time, this check 
				// just ensures we're not using multiple different structures and are just using one of them.
				var existing = _csvMappings[index];
				if (existing == null)
				{
					_csvMappings[index] = structure;
				}
				else
				{
					structure = existing;
				}
			}
		}

		return structure;
	}
}


namespace Api.Eventing
{

	/// <summary>
	/// A grouping of common events, such as before/ after create, update, delete etc.
	/// These are typically added to the Events class, named directly after the type that is being used.
	/// Like this:
	/// public static EventGroup{Page} Page;
	/// </summary>
	public partial class EventGroup<T, ID> : EventGroupCore<T, ID>
			where T : Content<ID>, new()
			where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
	{


		/// <summary>
		/// Just before a CSV field is added (and made gettable).
		/// </summary>
		public EventHandler<CsvFieldMap<T>> BeforeCsvGettable;

	}
}
