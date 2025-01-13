using LetsEncrypt.Client.Cryptography;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.Client.Entities
{
    /// <summary>
    /// </summary>
    public class Certificate
    {
        #region Consts + Fields + Properties

        private CertificateChain _certificateChain;

        /// <summary>
        /// </summary>
        public RsaKeyPair Key { get; private set; }

        #endregion Consts + Fields + Properties

        // Ctor

        /// <summary>
        /// </summary>
        public Certificate(RsaKeyPair key = null)
        {
            // Generate new RSA key for certificate
            if (key == null)
            {
                Key = RsaKeyPair.New();
            }
            else
            {
                Key = key;
            }
        }

        // Public Methods

        /// <summary>
        /// </summary>
        public byte[] CreateSigningRequest(string cn, List<string> subjectAlternativeNames)
        {
            return CertificateBuilder.CreateSigningRequest(Key.ToRSA(), cn, subjectAlternativeNames);
        }

        /// <summary>
        /// </summary>
        public void AddChain(CertificateChain certificateChain)
        {
            _certificateChain = certificateChain;
        }

        /// <summary>
        /// </summary>
        public byte[] GetOriginalCertificate()
        {
            return _certificateChain.CertificateBytes;
        }

        /// <summary>
        /// </summary>
        public byte[] GeneratePfx(string password)
        {
            return CertificateBuilder.Generate(Key.ToRSA(), _certificateChain, password, X509ContentType.Pfx);
        }

        /// <summary>
        /// </summary>
        public byte[] GenerateCrt(string password)
        {
            return CertificateBuilder.Generate(Key.ToRSA(), _certificateChain, password, X509ContentType.Cert);
        }

        /// <summary>
        /// </summary>
        public string GenerateCrtPem(string password)
        {
            return string.Format(
                "-----BEGIN CERTIFICATE-----\n{0}\n-----END CERTIFICATE-----",
                Convert.ToBase64String(GenerateCrt(password)));
        }

        /// <summary>
        /// </summary>
        public string GenerateKeyPem()
        {
            return Key.ToPrivateKeyPem();
        }

        /// <summary>
        /// </summary>
        public string Serialize()
        {
            return _certificateChain.Content;
        }

        /// <summary>
        /// </summary>
        public static Certificate Deserialize(string data, RsaKeyPair key)
        {
            var result = new Certificate(key);
            result.AddChain(new CertificateChain(data));
            return result;
        }
    }
}