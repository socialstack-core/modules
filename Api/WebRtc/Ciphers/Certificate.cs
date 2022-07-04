using Api.SocketServerLibrary;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.X509;

namespace Api.WebRTC;



/// <summary>
/// Retains certificate info.
/// </summary>
public class Certificate
{
	/// <summary>
	/// The bytes of this certificate.
	/// </summary>
	public byte[] CertificatePayload;

	/// <summary>
	/// The sha-256 fingerprint of the cert.
	/// </summary>
	public string Fingerprint256;

	/// <summary>
	/// The private key of this cert.
	/// </summary>
	public AsymmetricKeyParameter PrivateKey;

	/// <summary>
	/// The X509 cert in full.
	/// </summary>
	public X509Certificate X509;

	/// <summary>
	/// True if RSA, false for elliptic certs.
	/// </summary>
	public bool IsRSA;

	/// <summary>
	/// Creates a new certificate.
	/// </summary>
	/// <param name="cert"></param>
	/// <param name="isRSA"></param>
	public Certificate(X509Certificate cert, bool isRSA)
	{
		IsRSA = isRSA;
		X509 = cert;
		CertificatePayload = cert.GetEncoded();
		Fingerprint256 = "sha-256 " + GetFingerprint(CertificatePayload);
	}

	/// <summary>
	/// Saves this cert to 2 .pem files
	/// </summary>
	/// <param name="publicCertFile"></param>
	/// <param name="privateKeyFile"></param>
	public void SaveToFile(string publicCertFile, string privateKeyFile)
	{
        // Save pk to file:
        var textWriter = new System.IO.StringWriter();
        var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
        pemWriter.WriteObject(PrivateKey);
        pemWriter.Writer.Flush();

        string privateKey = textWriter.ToString();
        System.IO.File.WriteAllText(privateKeyFile, privateKey);

        textWriter = new System.IO.StringWriter();
        pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
        pemWriter.WriteObject(X509);
        pemWriter.Writer.Flush();

        var certText = textWriter.ToString();
        System.IO.File.WriteAllText(publicCertFile, certText);
	}

	private static string GetFingerprint(byte[] der)
	{
		byte[] sha1 = Sha256DigestOf(der);
		return Hex.ConvertWithSeparator(sha1, ':');
	}

	private static byte[] Sha256DigestOf(byte[] input)
	{
		var d = new Sha256Digest();
		d.BlockUpdate(input, 0, input.Length);
		byte[] result = new byte[d.GetDigestSize()];
		d.DoFinal(result, 0);
		return result;
	}

}