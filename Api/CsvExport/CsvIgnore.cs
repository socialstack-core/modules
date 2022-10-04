using System;

namespace Api.CsvExport;

/// <summary>
/// Ignore fields from a CSV. A simpler alternative to the BeforeCsvGettable event.
/// Note that CSV fields are also affected by the presence of JsonIgnore and BeforeGettable.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public partial class CsvIgnoreAttribute : Attribute
{
	
}