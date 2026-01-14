namespace Api.Payments;

/// <summary>
/// Pricing information for delivery, used to indicate which apportion of a delivery is taxed.
/// </summary>
public struct DeliveryPricingInfo
{
	/// <summary>
	/// The 0-1 delivery apportion.
	/// </summary>
	public double TaxApportion;
	/// <summary>
	/// The total value (in atomic units of currency i.e. pence/ cents) that was not taxed because it is zero rated.
	/// </summary>
	public ulong ValueUntaxed;
	/// <summary>
	/// The total value (in atomic units of currency i.e. pence/ cents) that was taxed because it is not zero rated.
	/// </summary>
	public ulong ValueTaxed;
}