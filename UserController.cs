using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.Startup;
using Api.PasswordResetRequests;

namespace Api.Users
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>

    [Route("v1/user")]
	public partial class UserController : AutoController<User>
    {
		private ContextService _contexts;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public UserController(
            ContextService contexts
		) : base()
        {
			_contexts = contexts;
		}
		
		/// <summary>
		/// Gets the current context.
		/// </summary>
		/// <returns></returns>
		[HttpGet("self")]
		public async ValueTask Self()
		{
			var context = await Request.GetContext();
			await context.RoleCheck(Request, Response);
			await OutputContext(context);
		}

		/// <summary>
		/// A date in the past used to set expiry on cookies.
		/// </summary>
		private static DateTimeOffset ThePast = new DateTimeOffset(1993, 1, 1, 0, 0, 0, TimeSpan.Zero);

		/// <summary>
		/// Logs out this user account.
		/// </summary>
		/// <returns></returns>
        [HttpGet("logout")]
        public async ValueTask Logout() {
			var context = await Request.GetContext();

			var result = await ((UserEventGroup)(_service.EventGroup)).Logout.Dispatch(context, new LogoutResult());

			if (result.SendContext)
			{
				// Send context:
				await OutputContext(context);
			}
			else
			{
				// Regular empty cookie:
				Response.Cookies.Append(
					_contexts.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Domain = _contexts.GetDomain(),
						IsEssential = true,
						Expires = ThePast
					}
				);

				Response.Cookies.Append(
					_contexts.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = ThePast
					}
				);
			}
        }

		/// <summary>
		/// Json serialization settings for canvases
		/// </summary>
		private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			},
			Formatting = Formatting.None
		};

		/// <summary>
		/// POST /v1/user/login/
		/// Attempts to login. Returns either a Context or a LoginResult.
		/// </summary>
		[HttpPost("login")]
		public async ValueTask Login([FromBody] UserLogin body)
		{
			var context = await Request.GetContext();

			var result = await (_service as UserService).Authenticate(context, body);

			if (result == null)
			{
				throw new PublicException("Incorrect user details. Either the account does not exist or the attempt was unsuccessful.", "user_not_found");
			}

			if (!result.Success)
			{
				// Output the result message. 
				// Fail message does not expose any content objects but does contain nested objects, so newtonsoft is ok here.
				var json = JsonConvert.SerializeObject(result, jsonSettings);
				var bytes = System.Text.Encoding.UTF8.GetBytes(json);
				await Response.Body.WriteAsync(bytes, 0, bytes.Length);
				return;
			}

			// output the context:
			await OutputContext(context);
        }

		/// <summary>
		/// POST /v1/user/sendverifyemail/
		/// Sends the user a new token to verify their email.
		/// </summary>
		[HttpPost("sendverifyemail")]
		public async ValueTask ResendVerificationEmail([FromBody] UserPasswordForgot body)
		{
			var context = await Request.GetContext();

			var user = await (_service as UserService).Where("Email=?", DataOptions.IgnorePermissions).Bind(body.Email).Last(context);

			if (user == null)
			{
				throw new PublicException("Incorrect user email. Either the account does not exist or the attempt was unsuccessful.", "user_not_found");
			}

			var result = await (_service as UserService).SendVerificationEmail(context, user);

			if (string.IsNullOrEmpty(result))
			{
				throw new PublicException("The attempt was unsuccessful, please try again laetr", "user_verify_failed");
			}

			// output the context:
			await OutputContext(context);
        }

		/// <summary>
		/// POST /v1/user/verify/{userid}/{token}
		/// Attempts to verify the users email. If a password is supplied, the users password is also set.
		/// </summary>
		[HttpPost("verify/{userid}/{token}")]
		public async ValueTask VerifyUser(uint userid, string token, [FromBody] NewPassword newPassword)
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
}
