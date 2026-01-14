using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Api.Contexts;
using Api.Eventing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Api.Startup;
using Microsoft.AspNetCore.Http;
using Azure;
using LetsEncrypt.Client.Json;

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
		public Context Self(Context context)
		{
			return context;
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
        public async ValueTask<Context> Logout(HttpContext httpContext, Context context) {
			var response = httpContext.Response;

			var result = await ((UserEventGroup)(_service.EventGroup)).Logout.Dispatch(context, new LogoutResult());

			if (result.SendContext)
			{
				// Send context only - don't change the cookie:
				return context;
			}

			// Clear user:
			context.User = null;
				
			// Regular empty cookie:
			response.Cookies.Append(
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

			response.Cookies.Append(
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
			return newContext;
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
		[Returns(typeof(object))]
		public async ValueTask Login(HttpContext httpContext, Context context, [FromBody] UserLogin body)
		{
			var result = await (_service as UserService).Authenticate(context, body);
			var response = httpContext.Response;

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
				await httpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
				return;
			}

			// output the context:
			await OutputContext(httpContext, context);
		}

		/// <summary>
		/// Impersonate a user by their ID. This is a hard cookie switch. You will loose all admin functionality to make the impersonation as accurate as possible.
		/// </summary>
		[HttpGet("{id}/impersonate")]
		public async ValueTask<Context> Impersonate(HttpContext httpContext, Context context, [FromRoute] uint id)
		{
			var request = httpContext.Request;
			var response = httpContext.Response;

			// Firstly, are they an admin?

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

			var cookie = request.Cookies[_loginTokens.CookieName];
			var impCookie = request.Cookies[_loginTokens.ImpersonationCookieName];

			// If we were already impersonating, don't overwrite the existing impersonation cookie.
			if (impCookie == null || impCookie.Length == 0)
			{
				// Set impersonation backup cookie:
				var expiry = DateTime.UtcNow.AddDays(120);

				response.Cookies.Append(
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
			return context;
		}

		/// <summary>
		/// Reverses an impersonation.
		/// </summary>
		[HttpGet("unpersonate")]
		public async ValueTask<Context> Unpersonate(HttpContext httpContext)
		{
			var request = httpContext.Request;
			var response = httpContext.Response;

			var _loginTokens = Services.Get<ContextService>();
			
			var impCookie = request.Cookies[_loginTokens.ImpersonationCookieName];

			if (impCookie == null || impCookie.Length == 0)
			{
				return null;
			}

			var context = new Context();
			await _loginTokens.Get(impCookie, context);

			// Remove the impersonation cookie:
			response.Cookies.Append(
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
			return context;
		}

    }

}
