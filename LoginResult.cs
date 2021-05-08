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
		/// Its value is canvas compatible object.
		/// </summary>
		public object MoreDetailRequired;

		/// <summary>
		/// The name of a cookie to store the token in, if it's being held in a cookie.
		/// </summary>
		public string CookieName;

		/// <summary>
		/// The user that got authenticated. This can be null when sent to the client in the scenario of a partial (2FA) auth.
		/// </summary>
		public User User;

		/// <summary>
		/// The signed token. Usually set to a cookie.
		/// </summary>
		public bool Success;

		/// <summary>
		/// Original info given during login. Can be null if this is a server driven auth.
		/// </summary>
		public UserLogin LoginData;
	}
}
