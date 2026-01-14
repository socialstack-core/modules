using Api.GuestUsers;
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
		public uint? GuestUserId;

	}
}