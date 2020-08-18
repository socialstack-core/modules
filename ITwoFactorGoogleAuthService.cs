namespace Api.TwoFactorGoogleAuth
{
	/// <summary>
	/// Google authenticator based 2FA.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial interface ITwoFactorGoogleAuthService
	{

		/// <summary>
		/// Checks if the given pin is acceptable for the given secret.
		/// </summary>
		/// <param name="secret"></param>
		/// <param name="pin"></param>
		/// <returns></returns>
		bool Validate(byte[] secret, string pin);

		/// <summary>
		/// Generates a new secret key.
		/// </summary>
		/// <returns></returns>
		byte[] GenerateKey();

		/// <summary>
		/// Generates an image for the given key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		byte[] GenerateProvisioningImage(byte[] key);
	}
}
