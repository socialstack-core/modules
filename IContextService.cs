namespace Api.Contexts
{
	/// <summary>
	/// The context service. This is signature based - it doesn't generate any database traffic.
	/// Used to establish primary user context - role, locale and the user ID - when possible.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public interface IContextService
	{
		/// <summary>
		/// The name of the cookie in use.
		/// </summary>
		string CookieName { get; }

		/// <summary>
		/// Gets a context from the given token text.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		Context Get(string token);

		/// <summary>
		/// Revokes all previous login tokens for the given user.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="loginRevokeCount"></param>
		void Revoke(int userId, int loginRevokeCount);

		/// <summary>
		/// Creates a signed token for the given user.
		/// </summary>
		/// <returns></returns>
		string CreateToken(Context context);

	}
}
