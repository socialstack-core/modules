using System;

namespace Api.Currency
{
	/// <summary>
	/// Use this to declare a database field as a price
	/// When a price field is also localized, it will either calculated using a conversion rate or contain different values per locale.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class PriceAttribute : Attribute
	{
		public PriceAttribute() { }
	}
}
