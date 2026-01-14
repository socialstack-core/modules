using Newtonsoft.Json;
using System.Collections.Generic;

namespace Api.Payments;


/// <summary>
/// Contents of a shopping cart.
/// </summary>
public partial class ProductQuantityPricing
{
	/// <summary>
	/// Total incl. any relevant taxes and any coupons applied.
	/// </summary>
	public ulong Total;
	
	/// <summary>
	/// Total without any tax (VAT).
	/// </summary>
	public ulong TotalLessTax;
	
	/// <summary>
	/// Total incl. any relevant taxes but excluding any coupons (PreDiscount)
	/// </summary>
	public ulong TotalPD;

	/// <summary>
	/// Total without any tax (VAT) but excluding any coupons (PreDiscount)
	/// </summary>
	public ulong TotalPDLessTax;
	
	/// <summary>
	/// The active tax jurisdiction.
	/// </summary>
	public string TaxJurisdiction;

	/// <summary>
	/// Coupon used.
	/// </summary>
	public uint CouponId;

	/// <summary>
	/// The active currency.
	/// </summary>
	public string CurrencyCode;
	
	/// <summary>
	/// The contents of the cart.
	/// </summary>
	public List<LineItem> Contents;

	/// <summary>
	/// An error message if there is something wrong with this set as a whole (typically coupon related).
	/// </summary>
	public string ErrorMessage;

	/// <summary>
	/// Error code if there is something wrong with this set as a whole (typically coupon related). Check this first.
	/// If one is present, checking out will be prevented.
	/// </summary>
	public string ErrorCode;

	/// <summary>
	/// True if any of the contents are subscription based.
	/// </summary>
	public bool HasSubscriptionProducts;
}

/// <summary>
/// A particular cart item.
/// </summary>
public struct LineItem
{
	/// <summary>
	/// The original PQ.
	/// </summary>
	[JsonIgnore]
	public ProductQuantity ProductQuantity;

	/// <summary>
	/// The PQ Id.
	/// </summary>
	public uint ProductQuantityId;

	/// <summary>
	/// The original product.
	/// </summary>
	[JsonIgnore]
	public Product Product;

	/// <summary>
	/// An error message if there is something wrong with this line item.
	/// </summary>
	public string ErrorMessage;

	/// <summary>
	/// Error code if there is something wrong with this line item. Check this first.
	/// </summary>
	public string ErrorCode;

	/// <summary>
	/// The product that this is a quantity of. The product may permit unlimited usage in which case units does not need to be used.
	/// </summary>
	public uint ProductId;
	
	/// <summary>
	/// The quantity.
	/// </summary>
	public ulong Quantity;
	
	/// <summary>
	/// TotalLessTax then sent through the Tax calc, if there is one.
	/// </summary>
	public ulong Total;
	
	/// <summary>
	/// Total without any tax (VAT).
	/// </summary>
	public ulong TotalLessTax;

	/// <summary>
	/// Creates a new empty line item
	/// </summary>
	public LineItem() { }

	/// <summary>
	/// Creates this item as a copy of the given one, but optionall replaces the PQ with the given one.
	/// </summary>
	public LineItem(LineItem toCopy, ProductQuantity productQuantity = null)
	{
		if (productQuantity != null)
		{
			ProductQuantity = productQuantity;
			ProductQuantityId = productQuantity.Id;
		}
		else
		{
			ProductQuantity = toCopy.ProductQuantity;
			ProductQuantityId = toCopy.ProductQuantityId;
		}

		Product = toCopy.Product;
		ErrorMessage = toCopy.ErrorMessage;
		ErrorCode = toCopy.ErrorCode;
		ProductId = toCopy.ProductId;
		Quantity = toCopy.Quantity;
		Total = toCopy.Total;
		TotalLessTax = toCopy.TotalLessTax;
	}
}