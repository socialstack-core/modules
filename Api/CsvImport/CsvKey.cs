using System;

namespace Api.CsvImport;

/// <summary>
/// Marks fields as keys in a CSV. A simpler alternative to the mapping configuration. 
/// This is used by the CsvImportService to determine which fields to use as keys when importing data from a CSV file.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public partial class CsvKeyAttribute : Attribute
{
	
}