using Api.AutoForms;
using Api.GuestUsers;
using Api.Permissions;
using Api.Startup;

namespace Api.Payments
{
	/// <summary>
	/// A Purchase
	/// </summary>

	// Which guest user is linked to the purchase
	[HasVirtualField("GuestUser", typeof(GuestUser), "GuestUserId")]

	public partial class Purchase
    {
		/// <summary>
		/// The guest user created for this purchase
		/// </summary>
		[Permissions(WriteRule = "false", Roles="!admins")]
		[Module(Hide = true)]
		public uint? GuestUserId;

	}
}
