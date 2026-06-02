using Api.Addresses;
using Api.Startup;
using Api.Users;


namespace Api.Payments;

/// <summary>
/// A delivery option.
/// </summary>
public partial class DeliveryOption : VersionedContent<uint>
{
	/// <summary>
	/// The info about this delivery option. It's the stringified DeliveryEstimate.
	/// </summary>
	[JsonOptions(PublicType = typeof(DeliveryEstimate))]
	public string InformationJson;

	/// <summary>
	/// Target address ID.
	/// </summary>
	public uint AddressId;

	/// <summary>
	/// The cart that this option is associated with.
	/// </summary>
	public uint ShoppingCartId;

	/// <summary>
    /// An anonymous key used to confirm that the user does have permission to call on this DeliveryOption
    /// </summary>
    public string AnonKey;
}