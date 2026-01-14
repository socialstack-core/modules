using Api.Configuration;

namespace Api.Payments;

/// <summary>
/// Config for the products part of the payment system.
/// </summary>
public class CouponConfig : Config
{
    /// <summary>
    /// How to handle rounding for a discount
    /// </summary>
	public RoundingMode DiscountRounding = RoundingMode.Down;
}

/// <summary>
/// Rounding options
/// </summary>
public enum RoundingMode
{
    /// <summary>
    /// Always round down
    /// </summary>
    Down,

    /// <summary>
    /// Always round up
    /// </summary>
    Up,

    /// <summary>
    /// Always round to nearest
    /// </summary>
    Nearest
}