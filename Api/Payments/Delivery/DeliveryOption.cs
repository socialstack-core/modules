using System;
using Api.Database;
using Api.Translate;
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
	public string InformationJson;

	/// <summary>
	/// Target address ID.
	/// </summary>
	public uint AddressId;

	/// <summary>
	/// The cart that this option is associated with.
	/// </summary>
	public uint ShoppingCartId;
}