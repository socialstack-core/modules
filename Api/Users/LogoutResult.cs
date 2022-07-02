namespace Api.Users
{
	/// <summary>
	/// Provides the ability to perform custom logout actions.
	/// </summary>
	public partial struct LogoutResult
	{
		/// <summary>
		/// True if the a regular context output should happen instead of clearing the cookie(s).
		/// </summary>
		public bool SendContext;
	}
}