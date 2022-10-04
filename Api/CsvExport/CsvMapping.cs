using Api.Contexts;
using Api.Database;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration;
using System.Reflection;
using Newtonsoft.Json;

namespace Api.CsvExport;

/// <summary>
/// Custom CSV file mapper
/// </summary>
public class CsvMapping<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
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
	private static CsvConfiguration _defaultConfig;

	/// <summary>
	/// Gets the default culture.
	/// </summary>
	public CultureInfo Culture
	{
		get
		{
			return CultureInfo.GetCultureInfo("en-GB");
		}
	}

	/// <summary>
	/// Use when not expecting a large CSV.
	/// </summary>
	/// <param name="results"></param>
	/// <param name="config"></param>
	/// <returns></returns>
	public async ValueTask<MemoryStream> OutputStream(IEnumerable<T> results, CsvConfiguration config = null)
	{
		var ms = new MemoryStream();
		var writer = new StreamWriter(ms, System.Text.Encoding.UTF8, -1, true);

		if (_defaultConfig == null)
		{
			_defaultConfig = new CsvConfiguration(Culture);
		}

		if (config == null)
		{
			config = _defaultConfig;
		}

		using (var csv = new CsvWriter(writer, config))
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
	public CsvMapping()
	{
	}

	/// <summary>
	/// Builds the CSV mapping from the given JSON structure.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="jsonStructure"></param>
	/// <param name="beforeGettable"></param>
	public async ValueTask BuildFrom(Context context, JsonStructure<T, ID> jsonStructure, Api.Eventing.EventHandler<CsvFieldMap<T>> beforeGettable)
	{
		foreach (var field in jsonStructure.ReadableFields)
		{
			CsvFieldMap<T> toAdd;

			if (field.FieldInfo != null)
			{
				var ignoredCsv = field.FieldInfo.GetCustomAttribute(typeof(CsvIgnoreAttribute));
				if (ignoredCsv != null)
				{
					continue;
				}

				toAdd = new CsvFieldMap<T>()
				{
					Name = field.Name,
					SrcField = field.FieldInfo,
					TargetType = field.TargetType
				};
			}
			else if (field.PropertyGet != null)
			{
				var ignoredCsv = field.PropertyInfo.GetCustomAttribute(typeof(CsvIgnoreAttribute));
				if (ignoredCsv != null)
				{
					continue;
				}

				toAdd = new CsvFieldMap<T>()
				{
					Name = field.Name,
					SrcProperty = field.PropertyGet,
					TargetType = field.TargetType
				};
			}
			else
			{
				// Can't add virtual fields as they are objects and don't function in a CSV
				continue;
			}

			// Allow any final CSV field filtering:
			toAdd = await beforeGettable.Dispatch(context, toAdd);

			if (toAdd == null)
			{
				continue;
			}

			Add(toAdd, toAdd.TargetType);
		}
	}

	private void BuildFromType()
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

			var ignoredCsv = field.GetCustomAttribute(typeof(CsvIgnoreAttribute));
			if (ignoredCsv != null)
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

			var ignoredCsv = property.GetCustomAttribute(typeof(CsvIgnoreAttribute));
			if (ignoredCsv != null)
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