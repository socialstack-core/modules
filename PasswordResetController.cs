using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Emails;
using Api.Users;
using Api.Eventing;
using Api.Contexts;

namespace Api.PasswordReset
{
    /// <summary>
    /// Handles user account endpoints.
    /// </summary>

    [Route("v1/password/reset")]
	[ApiController]
	public partial class PasswordResetController : ControllerBase
    {
        private IUserService _users;
		private IPasswordResetService _passwordReset;


		/// <summary>
		/// Instanced automatically.
		/// </summary>
		public PasswordResetController(
			IUserService users,
			IPasswordResetService passwordReset
		)
        {
			_users = users;
			_passwordReset = passwordReset;

		}

		/// <summary>
		/// POST /v1/password/reset/
		/// Forgot password. Post the email address to generate a recovery email.
		/// </summary>
		[HttpPost]
		public async Task<object> Create([FromBody] UserPasswordForgot body)
        {
			var context = Request.GetContext();
			// Get the user account:
			var user = await _users.Get(context, body.Email);

			if (user == null)
			{
				Response.StatusCode = 404;
				return null;
			}

			body = await Events.PasswordResetRequestReset.Dispatch(context, body, Response, user);

			if (body == null)
			{
				// A handler rejected this request.
				return null;
			}
			
			// Send the password reset request:
			if (!await _passwordReset.Create(user.Id, user.Email))
			{
				Response.StatusCode = 404;
				return null;
			}

            return new {
            };
        }

    }

}
