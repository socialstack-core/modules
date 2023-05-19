namespace Api.ErrorLogging;


/// <summary>
/// Validation state of a transaction.
/// </summary>
public enum ValidationState
{
	/// <summary>
	/// This transaction is valid.
	/// </summary>
	Valid = 1,
	/// <summary>
	/// This transaction is invalid.
	/// </summary>
	Invalid = 2,
	/// <summary>
	/// This transaction is currently waiting for some additional information.
	/// </summary>
	Pending = 3
}