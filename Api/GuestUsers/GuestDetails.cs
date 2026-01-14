using Api.Addresses;
using System.Collections.Generic;

namespace Api.GuestUsers
{
	/// <summary>
	/// Temp store for guest details
	/// </summary>
	public partial class GuestDetails
	{
		/// <summary>
		/// The guest user's email address.
		/// </summary>
		public string Email { get; set; }

		/// <summary>
		/// The first name of the guest user. 
		/// </summary>
		public string FirstName { get; set; }

		/// <summary>
		/// The last name(s) of the guest user. 
		/// </summary>
		public string LastName { get; set; }

		/// <summary>
		/// The address set linked to the guest 
		/// </summary>
		public List<Address> Addresses { get; set; }

	}
}
