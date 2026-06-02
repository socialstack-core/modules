using Api.Database;
using Api.Startup;
using System.Threading.Tasks;
using System;
using Api.SocketServerLibrary;
using Api.Contexts;
using System.Collections.Generic;
namespace Api.Payments;


/// <summary>
/// A virtual field value generator for a field called "calculatedPrice". It can only be used on a Product.
/// Automatically instanced and the include field name is derived from the class name by the includes system. See VirtualFieldValueGenerator for more info.
/// </summary>
public partial class CalculatedPriceValueGenerator<T, ID> : VirtualFieldValueGenerator<T, ID>
	where T : Content<ID>, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	private ProductService _productService;
	private PriceService _priceService;
	
	/// <summary>
	/// Generate the value.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="forObject"></param>
	/// <param name="writer"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public override async ValueTask GetValue(Context context, T forObject, Writer writer, ContextFlags flags)
	{
		if(typeof(T) == typeof(Product))
		{
			var product = (Product)((object)forObject);
			var tiers = await GetValueForProduct(context, product, writer);

			if (_priceService == null)
			{
				_priceService = Services.Get<PriceService>();
			}

			// Default locale tax jurisdiction.
			var taxCalculator = await _priceService.GetTaxCalculator(context, null);

			// Resolves instantly:
			var locale = await context.GetLocale();
			var currencyCode = locale.CurrencyCode;
			// Falls through to the null otherwise.
			if (tiers != null)
			{
				writer.WriteASCII("{\"listPrice\":[");
				await PopulatePricesWriter(context, tiers.Original, writer, taxCalculator, product, currencyCode);
				writer.Write((byte)']');

				if (tiers.Discounted != null && tiers.Discounted.Count > 0)
				{
					writer.WriteASCII(",\"discountedPrice\":[");
					await PopulatePricesWriter(context, tiers.Discounted, writer, taxCalculator, product, currencyCode);
					writer.Write((byte)']');
				}

				writer.Write((byte)'}');
				return;
			}
		}
		
		writer.WriteASCII("null");
	}

	private async ValueTask PopulatePricesWriter(
		Context context,
		List<Price> tiers,
		Writer writer,
		TaxCalculator taxCalculator,
		Product product,
		string currencyCode
	){
		if (tiers == null)
		{
			return;
		}
		
		for (var i = 0; i < tiers.Count; i++)
		{
			if (i != 0)
			{
				writer.Write((byte)',');
			}

			var tier = tiers[i];
			if (!tier.Amount.TryGet(context, out uint amountLessTax))
			{
				writer.WriteASCII("null");
				continue;
			}

			ulong amount = amountLessTax;

			if (taxCalculator != null)
			{
				if(product.TaxExempt == 0)
				{
					amount = taxCalculator.Apply(amount);
				}
				else if(product.TaxExempt == 2)
				{
					amount = await taxCalculator.Apply(context, amount);
				}
			}

			writer.WriteASCII("{\"amount\":");
			writer.WriteS(amount);
			writer.WriteASCII(",\"amountLessTax\":");
			writer.WriteS(amountLessTax);
			writer.WriteASCII(",\"currencyCode\":");
			writer.WriteEscaped(currencyCode);
			writer.WriteASCII(",\"minimumQuantity\":");
			writer.WriteS(tier.MinimumQuantity);

			writer.Write((byte)'}');
		}
	}

	/// <summary>
	/// Gets the price info for a given product.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="product"></param>
	/// <param name="writer"></param>
	/// <returns></returns>
	public async ValueTask<PriceComparison> GetValueForProduct(Context context, Product product, Writer writer)
	{
		// Get the product:
		if (_productService == null)
		{
			_productService = Services.Get<ProductService>();
		}

		if (product == null)
		{
			return null;
		}

		var tiers = await _productService.GetPriceTiers(context, product);

		return tiers;
	}
	
	/// <summary>
	/// The type, if any, associated with the value being outputted.
	/// For example, if GetValue outputs only strings, this is typeof(string).
	/// </summary>
	/// <returns></returns>
	public override Type OutputType => typeof(CalculatedPriceGroups);
}

/// <summary>
/// Holds a collection of list and discounted prices.
/// </summary>
public struct CalculatedPriceGroups
{
	/// <summary>
	/// List of list (standard) prices
	/// </summary>
	public List<PriceCurrency> ListPrice;
	/// <summary>
	/// List of discounted prices
	/// </summary>
	public List<PriceCurrency> DiscountedPrice;
}