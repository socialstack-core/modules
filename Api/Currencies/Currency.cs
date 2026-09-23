using System;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Currencies
{
	
	/// <summary>
	/// A Currency
	/// </summary>
	public partial class Currency : UserCreatedContent<uint>
	{
		/// <summary>
		/// Currency name.
		/// </summary>
		public string Name;
		
		/// <summary>
		/// Currency code.
		/// </summary>
		public string Code;
		
		/// <summary>
		/// Exchange rate with primary (Id #1) currency.
		/// </summary>
		public double ExchangeRateForPrimary;
		
		/// <summary>
		/// Currency symbol (e.g. £, $).
		/// </summary>
		public string Symbol;
		
	}

}