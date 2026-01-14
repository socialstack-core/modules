using LetsEncrypt.Client.Entities;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.Client.Cryptography
{
    /// <summary>
    /// </summary>
    public static class CertificateBuilder
    {
        // Public Methods

        /// <summary>
        /// </summary>
        public static byte[] CreateSigningRequest(RSA rsa, string cn, List<string> subjectAlternativeNames)
        {
            CertificateRequest req = new CertificateRequest($"CN={cn}",
                   rsa,
                   HashAlgorithmName.SHA256,
                   RSASignaturePadding.Pkcs1);

            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));
            req.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

            var sanb = new SubjectAlternativeNameBuilder();
            foreach (var subjectAlternativeName in subjectAlternativeNames)
            {
                sanb.AddDnsName(subjectAlternativeName);
            }
            req.CertificateExtensions.Add(sanb.Build());

            return req.CreateSigningRequest();
        }

        /// <summary>
        /// </summary>
        public static byte[] Generate(RSA rsa, CertificateChain certificateChain, string password, X509ContentType certificateType)
        {
			var certificate = X509CertificateLoader.LoadCertificate(certificateChain.CertificateBytes);
			var issuer = X509CertificateLoader.LoadCertificate(certificateChain.IssuerBytes);
			
            // Copy the certificate with the private key (RSA)
			certificate = certificate.CopyWithPrivateKey(rsa);

            // Create a collection of certificates (issuer and certificate)
            var collection = new X509Certificate2Collection();
            collection.Add(issuer);
            collection.Add(certificate);

            // Export the certificates to a byte array
            return collection.Export(certificateType, password);
        }
    }
}
