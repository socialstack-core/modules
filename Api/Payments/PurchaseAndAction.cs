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

	/// <summary>
	/// Additional data used for purchase challenge/validation
	/// </summary>
	public ChallengeMetaData MetaData;
}

/// <summary>
/// Additional metadata required for purchase validation challenges (3ds etc)
/// </summary>
public struct ChallengeMetaData
{
	/// <summary>
	/// The challenge request (cres) value for validation via 3D Secure
	/// </summary>
	public string ChallengeRequest;

	/// <summary>
	/// The challenge url for validation via 3D Secure
	/// </summary>
	public string ChallengeUrl;

	/// <summary>
	/// The session token to allow checks on purchase code updates passed to payment provider so maybe encoded 
	/// </summary>
	public string SessionToken;

	/// <summary>
	/// The raw session token to allow checks on purchase code updates
	/// </summary>
	public string Token;

}
