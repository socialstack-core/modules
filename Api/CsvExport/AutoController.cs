using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.IO;
using Api.CsvExport;


/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class AutoController<T,ID>
{
	/// <summary>
	/// GET /v1/entityTypeName/list.csv
	/// Lists all entities of this type available to this user, and outputs as a CSV.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list.csv")]
	public virtual async Task<FileResult> ListCSV([FromQuery] string includes = null)
	{
		return await ListCSV(null, includes);
	}

	/// <summary>
	/// POST /v1/entityTypeName/list.csv
	/// Lists filtered entities available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("list.csv")]
	public virtual async Task<FileResult> ListCSV([FromBody] JObject filters, [FromQuery] string includes = null)
	{
		var context = await Request.GetContext();

		var filter = _service.LoadFilter(filters) as Filter<T, ID>;
		filter = await _service.EventGroup.EndpointStartList.Dispatch(context, filter, Response);

		if (filter == null)
		{
			// A handler rejected this request.
			Response.StatusCode = 404;
			return null;
		}

		var results = await filter.ListAll(context);

		var csvMapping = await _service.GetCsvMapping(context);

		// Not expecting huge CSV's here.
		var ms = await csvMapping.OutputStream(results);
		ms.Seek(0, SeekOrigin.Begin);
		return File(ms, "text/csv", typeof(T).Name + ".csv");
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
