using Newtonsoft.Json;
using System.Text.Json.Serialization;
namespace Api.Payments;

/// <summary>
/// Used to describe information passed from the payment verification service 3D secure etc 
/// </summary>
public struct ChallengeResponse
{
	/// <summary>
	/// 3DS challenge request payload (cReq) for browser redirection.
	/// A Base64 encoded message to be passed to the Issuing Bank as part of the 3D Secure Authentication. 
	/// This replaces the PAReq. When forwarding the cReq to the acsUrl, pass it in a field called creq (note the lower case cr). 
	/// This avoids issues at the ACS which expects the fieldname to be all lowercase.
	/// </summary>
	[JsonProperty("cRes")]
	//[JsonPropertyName("cRes")]
	public string CRes { get; set; }

	/// <summary>
	/// One time token value to provide a link back to the purchase
	/// </summary>
	public string Token;

}