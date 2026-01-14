using System;

namespace Api.Payments;


/// <summary>
/// Calculates tax for a jurisdiction.
/// </summary>
public class TaxCalculator
{
	
	/// <summary>
	/// Creates a new tax calculator from the given config.
	/// </summary>
	public TaxCalculator(TaxConfiguration config)
	{
		Multiplier = (config.Multiplier / 100d) + 1d;
	}
	
	/// <summary>
	/// Overall multiplier.
	/// </summary>
	public double Multiplier;
	
	
	/// <summary>
	/// Applies this tax calculator to the given pence/cents value.
	/// </summary>
	public ulong Apply(ulong amount)
	{
		// Naturally floors.
		return (ulong)(amount * Multiplier);
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

		return (ulong)( (amount * (1 - apportion)) + (amount * apportion * Multiplier) );
	}

}