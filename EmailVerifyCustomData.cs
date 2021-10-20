using Api.Database;
using Newtonsoft.Json;
using System;

namespace Api.Users
{
	/// <summary>
	/// Email verification custom data.
	/// </summary>
	public class EmailVerifyCustomData
	{
		/// <summary>
		/// The secret token which is included in the button's URL.
		/// </summary>
		public string Token;

		/// <summary>
		/// The userId which is included in the button's URL.
		/// </summary>
		public uint UserId;
	}
}