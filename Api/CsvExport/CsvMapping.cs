using Api.Contexts;
using Api.Database;
using Api.Startup;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Api.SocketServerLibrary;

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
	/// Use when not expecting a large CSV.
	/// </summary>
	/// <param name="results"></param>
	/// <returns></returns>
	public async ValueTask<MemoryStream> OutputStream(IEnumerable<T> results)
	{
		var ms = new MemoryStream();
		await OutputStream(results, ms);
		return ms;
	}

	/// <summary>
	/// Outputs a streaming CSV in to the given target stream.
	/// </summary>
	/// <param name="results"></param>
	/// <param name="targetStream"></param>
	/// <returns></returns>
	public async ValueTask OutputStream(IEnumerable<T> results, Stream targetStream)
	{
		var writer = Writer.GetPooled();
		writer.Start(null);

		var first = true;

		foreach (var field in Entries)
		{
			if (first)
			{
				first = false;
			}
			else
			{
				writer.Write((byte)',');
			}
			writer.WriteASCII(field.Name);
		}
		
		foreach (var row in results)
		{
			await writer.CopyToAsync(targetStream);
			writer.Reset(null);
			writer.WriteASCII("\r\n");
			first = true;

			foreach (var field in Entries)
			{
				var val = field.GetValue(row);

				if (first)
				{
					first = false;
				}
				else
				{
					writer.Write((byte)',');
				}

				if (val != null)
				{
					if (field.TargetType == typeof(string))
					{
						// strings are escaped
						writer.WriteEscaped((string)val);
					}
					else
					{
						// Numbers, bools etc.
						writer.WriteS(val.ToString());
					}
				}
			}
		}

		await writer.CopyToAsync(targetStream);
		writer.Release();
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
		foreach (var entry in jsonStructure.Fields)
		{
			var field = entry.Value;
			
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