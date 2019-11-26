using Newtonsoft.Json;
using System;

namespace Api.Users
{
    /// <summary>
    /// Response from the login process. Gets serialised and sent to the end user.
    /// </summary>
    public partial class LoginResult
    {
		/// <summary>
		/// A login happened, but more information is required. Used by 2FA and similar handlers.
		/// Its value is whatever that handler would like to convey to the frontend to e.g. trigger a 2FA UI.
		/// If you need more than a string, extend LoginResult with anything else you'd like.
		/// </summary>
		public string MoreDetailRequired;

		/// <summary>
		/// The name of a cookie to store the token in, if it's being held in a cookie.
		/// </summary>
		public string CookieName;

		/// <summary>
		/// The user that got authenticated. This can be null when sent to the client in the scenario of a partial (2FA) auth.
		/// </summary>
		public User User;

		/// <summary>
		/// The expiry, if any, of this token.
		/// </summary>
		public DateTime Expiry;

		/// <summary>
		/// The signed token. Usually set to a cookie.
		/// </summary>
		public string Token;
	}
}
