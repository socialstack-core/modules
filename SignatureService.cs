using Api.Configuration;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using System.Security.Cryptography;
using System;


namespace Api.Signatures
{
	/// <summary>
	/// Handles generation and validation of signatures used for e.g. serving of private files.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SignatureService
    {

		private KeyPair _keyPair;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SignatureService()
        {

			// Generate or load keypair. First check if it's in appsettings:
			var appsettingsConfig = AppSettings.GetSection("SignatureService").Get<SignatureServiceConfig>();
			
			if(appsettingsConfig != null && !string.IsNullOrEmpty(appsettingsConfig.Private))
			{
				_keyPair = KeyPair.FromSerialized(appsettingsConfig.Public, appsettingsConfig.Private);
			}
			else
			{
				var filePath = "signatureService.key";

				if (System.IO.File.Exists(filePath))
				{
					_keyPair = KeyPair.FromSerialized(System.IO.File.ReadAllText(filePath));
				}
				else
				{
					_keyPair = KeyPair.Generate();
					System.IO.File.WriteAllText(filePath, _keyPair.Serialize());
				}
			}
			
			// Test the integrity of the key:
			var roundTrip = "signatureServiceTest";
			var sig = Sign(roundTrip);
			var result = ValidateSignature(roundTrip, sig);

			if (!result)
			{
				throw new Exception("You have a broken signatureService.key - delete it, and restart the API to generate a new one.");
			}
		}
		
		/// <summary>
		/// Generates a signature for the given piece of text.
		/// The timestamp will be appended to the end of the valueToSign as ?t={timestamp}.
		/// </summary>
		/// <returns></returns>
		public string Sign(string valueToSign, long timestamp)
		{
			return _keyPair.SignBase64(valueToSign + "?t=" + timestamp);
		}

		/// <summary>
		/// Validates a signature for a given signed value.
		/// </summary>
		/// <param name="signedValue">The value - usually a URL - being signed.</param>
		/// <param name="signatureB64">Base64</param>
		/// <param name="publicKeyHex">Hex formatted public key</param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, string signatureB64, string publicKeyHex)
		{
			var _verifier = new ECDsaSigner();
			_verifier.Init(false, KeyPair.LoadPublicKeyHex(publicKeyHex));

			var signature = System.Convert.FromBase64String(signatureB64);

			try
			{
				var rLength = (int)signature[0]; // first byte contains length of r array
				var r = new BigInteger(1, signature, 1, rLength);
				var s = new BigInteger(1, signature, rLength + 1, signature.Length - (rLength + 1));

				var messageBytes = System.Text.Encoding.UTF8.GetBytes(signedValue);

				// Can't share this as it has internal properties which get set during ComputeHash
				var sha256 = new SHA256Managed();

				// Double sha256 hash (Bitcoin compatible):
				messageBytes = sha256.ComputeHash(messageBytes, 0, messageBytes.Length);
				messageBytes = sha256.ComputeHash(messageBytes);
				lock (_verifier)
				{
					return _verifier.VerifySignature(messageBytes, r, s);
				}
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}
		}

		/// <summary>
		/// Validates a signature for a given signed value. The timestamp will be appended to the end as ?t={timestamp}.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="timestamp">The timestamp.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, long timestamp, string signature)
		{
			return _keyPair.Verify(signedValue + "?t=" + timestamp, signature);
		}

		/// <summary>
		/// Generates a signature for the given piece of text as-is.
		/// </summary>
		/// <returns></returns>
		public string Sign(string valueToSign)
		{
			return _keyPair.SignBase64(valueToSign);
		}

		/// <summary>
		/// Validates a signature for a given signed value as-is.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, string signature)
		{
			return _keyPair.Verify(signedValue, signature);
		}

	}

}
