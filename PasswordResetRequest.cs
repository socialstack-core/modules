using Api.Database;
using System;

namespace Api.PasswordReset
{
	/// <summary>
	/// A password reset request.
	/// </summary>
	public class PasswordResetRequest : DatabaseRow
	{

		/// <summary>
		/// The randomly generated token, used by the client, to prove ownership of the 2nd channel.
		/// </summary>
		[DatabaseField(Length =40)]
		public string Token;

		/// <summary>
		/// Expiry date, UTC.
		/// </summary>
		public DateTime ExpiryUtc;

		/// <summary>
		/// The user this reset request is for.
		/// </summary>
		public int UserId;

	}
}