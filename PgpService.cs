using System;
using System.IO;
using System.Configuration;
using System.Security.Cryptography;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Text;


namespace Api.Pgp
{
	/// <summary>
	/// Helper service for PGP.
	/// </summary>
	public class PgpService : AutoService
	{
        /// <summary>
        /// Encrypts the given stream with PGP (RSA 4096)
        /// </summary>
        public void EncryptPgpFile(Stream inputDataStream, Stream outputStream, PgpPublicKey key, bool withIntegrityCheck = true, bool armour = false)
        {
            // armour is for the benefit of oldschool ASCII only email clients. Nowadays it's not really needed.

            using (MemoryStream outputBytes = new MemoryStream())
            {
                PgpCompressedDataGenerator dataCompressor = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                
                var inputStream = dataCompressor.Open(outputBytes);
                inputDataStream.CopyTo(inputStream);

                dataCompressor.Close();
                PgpEncryptedDataGenerator dataGenerator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, withIntegrityCheck, new SecureRandom());
                dataGenerator.AddMethod(key);

                long dataLength = outputBytes.Length;
                
                if (armour)
                {
                    using (ArmoredOutputStream armoredStream = new ArmoredOutputStream(outputStream))
                    {
                        var str = dataGenerator.Open(armoredStream, dataLength);
                        outputBytes.CopyTo(str);
                    }
                }
                else
                {
                    var str = dataGenerator.Open(outputStream, dataLength);
                    outputBytes.CopyTo(str);
                }
            }
        }
		
		/// <summary>
		/// Loads a public key.
		/// </summary>
		public PgpPublicKey LoadKey(string key)
		{
			if (string.IsNullOrEmpty(key))
            {
                return null;
            }
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(key));
			return ReadPublicKey(stream);
		}
		
		/// <summary>
		/// Loads a public key from the given stream.
		/// </summary>
        public PgpPublicKey LoadKey(Stream inputStream)
        {
            inputStream = PgpUtilities.GetDecoderStream(inputStream);
            PgpPublicKeyRingBundle pgpPub = new PgpPublicKeyRingBundle(inputStream);

            foreach (PgpPublicKeyRing keyRing in pgpPub.GetKeyRings())
            {
                foreach (PgpPublicKey key in keyRing.GetPublicKeys())
                {
                    if (key.IsEncryptionKey)
                    {
                        return key;
                    }
                }
            }

            throw new ArgumentException("Can't find encryption key in key ring.");
        }
	}
}