using Api.Addresses;
using Api.AutoForms;
using Api.Database;
using Api.Permissions;
using Api.Startup;
using Api.Users;

namespace Api.GuestUsers
{
	/// <summary>
	/// A GuestUser
	/// </summary>
	
	[Permissions(Hide = true)] /* Fields hidden by default, except for admins */
	[Permissions(Hide = false, Roles = "admins")]
	
	// A guest must have a delivery address
	[HasVirtualField("DeliveryAddress", typeof(Address), "DeliveryAddressId")]

	// A guest can have a billing address
	[HasVirtualField("BillingAddress", typeof(Address), "BillingAddressId")]

	public partial class GuestUser : VersionedContent<uint>
	{
		/// <summary>
		/// The guest user's email address.
		/// </summary>
		[DatabaseField(Length = 80)]
		[Data("required", true)]
		[Data("validate", "Required")]
		[Permissions(Rule = "IsSelf()", Roles = "*,!admins")] /* Admins can always see the field */
		public string Email;

		/// <summary>
		/// The first name of the guest user. 
		/// </summary>
		[DatabaseField(Length = 40)]
		[Data("required", true)]
		[Data("validate", "Required")]
		[Permissions(Rule = "IsSelf()", Roles = "*,!admins")] /* Admins can always see the field */
		public string FirstName;

		/// <summary>
		/// The last name(s) of the guest user. 
		/// </summary>
		[DatabaseField(Length = 40)]
		[Data("required", true)]
		[Data("validate", "Required")]
		[Permissions(Rule = "IsSelf()", Roles = "*,!admins")] /* Admins can always see the field */
		public string LastName;

		/// <summary>
		/// Default delivery address.
		/// </summary>
		[Module("Admin/ContentSelect")]
		[Data("contentType", "Address")]
		[Data("tab", "delivery")]
		[Data("tabOrder", 2)]
		public uint DeliveryAddressId;

		/// <summary>
		/// Default billing address.
		/// </summary>
		[Module("Admin/ContentSelect")]
		[Data("contentType", "Address")]
		[Data("tab", "billing")]
		[Data("tabOrder", 3)]
		public uint? BillingAddressId;


	}

}