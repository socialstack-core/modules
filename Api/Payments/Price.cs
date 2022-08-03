using System;
using Api.AutoForms;
using Api.Database;
using Api.Translate;
using Api.Users;


namespace Api.Payments
{
	
	/// <summary>
	/// A Price
	/// </summary>
	public partial class Price : VersionedContent<uint>
	{
        /// <summary>
        /// The name of the price
        /// </summary>
        [DatabaseField(Length = 200)]
		public string Name;

		/// <summary>
		/// The amount in the target currency.
		/// </summary>
		[Data("help", "A whole number in the smallest unit of the currency (pence/ cents).")]
		public uint Amount;
		
		/// <summary>
		/// The uppercase 3 character currency code. "USD", "GBP" etc.
		/// </summary>
		public string CurrencyCode;
	}

}