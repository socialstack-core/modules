namespace Api.WebRTC;

/// <summary>
/// An offer including an SDP. This is suitable for serialising and sending to the end user.
/// </summary>
public struct SdpOffer
{
	/// <summary>
	/// type=offer
	/// </summary>
	public string Type = "offer";

	/// <summary>
	/// The SDP's header - indicates ICE info etc
	/// </summary>
	public string Header;

	/// <summary>
	/// Candidate info
	/// </summary>
	public string Candidate;
}