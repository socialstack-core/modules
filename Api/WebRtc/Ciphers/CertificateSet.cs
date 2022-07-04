using System;
using System.Collections;
using System.Collections.Generic;


namespace Api.WebRTC;

/// <summary>
/// A set of usually 2 certificates.
/// This exists to support clients that don't have ECDSA capabilities/ are RSA only.
/// </summary>
public class CertificateSet
{
	/// <summary>
	/// All the certificates in this set.
	/// </summary>
	public List<Certificate> Certificates = new List<Certificate>();
	
	/// <summary>
	/// The SDP fingerprints for the certificates, separated by newlines.
	/// This is used by the WebRTC offer/ answer phase.
	/// </summary>
	public string SdpFingerprints;
	
	/// <summary>
	/// The time when this set was generated. Used to establish if it should be expired.
	/// </summary>
	public DateTime GeneratedAt = DateTime.UtcNow;

	/// <summary>
	/// Gets a cert by if it's RSA or not.
	/// </summary>
	/// <param name="isRsa"></param>
	/// <returns></returns>
	public Certificate GetByType(bool isRsa)
	{
		for (var i = 0; i < Certificates.Count; i++)
		{
			var cert = Certificates[i];

			if (cert.IsRSA == isRsa)
			{
				return cert;
			}
		}

		return null;
	}

	/// <summary>
	/// Call when you're done adding certs to the set.
	/// </summary>
	public void FinishedAdding()
	{
		SdpFingerprints = "";

		for (var i = 0; i < Certificates.Count; i++) {

			if (i != 0)
			{
				SdpFingerprints += "\r\n";
			}

			var cert = Certificates[i];
			SdpFingerprints += "a=fingerprint:" + cert.Fingerprint256;
		} 
	}
}