namespace Api.Payments;


/// <summary>
/// The state of a purchase.
/// </summary>
public struct PurchaseStatus
{
	/// <summary>
	/// The state of the purchase.
	/// </summary>
	public uint Status;
	
	/// <summary>
	/// A next action redirect URL if one is required.
	/// This is usually displayed in a modal.
	/// </summary>
	public string NextAction;
}