using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;


namespace Api.Signatures
{
	/// <summary>
	/// Handles generation and validation of signatures used for e.g. serving of private files.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ISignatureService
	{
		/// <summary>
		/// Validates a signature for a given signed value. The timestamp will be appended to the end as ?t={timestamp}.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="timestamp">The timestamp.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		bool ValidateSignature(string signedValue, int timestamp, string signature);

		/// <summary>
		/// Generates a signature for the given piece of text. The timestamp will be appended to the end via ?t={timestamp}.
		/// </summary>
		/// <param name="valueToSign"></param>
		/// <param name="timestamp"></param>
		/// <returns></returns>
		string Sign(string valueToSign, int timestamp);

		/// <summary>
		/// Validates a signature for a given signed value as-is.
		/// </summary>
		/// <param name="signedValue">The value - usually the URL itself - being signed.</param>
		/// <param name="signature"></param>
		/// <returns>True if the signature is valid.</returns>
		bool ValidateSignature(string signedValue, string signature);

		/// <summary>
		/// Generates a signature for the given piece of text as-is.
		/// </summary>
		/// <param name="valueToSign"></param>
		/// <returns></returns>
		string Sign(string valueToSign);
	}
}
