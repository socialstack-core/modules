using Api.Contexts;
using Api.CsvImport;
using Api.Permissions;
using Api.Startup;
using Api.Uploader;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

/// <summary>
/// A convenience controller for defining common endpoints like create, list, delete etc. Requires an AutoService of the same type to function.
/// Not required to use these - you can also just directly use ControllerBase if you want.
/// Like AutoService this isn't in a namespace due to the frequency it's used.
/// </summary>
public partial class AutoController<T, ID>
{
	/// <summary>
	/// PUT /v1/entityTypeName/csv/import
	/// Imports entities from a CSV file provided in the request body. 
	/// The Content-Name header should be set to the desired file name (including .csv extension) for validation and debugging purposes.
	/// 
	/// fieldMappings is a comma-separated list of CsvColumn:EntityField pairs to map CSV columns to entity fields.
	/// 
	/// keyFields are used to determine if a CSV row corresponds to an existing record (based on matching values in those fields).
	/// 
	/// mode=create (default) - Only creates new records. If a record with matching key fields exists, it will be skipped.
	/// mode=update - Only updates existing records. If a record with matching key fields does not exist, it will be skipped.
	/// mode=createOrUpdate - Creates new records and updates existing records based on key field matches
	/// 
	/// convertFromUTC - Forcefully convert basic dates and times into UTC 
	/// 
	/// </summary>
	/// <returns></returns>

	[HttpPut("csv/import")]
	public async ValueTask<ImportResult> ImportFromCsv(
		HttpContext httpContext,
		Context ctx,
		[FromQuery] string fieldMappings = null,
		[FromQuery] string keyFields = null,
		[FromQuery] string mode = null,
		[FromQuery] string key = null,
		[FromQuery] bool convertToUTC = false
		)
	{
		var request = httpContext.Request;

        const long maxBytes = (1 * 1024 * 1024); // 1 MB
        var contentLength = httpContext.Request.ContentLength;
        if (contentLength == null || contentLength > maxBytes)
        {
            throw new PublicException("Incoming file is too large (1MB limit)", "csv_too_large");
        }

		if (ctx.Role == null || !ctx.Role.CanViewAdmin)
		{
			throw PermissionException.Create("csv/import", ctx);
		}

		if (!request.Headers.TryGetValue("Content-Name", out StringValues name))
		{
			throw new PublicException("Content-Name header is required", "no_name");
		}

		var fileName = HttpUtility.UrlDecode(name.ToString());

		if (!IsValidCsvImportFileName(fileName))
		{
			throw new PublicException("Content-Name header should be a CSV filename", "invalid_name");
		}

		using var memoryStream = new MemoryStream();
		await request.Body.CopyToAsync(memoryStream);
		memoryStream.Position = 0;

		var config = new CsvImportConfig()
		{
			Key = key,
			FieldMappings = ParseFieldMappings(fieldMappings),
			KeyFields = ParseKeyFields(keyFields),
			Mode = Enum.TryParse<ImportMode>(mode, ignoreCase: true, out var parsedMode) ? parsedMode : ImportMode.Create,
			ConvertToUtc = convertToUTC
		};

		var importResult = await _service.CsvImportForType(ctx, memoryStream, config);

		if (importResult != null)
		{
			importResult.Filename = fileName;
		}

		return importResult;
	}

	/// <summary>
	/// Parses a shorthand field mapping string into a list of FieldMappings.
	/// Format: "CsvCol:EntityField,CsvCol2:EntityField2"
	/// If null or empty, returns an empty list.
	/// </summary>
	private static List<FieldMapping> ParseFieldMappings(string? value)
	{
		var result = new List<FieldMapping>();
		if (string.IsNullOrWhiteSpace(value))
		{
			return result;
		}

		foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var colon = part.IndexOf(':');
			if (colon <= 0)
			{
				continue;
			}
			result.Add(new FieldMapping
			{
				CsvColumn = part[..colon].Trim(),
				EntityField = part[(colon + 1)..].Trim()
			});
		}
		return result;
	}

	/// <summary>
	/// Parses a comma-separated list of key field names.
	/// Format: "Id,Reference"
	/// If null or empty, returns an empty list.
	/// </summary>
	private static List<string> ParseKeyFields(string value)
	{
		var result = new List<string>();
		if (string.IsNullOrWhiteSpace(value))
		{
			return result;
		}

		foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			result.Add(part);
		}
		return result;
	}

	// Regex pattern: 
	// ^                   - Start of the string
	// [ a-zA-Z0-9_-]+      - One or more allowed characters (letters, numbers, hyphen, underscore)
	// (\.csv)             - Exactly one dot followed by 'csv'
	// $                   - End of the string
	// RegexOptions.IgnoreCase - Makes the check case-insensitive (e.g., works for .CSV)
	private static readonly Regex CsvImportFilenameRegex = new Regex(
		@"^[ a-zA-Z0-9_-]+(\.csv)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled
	);

	/// <summary>
	/// Validates if a string is a safe and correctly formatted CSV file name 
	/// using a whitelist Regular Expression.
	/// </summary>
	/// <param name="fileName">The file name provided by the user.</param>
	/// <returns>True if the name is valid; otherwise, false.</returns>
	private static bool IsValidCsvImportFileName(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return false;
		}

		// We use Trim() here to allow users to accidentally include leading/trailing spaces
		// but the core Regex must match the content *after* trimming.
		return CsvImportFilenameRegex.IsMatch(fileName.Trim());
	}
}

public partial class AutoService<T, ID>
{

	/// <summary>
	/// Generic method that handles the csv import for a specific entity type.
	/// </summary>
	public async ValueTask<ImportResult> CsvImportForType(Context context, Stream stream, CsvImportConfig config)
	{
		var result = new ImportResult()
		{
			Result = new Upload()
		};

		var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = true,
			MissingFieldFound = null,
			BadDataFound = null,
			HeaderValidated = null,
			PrepareHeaderForMatch = args =>
				(args.Header ?? string.Empty)
				.Replace(" ", "")
				.Trim()
				.ToLowerInvariant()
		};

		using var reader = new StreamReader(stream);
		using var csv = new CsvReader(reader, csvConfig);

		// Read header if present
		if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord == null || csv.HeaderRecord.Length == 0)
		{
			result.ErrorMessages.Add("No header row was found in the csv file");
			return result;
		}

		if (EventGroup.CustomCsvImport.HasListeners())
		{
			result = await EventGroup.CustomCsvImport.Dispatch(context, result, config, csv);
			return result;
		}

		// Get the JSON structure for this entity type
		var jsonStructure = await GetTypedJsonStructure(context);

		// Build a lookup from entity field name to JsonField (case insensitive field lookup)
		var fieldLookup = new Dictionary<string, JsonField<T, ID>>(StringComparer.OrdinalIgnoreCase);
		foreach (var kvp in jsonStructure.Fields)
		{
			fieldLookup[kvp.Key] = kvp.Value;
		}

		// Build a lookup from CSV column to entity field mapping.

		// Default: each entity field maps to a CSV column of the same name (case in-sentitive).
		var csvFieldMappings = new Dictionary<string, JsonField<T, ID>>(StringComparer.OrdinalIgnoreCase);

		// Reverse lookup: JsonField -> CSV column, used to avoid scans when overriding mappings.
		var reverseMappings = new Dictionary<JsonField<T, ID>, string>();

		// Key fields and names, used to locate existing records, by email for example
		var keyFields = new List<(string FieldName, JsonField<T, ID> JsonField)>();
		var keyFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var kvp in fieldLookup)
		{
			var csvColumn = CheckCsvColumn(kvp.Key, csv);
			if (csvColumn == null)
			{
				// matching field not found in csv header so can be ignored
				continue;
			}

			// is it flagged as a key field ?
			if (kvp.Value.FieldInfo != null) {
				var csvKeyField = kvp.Value.FieldInfo.GetCustomAttribute(typeof(CsvKeyAttribute));
				if (csvKeyField != null && !keyFieldNames.Contains(kvp.Key))
				{
					keyFields.Add((FieldName: kvp.Key, JsonField: kvp.Value));
					keyFieldNames.Add(kvp.Key);
				}
			}

			csvFieldMappings[csvColumn] = kvp.Value;
			reverseMappings[kvp.Value] = csvColumn;
		}

		// Explicit FieldMappings override or extend the defaults.
		foreach (var mapping in config.FieldMappings)
		{
			if (string.IsNullOrEmpty(mapping.CsvColumn) || string.IsNullOrEmpty(mapping.EntityField))
			{
				continue;
			}

			var csvColumn = CheckCsvColumn(mapping.CsvColumn, csv);
			if (csvColumn == null)
			{
				result.ErrorMessages.Add($"Could not find a mapping column name in the provided data :: {mapping.CsvColumn}");
				return result;
			}

			if (fieldLookup.TryGetValue(mapping.EntityField, out var jsonField))
			{
				// Remove any existing mapping for this entity field (identity or prior override)
				if (reverseMappings.TryGetValue(jsonField, out var existingCsvColumn))
				{
					csvFieldMappings.Remove(existingCsvColumn);
				}
				csvFieldMappings[csvColumn] = jsonField;
				reverseMappings[jsonField] = csvColumn;

				// is it flagged as a key field ?
				if (jsonField.FieldInfo != null)
				{
					var csvKeyField = jsonField.FieldInfo.GetCustomAttribute(typeof(CsvKeyAttribute));
					if (csvKeyField != null && !keyFieldNames.Contains(mapping.EntityField))
					{
						keyFields.Add((FieldName: mapping.EntityField, JsonField: jsonField));
						keyFieldNames.Add(mapping.EntityField);
					}
				}
			}
		}

		// intial mapping all done
		// may re use other fields via custom events
		// so not an issue if there are some fields in teh csv file which are not used
		if (csvFieldMappings.Count == 0)
		{
			result.ErrorMessages.Add($"Unable to map column names from csv columns :: {string.Join(",", csv.HeaderRecord)}");
			return result;
		}

		// Add config defined key field lookups
		if (config.KeyFields != null && config.KeyFields.Count > 0)
		{
			foreach (var keyFieldName in config.KeyFields)
			{
				if (!keyFieldNames.Contains(keyFieldName))
				{
					// find the field 
					if (fieldLookup.TryGetValue(keyFieldName, out var jsonField))
					{
						// ensure we have a mapping from the data
						// will take into account mapped fields such as "email address" -> "email"
						if (reverseMappings.TryGetValue(jsonField, out var existingCsvColumn))
						{
							keyFields.Add((FieldName: keyFieldName, JsonField: jsonField));
							keyFieldNames.Add(keyFieldName);
						}
						else
						{
							result.ErrorMessages.Add($"Could not find a key column name in the provided data :: {keyFieldName}");
							return result;
						}
					}
					else
					{
						result.ErrorMessages.Add($"Could not find the field details for key field :: {keyFieldName}");
						return result;
					}
				}
			}
		}

		// if in create mode and have no keys fields items will always be created
		// if necessary the BeforeCsvImportCreate handler can be used for complex logic
		if (keyFields.Count == 0 && config.Mode != ImportMode.Create)
		{
			result.ErrorMessages.Add($"Unable to map key names from csv columns :: {string.Join(",", config.KeyFields)}");
			return result;
		}

		while (await csv.ReadAsync())
		{
			result.Total++;

			try
			{
				var entity = new T();
				var keyValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				
                // Pre-convert all field values; used by both create and update paths.
				var convertedValues = new Dictionary<string, (JsonField<T, ID> Field, object Value)>(StringComparer.OrdinalIgnoreCase);
				var fieldsWithValue = 0;

				// Populate fields from CSV
				foreach (var (csvColumn, jsonField) in csvFieldMappings)
				{
					var csvValue = csv.GetField(csvColumn);
					if (string.IsNullOrWhiteSpace(csvValue))
					{
						continue;
					}

					try
					{
						var value = ConvertValue(config, csvValue, jsonField);
                        if (config.Mode != ImportMode.Create) {
						    convertedValues[csvColumn] = (jsonField, value);
                        }
						jsonField.FieldInfo.SetValue(entity, value);

						// Track key field values for lookup
						if (keyFieldNames.Contains(jsonField.Name))
						{
							keyValues[jsonField.Name] = value;
						}

						fieldsWithValue++;
					}
					catch (Exception ex)
					{
						result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Failed to set field '{jsonField.Name}'::'{csvValue}'::{ex.Message}");
					}
				}

				if (fieldsWithValue == 0)
				{
					result.Ignored++;
					result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Entry had no values to populate, ignoring");
					continue;
				}

				// Check for existing record if key fields are defined
				var (existingCount, existing) = await FindExistingEntity(context, keyFields, keyValues);

				if (existing != null)
				{
					if (config.Mode == ImportMode.Create)
					{
						result.Ignored++;
						result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Existing entry found as id {existing.Id} (Create only mode)");
						continue;
					}

					if (existingCount != 1)
					{
						result.Ignored++;
						result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Multiple matching entries :: {existingCount} (Create only mode)");
						continue;
					}

					// Update existing entity
					var updated = await Update(context, existing, async (ctx, toUpdate, orig) =>
					{
						foreach (var (key, (jsonField, value)) in convertedValues)
						{
							jsonField.FieldInfo.SetValue(toUpdate, value);
						}

						// handle any more complex logic for customisations etc 
						toUpdate = await EventGroup.BeforeCsvImportUpdate.Dispatch(context, toUpdate, result, config, csv);

					}, DataOptions.IgnorePermissions);

					if (updated != null)
					{
						result.Updated++;
					}
					else
					{
						result.Ignored++;
						result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Failed to update existing id {existing.Id}");
					}
				}
				else
				{
					if (config.Mode == ImportMode.Update)
					{
						result.Ignored++;
						result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Existing entry could not be found (Update Only Mode)");
						continue;
					}

					// handle any more complex logic for customisations etc 
					entity = await EventGroup.BeforeCsvImportCreate.Dispatch(context, entity, result, config, csv);

					if (entity != null)
					{
						// Create new entity
						var created = await Create(context, entity, DataOptions.IgnorePermissions);
						if (created != null)
						{
							result.Created++;
						}
						else
						{
							result.Ignored++;
							result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Failed to create new entry");
						}
					} 
					else
					{
						// processed by event handler 
						result.Created++;
					}
				}
			}
			catch (Exception ex)
			{
				result.Ignored++;
				result.ErrorMessages.Add($"Row {csv.Parser.Row} :: Failed to process :: {ex.Message}");
			}
		}

		return result;
	}

	/// <summary>
	/// Ensure we have a matching column name in the file and return it for mapping
	/// </summary>
	/// <param name="key"></param>
	/// <param name="csv"></param>
	/// <returns></returns>
	private string CheckCsvColumn(string key, CsvReader csv)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}

		var normalizedKey = NormalizeCsvHeader(key);
		var index = Array.FindIndex(csv.HeaderRecord, header =>	NormalizeCsvHeader(header).Equals(normalizedKey, StringComparison.Ordinal));
		if (index == -1)
		{
			return null;
		}

		return csv.HeaderRecord[index];
	}

	/// <summary>
	/// Normalise column names for matching purposes such as "Last Name" == "lastname"
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	private static string NormalizeCsvHeader(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		return value.Replace(" ", string.Empty).Trim().ToLowerInvariant();
	}


	/// <summary>
	/// Finds an existing entity by matching key field values.
	/// </summary>
	private async ValueTask<(int, T)> FindExistingEntity(Context context,
		List<(string FieldName, JsonField<T, ID> JsonField)> keyFields,
		Dictionary<string, object> keyValues)
	{
		// no keyfields are defined so pass back as not found
		if (keyFields == null || keyFields.Count == 0 || keyValues == null || keyValues.Count == 0)
		{
			return (-1,default);
		}

		// Build query with all key fields
		var queryParts = new List<string>();
		var bindValues = new List<object>();

		foreach (var key in keyFields)
		{
			if (keyValues.TryGetValue(key.JsonField.Name, out var value))
			{
				queryParts.Add($"{key.JsonField.Name}=?");
				bindValues.Add(value);
			}
		}

		// do we have a value for each key 
		if (queryParts.Count != keyFields.Count)
		{
			return (-2, default);
		}

		var query = string.Join(" AND ", queryParts);
		var filter = Where(query, DataOptions.IgnorePermissions);

		// Bind all values
		foreach (var val in bindValues)
		{
			filter.Bind(val);
		}

		var existing = await filter.ListAll(context);

		if (existing == null || existing.Count == 0)
		{
			return (0, default);
		}

		return (existing.Count, existing[0]);
	}

	/// <summary>
	/// Converts a string value to the target type.
	/// </summary>
	private object ConvertValue(CsvImportConfig config, string value, JsonField<T, ID> jsonField)
	{
		var targetType = jsonField.TargetType;

		if (targetType == typeof(string))
		{
			return value;
		}
		else if (targetType == typeof(uint) || targetType == typeof(uint?))
		{
			return uint.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(int) || targetType == typeof(int?))
		{
			return int.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(long) || targetType == typeof(long?))
		{
			return long.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(ulong) || targetType == typeof(ulong?))
		{
			return ulong.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(float) || targetType == typeof(float?))
		{
			return float.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(double) || targetType == typeof(double?))
		{
			return double.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
		{
			return decimal.Parse(value, CultureInfo.InvariantCulture);
		}
		else if (targetType == typeof(bool) || targetType == typeof(bool?))
		{
			return bool.Parse(value);
		}
		else if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
		{
			if (config.ConvertToUtc && jsonField.Name.Contains("UTC", StringComparison.OrdinalIgnoreCase))
			{
				// Value is local time — parse and convert to UTC
				return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal);
			}
			// Value is local time — keep as local
			return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
		}
		else
		{
			return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
		}
	}
}

namespace Api.Eventing
{
	public partial class EventGroup<T, ID>
	{
		#region Service events

		/// <summary>
		/// Called just before a entity is updated with data as part of a csv import
		/// Allows for custom logic to be used with the raw csv entry 
		/// </summary>
		public EventHandler<T, ImportResult, CsvImportConfig, CsvReader> BeforeCsvImportCreate;

		/// <summary>
		/// Called just before a new entity is created with data as part of a csv import
		/// Allows for custom logic to be used with the raw csv entry 
		/// </summary>
		public EventHandler<T, ImportResult, CsvImportConfig, CsvReader> BeforeCsvImportUpdate;

		/// <summary>
		/// For special cases call a custom event to perform any updates
		/// </summary>
		/// <returns></returns>
		public EventHandler<ImportResult, CsvImportConfig, CsvReader> CustomCsvImport;


		#endregion
	}
}
