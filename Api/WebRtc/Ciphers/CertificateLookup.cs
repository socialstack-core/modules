using Api.SocketServerLibrary;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;

namespace Api.WebRTC;


/// <summary>
/// Used to get cached certificates.
/// </summary>
public static class CertificateLookup
{
    /// <summary>
    /// Generates an elliptic curve certificate.
    /// </summary>
    public static Certificate GenerateEllipticCertificate()
    {
        var certificateGenerator = GenerateCertificate();

        // Create a pubkey:
        var keyParams = new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, _rng);
        var generator = new ECKeyPairGenerator("ECDSA");
        generator.Init(keyParams);
        var subjectKeyPair = generator.GenerateKeyPair();

        certificateGenerator.SetPublicKey(subjectKeyPair.Public);

        // Sign the cert:
        var signatureFactory = new Asn1SignatureFactory("SHA256WITHECDSA", subjectKeyPair.Private, _rng);

        // Complete the generation:
        X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

        return new Certificate(certificate, false)
		{
            PrivateKey = subjectKeyPair.Private
		};
    }

    /// <summary>
    /// Generates an RSA certificate.
    /// </summary>
    public static Certificate GenerateRSACertificate()
    {
        var certificateGenerator = GenerateCertificate();

        // Create a pubkey:
        var keyParams = new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), _rng, 2048, 80);
        var generator = new RsaKeyPairGenerator();
        generator.Init(keyParams);
        var subjectKeyPair = generator.GenerateKeyPair();

        certificateGenerator.SetPublicKey(subjectKeyPair.Public);

        // Sign the cert:
        var signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", subjectKeyPair.Private, _rng);

        // Complete the generation:
        X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

        return new Certificate(certificate, true)
        {
            PrivateKey = subjectKeyPair.Private
        };
    }

    /// <summary>
    /// RNG engine for certs.
    /// </summary>
    private static SecureRandom _rng = new SecureRandom();

    /// <summary>
    /// Starts generating a certificate.
    /// </summary>
    /// <returns></returns>
    private static X509V3CertificateGenerator GenerateCertificate()
    {
        // The Certificate Generator
        var certificateGenerator = new X509V3CertificateGenerator();

        // Serial Number
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), _rng);
        certificateGenerator.SetSerialNumber(serialNumber);

        // Issuer and Subject Name
        X509Name subjectDN = new X509Name("CN=WebRTC");
        X509Name issuerDN = subjectDN;
        certificateGenerator.SetIssuerDN(issuerDN);
        certificateGenerator.SetSubjectDN(subjectDN);

        // Valid For
        DateTime notBefore = DateTime.UtcNow.Date;
        DateTime notAfter = notBefore.AddMonths(3);

        certificateGenerator.SetNotBefore(notBefore);
        certificateGenerator.SetNotAfter(notAfter);

        return certificateGenerator;
    }

    private static CertificateSet _set;

    /// <summary>
    /// Gets (or generates) the set of certificates.
    /// Note that the set returned must be used for a whole handshake - there is a risk of being given a newly generated set during the handshake otherwise.
    /// </summary>
    /// <returns></returns>
    public static CertificateSet GetCertificateSet()
    {
        var now = DateTime.UtcNow;

        if (_set == null || (now - _set.GeneratedAt).TotalDays > 30)
        {
            // Generate (or regenerate) the set now.
            var set = new CertificateSet();

            set.Certificates.Add(GenerateEllipticCertificate());
            // set.Certificates.Add(GenerateRSACertificate()); // Chrome doesn't like >1 fingerprint in the SDP.

            // This generates some internal field values:
            set.FinishedAdding();

            _set = set;
        }

        return _set;
    }

}
