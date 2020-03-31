using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Emails;
using Api.Contexts;
using Api.Uploader;
using Api.PasswordReset;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;

namespace Api.Users
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>

    [Route("v1/user")]
	public partial class UserController : AutoController<User>
    {
		private IContextService _contexts;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public UserController(
            IContextService contexts
		) : base()
        {
			_contexts = contexts;
		}
		
		/// <summary>
		/// Gets the currently logged in user, or null if there isn't one.
		/// </summary>
		/// <returns></returns>
		[HttpGet("self")]
		public async Task<User> Self()
		{
			var context = Request.GetContext();

			if (context == null || context.UserId == 0)
			{
				return null;
			}

			var user = await context.GetUser();
			return user;
		}
		
		/// <summary>
		/// GET /v1/user/2/profile
		/// Returns the profile user data for a single user.
		/// </summary>
		[HttpGet("{id}/profile")]
		public async Task<UserProfile> LoadProfile([FromRoute] int id)
		{
			var context = Request.GetContext();

			var result = await (_service as IUserService).GetProfile(context, id);

			if (result == null)
			{
				Response.StatusCode = 404;
				return null;
			}
			
			return result;
		}

		/// <summary>
		/// Logs out this user account.
		/// </summary>
		/// <returns></returns>
        [HttpGet("logout")]
        public Success Logout() {

			// var context = Request.GetContext();

			var expiry = DateTime.MinValue;

            Response.Cookies.Append(
                _contexts.CookieName,
                "",
                new Microsoft.AspNetCore.Http.CookieOptions()
                {
                    Path = "/",
                    Expires = expiry
                }
            );

            return new Success();
        }

		/// <summary>
		/// POST /v1/user/login/
		/// Attempts to login.
		/// </summary>
		[HttpPost("login")]
		public async Task<LoginResult> Login([FromBody] UserLogin body)
		{
			var context = Request.GetContext();

			var result = await (_service as IUserService).Authenticate(context, body);

			if (result != null && result.Token != null)
			{
				Response.Cookies.Append(
					result.CookieName,
					result.Token,
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = result.Expiry
					}
				);
			}
			else
			{
				Response.StatusCode = 400;
				return null;
			}

			#warning todo
			// Locale cookie too (Added by translation system).

			return result;
        }

    }

}
