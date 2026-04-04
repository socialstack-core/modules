using Newtonsoft.Json;
namespace Api.Payments;

/// <summary>
/// Used to describe information passed from the payment verification service from a hosted page transaction
/// </summary>
public struct HostedPageResponse
{
	/// <summary>
    /// The transaction id
    /// </summary>
	[JsonProperty("transactionId")]
	public string TransactionId  { get; set; }

	/// <summary>
	/// The purchase ref 
	/// </summary>
	[JsonProperty("reference")]
	public string Reference { get; set; }

	/// <summary>
	/// One time token value to provide a link back to the purchase
	/// </summary>
	[JsonProperty("token")]
	public string Token { get; set; }

	/// <summary>
	/// The status passed back from the gateway
	/// success
	/// cancel
	/// </summary>
	[JsonProperty("status")]
	public string Status { get; set; }	

}