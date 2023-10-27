using Api.Database;
using Newtonsoft.Json;
using System;

namespace Api.PasswordResetRequests
{
	/// <summary>
	/// A password reset request.
	/// </summary>
	public partial class PasswordResetRequest : Content<uint>
	{
		/// <summary>
		/// The randomly generated token, used by the client, to prove ownership of the 2nd channel.
		/// </summary>
		[DatabaseField(Length =40)]
		[JsonIgnore]
		public string Token;

		/// <summary>
		/// True if this token was used.
		/// </summary>
		public bool IsUsed;

		/// <summary>
		/// The email address that is being reset.
		/// </summary>
		public string Email;
		
		/// <summary>
		/// Created date UTC. This is used to establish if the token has expired yet.
		/// </summary>
		[JsonIgnore]
		public DateTime CreatedUtc;

		/// <summary>
		/// The user this reset request is for.
		/// </summary>
		[JsonIgnore]
		public uint UserId;

	}
}