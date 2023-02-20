using System;
using Api.Database;
using Api.Startup;
using Api.Translate;
using Api.Users;


namespace Api.Currency
{

	/// <summary>
	/// An ExchangeRate
	/// </summary>
	[HasVirtualField("FromLocale", typeof(Locale), "FromLocaleId")]
	[HasVirtualField("ToLocale", typeof(Locale), "ToLocaleId")]
	public partial class ExchangeRate : VersionedContent<uint>
	{
		/// <summary>
		/// The name of this exchange rate
		/// </summary>
		public string Name;
		
		/// <summary>
		/// The currency to be converted from
		/// </summary>
		public uint FromLocaleId;

		/// <summary>
		/// The currency to be converted to
		/// </summary>
		public uint ToLocaleId;

		/// <summary>
		/// The exchange rate
		/// </summary>
		public double Rate;
	}

}