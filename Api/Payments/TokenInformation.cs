using Newtonsoft.Json;
using System;

namespace Api.Payments;


/// <summary>
/// Used to describe information about a gateway token (e.g. a tokenised card).
/// </summary>
public struct TokenInformation
{
	/// <summary>
	/// True/ false if the token exists.
	/// </summary>
	public bool Valid;
	
	/// <summary>
	/// The human readable name.
	/// </summary>
	public string Name;
	
	/// <summary>
	/// The tokens expiry date.
	/// </summary>
	public DateTime ExpiryUtc;
	
	/// <summary>
	/// Other gateway specific data.
	/// </summary>
	[JsonIgnore]
	public object GatewayData;
}