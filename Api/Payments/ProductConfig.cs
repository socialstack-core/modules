using Api.Configuration;

namespace Api.Payments;

/// <summary>
/// Config for the products part of the payment system.
/// </summary>
public class ProductConfig : Config
{
	/// <summary>
	/// True if it should error if an order for less than the min is placed.
	/// Otherwise it will be rounded up.
	/// </summary>
	public bool ErrorIfBelowMinimum {get; set;}
	
}