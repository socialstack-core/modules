using Api.Uploader;
using System.Collections.Generic;

namespace Api.CsvImport
{
	/// <summary>
	/// Result of a CSV import operation.
	/// </summary>
	public class ImportResult
	{
		/// <summary>
		/// The name of the CSV file being imported.
		/// </summary>
		public string Filename;
        
        /// <summary>
		/// Number of records successfully created.
		/// </summary>
		public int Created;

		/// <summary>
		/// Number of records successfully updated.
		/// </summary>
		public int Updated;

		/// <summary>
		/// Number of records ignored (no change or error).
		/// </summary>
		public int Ignored;

		/// <summary>
		/// Number of records that failed to process.
		/// </summary>
		public int Errors => ErrorMessages != null ? ErrorMessages.Count : 0;

		/// <summary>
		/// Detailed error messages for failed records.
		/// </summary>
		public List<string> ErrorMessages = new();

		/// <summary>
		/// Detailed information messages for user feedback.
		/// </summary>
		public List<string> InfoMessages = new();

		/// <summary>
		/// Total number of records processed.
		/// </summary>
		public int Total;

		/// <summary>
		/// Result details (passed so that uploader UI reacts as expected)
		/// </summary>
		public Upload Result { get; set; }

	}
}
