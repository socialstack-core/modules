using System;

namespace Api.Currency
{
	/// <summary>
	/// Use this to declare a database field as a price
	/// When a price field is also localized, it will not fall back to the default locale if null, instead it will be converted using an exchange rate or returned as null if converison is not possible.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	internal sealed class PriceAttribute : Attribute
	{
		public PriceAttribute() { }
	}
}
