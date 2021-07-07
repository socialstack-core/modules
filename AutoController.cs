using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration;
using System.Reflection;
using Newtonsoft.Json;


/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class AutoController<T,ID>
{

	private CsvMapping<T> _csvMapping;

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
		var results = await _service.Where().ListAll(context);

		var listWithTotal = new ListWithTotal<T>()
		{
			Results = results
		};

		if (listWithTotal == null){
			Response.StatusCode = 400;
			return null;
		}

		if (_csvMapping == null)
		{
			_csvMapping = new CsvMapping<T>();
		}

		// Not expecting huge CSV's here.
		var ms = await _csvMapping.OutputStream(results);
		ms.Seek(0, SeekOrigin.Begin);
		return File(ms, "text/csv", typeof(T).Name + ".csv");
	}

}

/// <summary>
/// Custom CSV file mapper
/// </summary>
public class CsvMapping<T>
{
	/// <summary>
	/// Creates a mapping with specific fields
	/// </summary>
	/// <param name="customFields"></param>
	public CsvMapping(string[] customFields)
	{
		var t = typeof(T);

		foreach(var fieldName in customFields)
		{
			var field = t.GetField(fieldName);

			if (field != null)
			{
				Add(new CsvFieldMap<T>()
				{
					Name = field.Name,
					SrcField = field
				}, field.FieldType);
				continue;
			}

			var property = t.GetProperty(fieldName);

			if (property == null)
			{
				continue;
			}

			Add(new CsvFieldMap<T>()
			{
				Name = property.Name,
				SrcProperty = property.GetGetMethod()
			}, property.PropertyType);
		}
	}

	/// <summary>
	/// The GB culture, primarily for date formatting in DD/MM/YYYY
	/// </summary>
	private static CultureInfo _culture;

	/// <summary>
	/// Use when not expecting a large CSV.
	/// </summary>
	/// <param name="results"></param>
	/// <returns></returns>
	public async ValueTask<MemoryStream> OutputStream(IEnumerable<T> results)
	{
		var ms = new MemoryStream();
		var writer = new StreamWriter(ms, System.Text.Encoding.UTF8, -1, true);

		if (_culture == null)
		{
			_culture = CultureInfo.GetCultureInfo("en-GB");
		}

		using (var csv = new CsvWriter(writer, _culture))
		{
			foreach (var field in Entries)
			{
				csv.WriteField(field.Name);
			}

			foreach (var row in results)
			{
				csv.NextRecord();

				foreach (var field in Entries)
				{
					await field.WriteValue(row, csv);
				}
			}
		}

		return ms;
	}

	/// <summary>
	/// Adds a dynamic field to the CSV set
	/// </summary>
	/// <param name="name"></param>
	/// <param name="onWrite"></param>
	public void Add(string name, Func<T, CsvWriter, ValueTask> onWrite)
	{
		Entries.Add(new CsvFieldMap<T>()
		{
			Name = name,
			AdvancedHandler = onWrite
		});
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="t"></param>
	public CsvMapping()
	{
		var t = typeof(T);

		// Get all fields (DB fields) - we'll omit them if they're JsonIgnore'd:
		var fields = t.GetFields();

		// And all public properties too:
		var props = t.GetProperties();

		foreach (var field in fields)
		{
			var ignored = field.GetCustomAttribute(typeof(JsonIgnoreAttribute));
			if (ignored != null)
			{
				continue;
			}

			Add(new CsvFieldMap<T>() {
				Name = field.Name,
				SrcField = field
			}, field.FieldType);
		}

		foreach (var property in props)
		{
			var ignored = property.GetCustomAttribute(typeof(JsonIgnoreAttribute));
			if (ignored != null)
			{
				continue;
			}

			Add(new CsvFieldMap<T>()
			{
				Name = property.Name,
				SrcProperty = property.GetGetMethod()
			}, property.PropertyType);
		}
	}

	private void Add(CsvFieldMap<T> map, Type type)
	{
		// If type is an advanced field, apply an AdvancedMapper to the fieldMap.
		var baseType = Nullable.GetUnderlyingType(type);

		if (baseType == null)
		{
			baseType = type;
		}

		if (!baseType.IsPrimitive && baseType != typeof(string) && baseType != typeof(DateTime))
		{
			return;
		}

		Entries.Add(map);
	}

	/// <summary>
	/// The list of fields in the mapping.
	/// </summary>
	public List<CsvFieldMap<T>> Entries = new List<CsvFieldMap<T>>();

}

/// <summary>
/// A particular field in a CSV map.
/// </summary>
public class CsvFieldMap<T>
{
	/// <summary>
	/// Field name, as it will appear in the CSV.
	/// </summary>
	public string Name;
	/// <summary>
	/// If a fieldInfo, the raw one it came from.
	/// </summary>
	public FieldInfo SrcField;
	/// <summary>
	/// If a property, the get method.
	/// </summary>
	public MethodInfo SrcProperty;

	/// <summary>
	/// Maps an 'advanced' object. E.g. a list of interests -> comma separated ID's.
	/// </summary>
	public Func<T, CsvWriter, ValueTask> AdvancedHandler;

	/// <summary>
	/// Gets this field value for the given object.
	/// </summary>
	/// <param name="src"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public async ValueTask WriteValue(T src, CsvWriter writer)
	{
		if (AdvancedHandler != null)
		{
			await AdvancedHandler(src, writer);
			return;
		}

		object value;

		if (SrcField != null)
		{
			value = SrcField.GetValue(src);
		}
		else
		{
			value = SrcProperty.Invoke(src, null);
		}

		writer.WriteField(value);
	}
}