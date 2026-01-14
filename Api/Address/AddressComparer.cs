using System.Collections.Generic;
using System;

namespace Api.Addresses
{
	/// <summary>
	/// Simple address comparison, used when adding addresses from external parties to ignore duplicates
	/// </summary>
	public class AddressComparer : IEqualityComparer<Address>
	{
		/// <summary>
		/// Check if the core address fields are equal 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>

		public bool Equals(Address x, Address y)
		{
			if (x == null || y == null)
			{
				return false;
			}

			return x.Line1 == y.Line1 &&
				   x.Line2 == y.Line2 &&
				   x.City == y.City &&
				   x.County == y.County &&
				   x.Postcode == y.Postcode &&
				   x.CountryCode == y.CountryCode;
		}


		/// <summary>
		/// Generate a hash code for the address core fields
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int GetHashCode(Address obj)
		{
			return HashCode.Combine(
				obj.Line1,
				obj.Line2,
				obj.City,
				obj.County,
				obj.Postcode,
				obj.CountryCode
			);
		}
	}
}
