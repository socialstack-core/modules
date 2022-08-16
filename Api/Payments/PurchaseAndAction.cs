namespace Api.Payments;


/// <summary>
/// Represents a purchase along with some additional state for "user present" transactions.
/// </summary>
public struct PurchaseAndAction
{
	/// <summary>
	/// The purchase.
	/// </summary>
	public Purchase Purchase;
	/// <summary>
	/// An action (usually a URL) that the user is required to go to.
	/// </summary>
	public string Action;
}