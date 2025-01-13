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
				// Send context only - don't change the cookie:
				await OutputContext(context);
			}
			else
			{
				// Clear user:
				context.User = null;
				
				// Regular empty cookie:
				Response.Cookies.Append(
					_contexts.CookieName,
					"",
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Domain = _contexts.GetDomain(context.LocaleId),
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
				
				// Send a new context:
				var newContext = new Context();
				
				newContext.LocaleId = context.LocaleId;
				
				await OutputContext(newContext);
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
		/// Impersonate a user by their ID. This is a hard cookie switch. You will loose all admin functionality to make the impersonation as accurate as possible.
		/// </summary>
		[HttpGet("{id}/impersonate")]
		public async ValueTask Impersonate([FromRoute] uint id)
		{
			// Firstly, are they an admin?
			var context = await Request.GetContext();

			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Unavailable", "no_access");
			}

			// Next, is this an elevation? Currently a simple role ID based check. 
			// You can't impersonate someone of a role "higher" than yours (or a user that you can't load).
			var targetUser = await _service.Get(context, id);

			if (targetUser == null || targetUser.Role < context.Role.Id)
			{
				throw new PublicException("Cannot elevate to a higher role", "elevation_required");
			}

			var _loginTokens = Services.Get<ContextService>();

			var cookie = Request.Cookies[_loginTokens.CookieName];
			var impCookie = Request.Cookies[_loginTokens.ImpersonationCookieName];

			// If we were already impersonating, don't overwrite the existing impersonation cookie.
			if (impCookie == null || impCookie.Length == 0)
			{
				// Set impersonation backup cookie:
				var expiry = DateTime.UtcNow.AddDays(120);

				Response.Cookies.Append(
					_loginTokens.ImpersonationCookieName,
					cookie,
					new Microsoft.AspNetCore.Http.CookieOptions()
					{
						Path = "/",
						Expires = expiry,
						Domain = _loginTokens.GetDomain(context.LocaleId),
						IsEssential = true,
						HttpOnly = true,
						Secure = true,
						SameSite = SameSiteMode.Lax
					}
				);
			}

			// Update the context to the new user:
			context.User = targetUser;

			await OutputContext(context);
		}

		/// <summary>
		/// Reverses an impersonation.
		/// </summary>
		[HttpGet("unpersonate")]
		public async ValueTask Unpersonate()
		{
			var _loginTokens = Services.Get<ContextService>();
			
			var impCookie = Request.Cookies[_loginTokens.ImpersonationCookieName];

			if (impCookie == null || impCookie.Length == 0)
			{
				return;
			}
			
			var context = await _loginTokens.Get(impCookie);

			// Remove the impersonation cookie:
			Response.Cookies.Append(
				_loginTokens.ImpersonationCookieName,
				"",
				new Microsoft.AspNetCore.Http.CookieOptions()
				{
					Path = "/",
					Expires = ThePast,
					Domain = _loginTokens.GetDomain(context.LocaleId),
					IsEssential = true,
					HttpOnly = true,
					Secure = true,
					SameSite = SameSiteMode.Lax
				}
			);

			// Note that this will also generate a new token:
			await OutputContext(context);
		}

    }
}
