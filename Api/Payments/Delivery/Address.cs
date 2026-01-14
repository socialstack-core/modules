
namespace Api.Addresses;

/// <summary>
/// An Address
/// </summary>
public partial class Address 
{
	/// <summary>
	/// True if this is the default delivery address.
	/// </summary>
	public bool IsDefaultDeliveryAddress;

	/// <summary>
	/// True if this is the default billing address.
	/// </summary>
	public bool IsDefaultBillingAddress;
	
}