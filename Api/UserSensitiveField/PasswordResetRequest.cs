using Api.Database;

namespace Api.PasswordResetRequests
{
	/// <summary>
	/// An extension of password reset request.
	/// </summary>
	public partial class PasswordResetRequest : Content<uint>
	{
        /// <summary>
		/// The randomly generated token, used by the client, to prove ownership and to allow the
        /// field changes when recovering the account without triggering the sensitive field change flow.
		/// </summary>
        public string EmailRecoveryKey;
    }
}
