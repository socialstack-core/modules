using System;
using System.Threading.Tasks;
using CsvHelper;
using System.Reflection;

namespace Api.CsvExport;

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
	/// Field/ property value type.
	/// </summary>
	public Type TargetType;

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