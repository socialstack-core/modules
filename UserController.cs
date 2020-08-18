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
		/// Gets the current context.
		/// </summary>
		/// <returns></returns>
		[HttpGet("self")]
		public async Task<PublicContext> Self()
		{
			var context = Request.GetContext();

			if (context == null)
			{
				return null;
			}

			var ctx = await context.GetPublicContext();
			
			if(ctx != null && ctx.User != null && ctx.Role != null)
			{
				if(ctx.User.Role != ctx.Role.Id)
				{
					// Force reset if role changed.
					return null;
				}
			}
			
			return ctx;
		}
		
		/// <summary>
		/// Logs out this user account.
		/// </summary>
		/// <returns></returns>
        [HttpGet("logout")]
        public Success Logout() {

			// var context = Request.GetContext();

			var expiry = default(DateTimeOffset?);

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
		/// Attempts to login. Returns either a Context or a LoginResult.
		/// </summary>
		[HttpPost("login")]
		public async Task<object> Login([FromBody] UserLogin body)
		{
			var context = Request.GetContext();

			var result = await (_service as IUserService).Authenticate(context, body);

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

    }

}
