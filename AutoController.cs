using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Results;
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
public partial class AutoController<T>
	where T : Api.Database.DatabaseRow, new()
{

	private CsvMapping _csvMapping;

	/// <summary>
	/// GET /v1/entityTypeName/list.csv
	/// Lists all entities of this type available to this user, and outputs as a CSV.
	/// </summary>
	/// <returns></returns>
	[HttpGet("list.csv")]
	public virtual async Task<FileResult> ListCSV()
	{
		return await ListCSV(null);
	}

	/// <summary>
	/// POST /v1/entityTypeName/list.csv
	/// Lists filtered entities available to this user.
	/// See the filter documentation for more details on what you can request here.
	/// </summary>
	/// <returns></returns>
	[HttpPost("list.csv")]
	public virtual async Task<FileResult> ListCSV([FromBody] JObject filters)
	{
		var listWithTotal = await List(filters) as ListWithTotal<T>;
		
		if(listWithTotal == null){
			return null;
		}

		if (_csvMapping == null)
		{
			_csvMapping = new CsvMapping(typeof(T));
		}

		// Not expecting huge CSV's here.
		var ms = new MemoryStream();
		var writer = new StreamWriter(ms, System.Text.Encoding.UTF8, -1, true);

		using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
		{
			foreach (var field in _csvMapping.Entries)
			{
				csv.WriteField(field.Name);
			}

			foreach (var row in listWithTotal.Results)
			{
				csv.NextRecord();

				foreach (var field in _csvMapping.Entries)
				{
					csv.WriteField(field.GetValue(row));
				}
			}
		}

		ms.Seek(0, SeekOrigin.Begin);

		return File(ms, "text/csv", typeof(T).Name + ".csv");
	}

}

/// <summary>
/// Custom CSV file mapper
/// </summary>
public class CsvMapping
{

	/// <summary>
	/// 
	/// </summary>
	/// <param name="t"></param>
	public CsvMapping(Type t)
	{
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

			Add(new CsvFieldMap() {
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

			Add(new CsvFieldMap()
			{
				Name = property.Name,
				SrcProperty = property.GetGetMethod()
			}, property.PropertyType);
		}
	}

	private void Add(CsvFieldMap map, Type type)
	{
		// If type is an advanced field, apply an AdvancedMapper to the fieldMap.
		if (type != typeof(int) && type != typeof(string) && type != typeof(DateTime) && type != typeof(bool) && type != typeof(int?))
		{
			return;
		}

		Entries.Add(map);
	}

	/// <summary>
	/// The list of fields in the mapping.
	/// </summary>
	public List<CsvFieldMap> Entries = new List<CsvFieldMap>();

}

/// <summary>
/// A particular field in a CSV map.
/// </summary>
public class CsvFieldMap {
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
	public Func<object, string> AdvancedHandler;

	/// <summary>
	/// Gets this field value for the given object.
	/// </summary>
	/// <param name="src"></param>
	/// <returns></returns>
	public string GetValue(object src)
	{
		object value;

		if (SrcField != null)
		{
			value = SrcField.GetValue(src);
		}
		else
		{
			value = SrcProperty.Invoke(src, null);
		}

		// If this is a "simple" value type, return it via toString.
		if (AdvancedHandler == null)
		{
			return value == null ? "" : value.ToString();
		}

		return AdvancedHandler(value);
	}
}