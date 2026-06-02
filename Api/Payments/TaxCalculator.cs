using System;
using System.Threading.Tasks;
using Api.CanvasRenderer;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Payments;


/// <summary>
/// Calculates tax for a jurisdiction.
/// </summary>
public class TaxCalculator
{
	
	/// <summary>
	/// Creates a new tax calculator from the given config.
	/// </summary>
	public TaxCalculator(TaxConfiguration config, RoundingMode roundingMode = RoundingMode.Down)
	{
		Multiplier = (config.Multiplier / 100d) + 1d;
		RoundingMode = roundingMode;
	}
	
	/// <summary>
	/// Overall multiplier.
	/// </summary>
	public double Multiplier;

	/// <summary>
	/// How should VAT be rounded
	/// </summary>
	public RoundingMode RoundingMode;
	
	/// <summary>
	/// Applies this tax calculator to the given pence/cents value.
	/// </summary>
	public ulong Apply (ulong amount)
	{
		var total = amount * Multiplier;
		return Round(total);
	}

	/// <summary>
	/// Applies this tax calculator to the given apportioned amount.
	/// </summary>
	/// <param name="amount"></param>
	/// <param name="apportion">0-1 taxable apportion.</param>
	/// <returns></returns>
	public ulong Apply(ulong amount, double apportion)
	{
		// E.g. Apply(100, 0.6)
		// means 60% is taxed, the other 40% is not.
		// So we take the 40% part out and add it back to the tax multiplied 60%.

		var total = (amount * (1 - apportion)) + (amount * apportion * Multiplier);
		return Round(total);
	}

	/// <summary>
	/// Applies this tax calculator to the given pence/cents value.
	/// </summary>
	public async ValueTask<ulong> Apply (Context context, ulong amount)
	{
		//Confirms if a tax calculator is needed
		var conditionalCalculator = await Events.Price.BeforeApplyTaxCalculation.Dispatch(context, new ConditionalVATCalculator(), this);

		if(conditionalCalculator.Multiplier.HasValue)
		{
			return conditionalCalculator.Apply(amount);
		}

		return Apply(amount);
	}

	/// <summary>
	/// Calculate the tax on a complete list of products rather than line by line
	/// </summary>
	/// <param name="collection"></param>
	/// <param name="deliveryCost"></param>
	/// <param name="lineByLine"></param>
	/// <returns></returns>
	public ulong Apply(ProductQuantityPricing collection, ulong deliveryCost = 0, bool lineByLine = true)
	{
		if(collection == null || collection.TotalLessTax == 0)
		{
			return Apply(collection.TotalLessTax + deliveryCost);
		}

		var deliveryInfo = collection.DeliveryPricingInfo;

		if(!deliveryInfo.HasValue)
		{
			throw new PublicException("Could not calculate delivery pricing information", "TaxCalculator/invalid_collection");
		}

		if(lineByLine)
		{
			return Apply(collection.TotalLessTax + deliveryCost, deliveryInfo.Value.TaxApportion);
		}

		//We have to calculate VAT on the total
		return Apply(deliveryInfo.Value.ValueTaxed + deliveryCost) + deliveryInfo.Value.ValueUntaxed;
		
	}

	private ulong Round(double total)
	{
		if(RoundingMode == RoundingMode.Down)
		{
			// Naturally floors.
			return (ulong)total;
		}else if(RoundingMode == RoundingMode.Nearest)
		{
			return (ulong)Math.Round(total);
		}
		else
		{
			return (ulong)Math.Ceiling(total);
		}
	}
}

/// <summary>
/// Used to calculate the VAT if the VAT is conditionally altered
/// </summary>
public struct ConditionalVATCalculator
{
	/// <summary>
	/// VAT Multiplier
	/// </summary>
	public uint? Multiplier;

	/// <summary>
	/// Applies this tax calculator to the given pence/cents value.
	/// </summary>
	public ulong Apply (ulong amount)
	{
		// Naturally floors.
		return (ulong)(amount * Multiplier);
	}
}