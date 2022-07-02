using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;

namespace Api.WebRTC.Ciphers;


/// <summary>
/// A lookup of curves for the key exchange.
/// </summary>
public static class CurveLookup
{
	/// <summary>
	/// Active curves.
	/// </summary>
	public static string[] Active = new string[]{
		// "x448",   // Signature algorithm here is incorrect.
		// "x25519", // Signature algorithm here is incorrect; Browsers don't support these yet anyway (Apr 2022).
		"secp521r1",
		"secp384r1",
		"secp256r1"
	};

	private static Dictionary<ushort, CurveInfo> _curveLookup;

	/// <summary>
	/// Loads active curves into the lookup
	/// </summary>
	private static void LoadCurves()
	{
		var cu = new Dictionary<ushort, CurveInfo>();

		for (var i = 0; i < Active.Length; i++)
		{
			var curve = LoadCurve(Active[i]);
			curve.Init();

			if (curve == null)
			{
				System.Console.WriteLine("Warning: Unrecognised TLS curve - " + Active[i]);
				continue;
			}

			cu[curve.SignatureAlgorithmTlsId] = curve;
		}

		_curveLookup = cu;
	}

	/// <summary>
	/// Loads curve info from the IANA name. Must call Init on the returned curve to get it to setup internal values.
	/// </summary>
	/// <param name="ianaName"></param>
	/// <returns></returns>
	private static CurveInfo LoadCurve(string ianaName)
	{
		X9ECParameters curve;
		ECDomainParameters domainParameters;

		switch (ianaName)
		{
			case "x448":
				curve = Org.BouncyCastle.Crypto.EC.CustomNamedCurves.GetByName("Curve448");
				domainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

				return new CurveInfo("x448", 0x001e, 1, 0x0808)
				{
					DomainParameters = domainParameters,
					Digest = new Org.BouncyCastle.Crypto.Digests.Sha256Digest()
				};
			case "x25519":
				curve = Org.BouncyCastle.Crypto.EC.CustomNamedCurves.GetByName("Curve25519");
				domainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

				return new CurveInfo("x25519", 0x001d, 2, 0x0807)
				{
					DomainParameters = domainParameters,
					Digest = new Org.BouncyCastle.Crypto.Digests.Sha256Digest()
				};
			case "secp521r1":
				curve = ECNamedCurveTable.GetByName("secp521r1");
				domainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

				return new CurveInfo("secp521r1", 0x0019, 3, 0x0603)
				{
					DomainParameters = domainParameters,
					Digest = new Org.BouncyCastle.Crypto.Digests.Sha512Digest()
				};
			case "secp384r1":
				curve = ECNamedCurveTable.GetByName("secp384r1");
				domainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

				return new CurveInfo("secp384r1", 0x0018, 4, 0x0503)
				{
					DomainParameters = domainParameters,
					Digest = new Org.BouncyCastle.Crypto.Digests.Sha384Digest()
				};
			case "secp256r1":
				curve = ECNamedCurveTable.GetByName("secp256r1");
				domainParameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());

				return new CurveInfo("secp256r1", 0x0017, 5, 0x0403)
				{
					DomainParameters = domainParameters,
					Digest = new Org.BouncyCastle.Crypto.Digests.Sha256Digest()
				};
		}

		// Unsupported curve
		return null;
	}

	/// <summary>
	/// Get a curve by its signature algo TLS ID.
	/// </summary>
	/// <param name="tlsCipherId"></param>
	/// <returns></returns>
	public static CurveInfo GetCurveBySignatureAlgorithm(ushort tlsCipherId)
	{

		if (_curveLookup == null)
		{
			// Only loads the ones we have active though:
			LoadCurves();
		}

		_curveLookup.TryGetValue(tlsCipherId, out CurveInfo result);
		return result;
	}

}
