namespace Api.Payments;


/// <summary>
/// Config for a BNPL payment method.
/// </summary>
public struct BuyNowPayLater
{
	/// <summary>
	/// Must be explicitly authorized for a given context.
	/// </summary>
	public bool Authorized;
}