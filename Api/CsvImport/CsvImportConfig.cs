using System.Collections.Generic;

namespace Api.CsvImport
{
	/// <summary>
	/// Configuration for a CSV import, bindable directly from a query string via [FromQuery].
	/// </summary>
	public class CsvImportConfig
	{
		/// <summary>
		/// Unique identifier for this import
		/// </summary>
		public string Key;

		/// <summary>
		/// Field mappings from CSV column names to entity field names.
		/// </summary>
		public List<FieldMapping> FieldMappings = new();

		/// <summary>
		/// Fields used to match existing records (e.g. ["Reference"]).
		/// If empty, all records will be created as new.
		/// </summary>
		public List<string> KeyFields = new();

		/// <summary>
		/// Import mode - controls create/update behavior.
		/// </summary>
		public ImportMode Mode = ImportMode.Create;

		/// <summary>
		/// Convert any times to be UTC, assuming they are local 
		/// e.g.
		/// 2026-04-02 09:48:41 during BST would become 2026-04-02T08:48:41.000+00:00
		/// </summary>
		public bool ConvertToUtc = false;
	}

	/// <summary>
	/// Maps a CSV column to an entity field.
	/// </summary>
	public class FieldMapping
	{
		/// <summary>
		/// The column name in the CSV file.
		/// </summary>
		public string CsvColumn;

		/// <summary>
		/// The field name on the entity.
		/// </summary>
		public string EntityField;
	}

	/// <summary>
	/// Import mode options.
	/// </summary>
	public enum ImportMode
	{
		/// <summary>
		/// Create new records, skip existing.
		/// </summary>
		Create,

		/// <summary>
		/// Update existing records, skip new.
		/// </summary>
		Update,

		/// <summary>
		/// Create new or update existing (default).
		/// </summary>
		CreateOrUpdate
	}
}
