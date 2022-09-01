namespace Api.Payments;


/// <summary>
/// The cost of a product.
/// </summary>
public struct ProductCost
{
	/// <summary>
	/// Representitive of no cost at all.
	/// </summary>
	public static readonly ProductCost None = new ProductCost();

	/// <summary>
	/// The currency code to use.
	/// </summary>
	public string CurrencyCode;
	
	/// <summary>
	/// The amount.
	/// </summary>
	public ulong Amount;

	/// <summary>
	/// True if there are subscription products in this cost.
	/// </summary>
	public bool SubscriptionProducts;
}