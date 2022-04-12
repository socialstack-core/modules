using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.Startup;
using Microsoft.AspNetCore.Http;


namespace Api.Users
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>
	public partial class UserController
    {
		/// <summary>
		/// POST /v1/user/sendverifyemail/
		/// Sends the user a new token to verify their email.
		/// </summary>
		[HttpPost("sendverifyemail")]
		public async ValueTask ResendVerificationEmail([FromBody] UserPasswordForgot body)
		{
			var context = await Request.GetContext();

			var user = context.User;

			if (!string.IsNullOrEmpty(body.Email))
            {
				user = await (_service as UserService).Where("Email=?", DataOptions.IgnorePermissions).Bind(body.Email == null ? null : body.Email.Trim()).Last(context);
			}

			if (user == null)
			{
				throw new PublicException("Incorrect user email. Either the account does not exist or the attempt was unsuccessful.", "user_not_found");
			}

			var result = await (_service as UserService).SendVerificationEmail(context, user);

			if (string.IsNullOrEmpty(result))
			{
				throw new PublicException("The attempt was unsuccessful, please try again later", "user_verify_failed");
			}

			// output the context:
			await OutputContext(context);
        }

		/// <summary>
		/// POST /v1/user/verify/{userid}/{token}
		/// Attempts to verify the users email. If a password is supplied, the users password is also set.
		/// </summary>
		[HttpPost("verify/{userid}/{token}")]
		public async ValueTask VerifyUser(uint userid, string token, [FromBody] OptionalPassword newPassword)
		{
			var context = await Request.GetContext();

			var user = await (_service as UserService).Where("Id=?", DataOptions.IgnorePermissions).Bind(userid).Last(context);

			if (user == null)
			{
				throw new PublicException("Incorrect user. Either the account does not exist or the attempt was unsuccessful.", "user_not_found");
			}

			var isToeknValid = !string.IsNullOrEmpty(user.EmailVerifyToken) && user.EmailVerifyToken.ToUpper() == token.ToUpper();

			if (!isToeknValid)
			{
				throw new PublicException("Incorrect token. Either the token is invalid or the attempt was unsuccessful.", "invalid_token");
			}

			var result = await (_service as UserService).VerifyEmail(context, user, newPassword?.Password);

			context.User = result;

			// output the context:
			await OutputContext(context);
		}
	}

	/// <summary>
	/// Used when setting a password during user verification.
	/// </summary>
	public class OptionalPassword
	{
		/// <summary>
		/// The new password.
		/// </summary>
		public string Password;
	}
}