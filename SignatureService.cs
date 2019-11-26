using System;


namespace Api.Signatures
{
	/// <summary>
	/// Handles generation and validation of signatures used for e.g. serving of private files.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class SignatureService : ISignatureService
    {

		private KeyPair _keyPair;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public SignatureService()
        {

			// Generate or load keypair:
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
		
		/// <summary>
		/// Generates a signature for the given piece of text.
		/// The timestamp will be appended to the end of the valueToSign as ?t={timestamp}.
		/// </summary>
		/// <returns></returns>
		public string Sign(string valueToSign, int timestamp)
		{
			return _keyPair.SignBase64(valueToSign + "?t=" + timestamp);
		}

		/// <summary>
		/// Validates a signature for a given signed value. The timestamp will be appended to the end as ?t={timestamp}.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="timestamp">The timestamp.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		public bool ValidateSignature(string signedValue, int timestamp, string signature)
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
