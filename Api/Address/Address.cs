using Api.AutoForms;
using Api.Database;
using Api.Users;

namespace Api.Addresses;

/// <summary>
/// An Address
/// </summary>
public partial class Address : VersionedContent<uint>
{    /// <summary>
    /// Descriptive name for the address home,work business etc 
    /// </summary>
    public string Name;
	
    /// <summary>
    /// Random string that grants anonymous users with the appropriate cart access to this address
    /// </summary>
    public string AnonKey;
	
    /// <summary>
    /// The type of address payment/business (0 = payment/1 = business)
    /// </summary>
    [Module("Admin/Address/AddressTypes")]
    public uint AddressType; 

	/// <summary>
	/// Resolved UPRN of this address, if known.
	/// </summary>
	public ulong? Uprn;
	
	/// <summary>
	/// Resolved latitude of this address, if known.
	/// </summary>
	public double? Latitude;
	
	/// <summary>
	/// Resolved longitude of this address, if known.
	/// </summary>
	public double? Longitude;
	
	/// <summary>
	/// Address line 2, if present.
	/// </summary>
	public string Line1;
	
	/// <summary>
	/// Address line 2, if present.
	/// </summary>
	public string Line2;
	
	/// <summary>
	/// Address line 3, if present.
	/// </summary>
	public string Line3;
	
	/// <summary>
	/// Address city.
	/// </summary>
	public string City;

    /// <summary>
    /// Address county.
    /// </summary>
    public string County;

    /// <summary>
    /// Address postcode.
    /// </summary>
    public string Postcode;

    /// <summary>
    /// 2 character ISO code
    /// </summary>
    [DatabaseField(2)]
    public string CountryCode;

    /// <summary>
    /// The feature image ref
    /// </summary>
    [DatabaseField(Length = 300)]
    public string FeatureRef;

	/// <summary>
	/// Address contact name.
	/// </summary>
	public string Contact;

	/// <summary>
	/// Address contact telephone number.
	/// </summary>
	public string TelNo;


}