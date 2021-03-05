using System;
using System.Threading.Tasks;
using Api.Permissions;
using Microsoft.AspNetCore.Mvc;
using Api.Database;
using Api.Emails;
using Api.Contexts;
using Api.Uploader;
using Api.Results;
using Api.Eventing;
using Newtonsoft.Json.Linq;
using Api.AutoForms;
using Api.Startup;

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
		public async Task<PublicContext> Self()
		{
			var context = Request.GetContext();

			if (context == null)
			{
				// Should never happen but just in case.
				return null;
			}
			
			var cookieRole = context.RoleId;

			if (context.UserId == 0 && context.CookieState != 0)
			{
				// Anonymous - fire off the anon user event:
				context = await Events.ContextAfterAnonymous.Dispatch(context, context, Request);

				if (context == null)
				{
					return null;
				}

				// Update cookie role:
				cookieRole = context.RoleId;
			}

			var ctx = await context.GetPublicContext();

			if (context.RoleId != cookieRole)
			{
				// Force reset if role changed. Getting the public context will verify that the roles match.

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

				return null;
			}
			else
			{
				// Update the token:
				context.SendToken(Response);
			}
			
			return ctx;
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
        public Success Logout() {

			// var context = Request.GetContext();

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

            return new Success();
        }

		/// <summary>
		/// POST /v1/user/login/
		/// Attempts to login. Returns either a Context or a LoginResult.
		/// </summary>
		[HttpPost("login")]
		public async Task<object> Login([FromBody] UserLogin body)
		{
			try{
				var context = Request.GetContext();

				var result = await (_service as UserService).Authenticate(context, body);

				if (result == null)
				{
					Response.StatusCode = 400;
					return null;
				}

				if (result.Success)
				{
					// Regenerate the contextual token:
					context.SendToken(Response);

					return await context.GetPublicContext();
				}
				
				return result;
			}
			catch(PublicException e)
			{
				return e.Apply(Response);
			}
        }

    }

}
