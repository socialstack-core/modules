using Api.Startup;
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

	/// <summary>
	/// Returns the delivery pricing info for the collection. Returns null if this doesn't require delivery
	/// </summary>
	/// <returns></returns>
	/// <exception cref="PublicException"></exception>
	public DeliveryPricingInfo? DeliveryPricingInfo
	{
		get{
			if(Contents == null)
			{
				return null;
			}
			
			bool requiresDelivery = false;

			// It's common (such as in the UK) to use a value rating
			// mechanism for mixed tax status physical deliveries.
			// It's based on value because it aligns with the fundamental principles of VAT, however,
			// that adds a suprising amount of complexity to how it actually is calculated on delivery fees.
			// For example, the tax code has special cases for free samples and so on.
			// A 100% discount is not a free sample so they work differently too!
			// 
			// This code operates on the price *before* discounts are applied as it treats coupons as global only
			// thus somewhat cancelling out the discount case (for now, audit required).
			ulong valueTaxed = 0;
			ulong valueUntaxed = 0;

			foreach (var lineItem in Contents)
			{
				var product = lineItem.Product;

				if (product == null || product.ProductType != 0)
				{
					continue;
				}

				requiresDelivery = true;
				var tax = lineItem.Total - lineItem.TotalLessTax;

				if (tax > 0)
				{
					// This item is taxed.
					valueTaxed += lineItem.TotalLessTax;
				}
				else
				{
					// Zero rated. The delivery is zero rated for these too.
					valueUntaxed += lineItem.TotalLessTax;
				}

				if (lineItem.TotalLessTax == 0)
				{
					// Free sample special case. Must use their nominal value.
					if (!product.FreeSampleNominalValue.HasValue || product.FreeSampleNominalValue.Value <= 0)
					{
						throw new PublicException("A free sample in this order can't be checked out due to missing tax information", "product/nominal_missing");
					}

					if (product.TaxExempt == 1)
					{
						// Zero rated.
						valueUntaxed += product.FreeSampleNominalValue.Value;
					}
					else
					{
						valueTaxed += product.FreeSampleNominalValue.Value;
					}
				}
			}

			if (!requiresDelivery)
			{
				// Digital delivery only.
				return null;
			}

			var totalValue = valueTaxed + valueUntaxed;

			if (totalValue <= 0)
			{
				// This should never happen.
				throw new PublicException("Unexpected delivery value total", "delivery/fault");
			}

			var apportion = (double)valueTaxed / (double)totalValue;

			return new DeliveryPricingInfo()
			{
				TaxApportion  = apportion,
				ValueTaxed = valueTaxed,
				ValueUntaxed = valueUntaxed
			};
		}
	}
	
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