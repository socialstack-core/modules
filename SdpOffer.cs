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
	/// The SDP
	/// </summary>
	public string Sdp;
}