using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;

namespace Api.WebRTC.Ciphers;

/// <summary>
/// Information about curves used during ECDHE.
/// </summary>
public partial class CurveInfo
{
	
	/// <summary>
	/// IANA name
	/// </summary>
	public string Name;
	
	/// <summary>
	/// ID of this curve in TLS protocol
	/// </summary>
	public ushort TlsId;
	
	/// <summary>
	/// The lower this is, the more favourable the curve is.
	/// </summary>
	public int Priority;

	/// <summary>
	/// The digest to use with this curve for ECDHA.
	/// </summary>
	public IDigest Digest;

	/// <summary>
	/// The curve's domain parameters.
	/// </summary>
	public ECDomainParameters DomainParameters;

	/// <summary>
	/// The TLS ID of the signature algorithm when this curve is used.
	/// </summary>
	public ushort SignatureAlgorithmTlsId;

	/// <summary>
	/// Creates a new curve info.
	/// </summary>
	/// <param name="ianaName"></param>
	/// <param name="tlsId"></param>
	/// <param name="priority"></param>
	/// <param name="sigAlgorithmTlsId"></param>
	public CurveInfo(string ianaName, ushort tlsId, int priority, ushort sigAlgorithmTlsId)
	{
		TlsId = tlsId;
		Priority = priority;
		Name = ianaName;
		SignatureAlgorithmTlsId = sigAlgorithmTlsId;
	}
	
	/// <summary>
	/// Sets up any internal values if the curve needs to do so.
	/// </summary>
	public void Init()
	{
		
	}
	
}