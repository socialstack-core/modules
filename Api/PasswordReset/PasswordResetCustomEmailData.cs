using Api.Database;
using Newtonsoft.Json;
using System;

namespace Api.PasswordResetRequests
{
	/// <summary>
	/// A password reset request.
	/// </summary>
	public class PasswordResetCustomEmailData
	{
		/// <summary>
		/// The randomly generated token, used by the client, to prove ownership of the 2nd channel.
		/// </summary>
		public string Token;
		
		/// <summary>
		/// The complete request.
		/// </summary>
		public PasswordResetRequest Reset;
	}
}