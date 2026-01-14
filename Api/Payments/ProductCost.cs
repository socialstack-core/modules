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
	/// The amount inclusive of any tax, if tax is active. The currency is that of the context that requested the amount.
	/// Note that line item tax can appear inaccurate because it is applied to the overall total actually.
	/// </summary>
	public ulong Amount;

	/// <summary>
	/// The currency code (the same as the one in the locale).
	/// </summary>
	public string CurrencyCode;

	/// <summary>
	/// The amount minus any tax.
	/// </summary>
	public ulong AmountLessTax;

	/// <summary>
	/// True if there are subscription products in this cost.
	/// </summary>
	public bool SubscriptionProducts;

	/// <summary>
	/// An error if there is one. These are soft errors as they often appear on e.g. 
	/// the checkout UI without preventing the rest of the response from appearing.
	/// </summary>
	public string ErrorMessage;

	/// <summary>
	/// An error if there is one. These are soft errors as they often appear on e.g. 
	/// the checkout UI without preventing the rest of the response from appearing.
	/// </summary>
	public string ErrorCode;
}