using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Api.Translate;
using Api.Eventing;
using System;

namespace Api.Contexts
{
    /// <summary>
    /// Extensions to enable custom cookie based authentication.
    /// </summary>
    public static class UserAuthenticationExtensions
	{
		private static ContextService _loginTokens;
		private static LocaleService _locales;

		/// <summary>
		/// Gets the basic context. Does not authenticate the user at all: this just gets the initiated context.
		/// This allows the getting of a context and then authenticating it separately, if authenticating it is actually required.
		/// </summary>
		/// <returns></returns>
		public static async ValueTask<Context> GetBasicContext(this Microsoft.AspNetCore.Http.HttpRequest request)
		{
			var context = new Context();

			if (_locales == null)
			{
				_locales = Api.Startup.Services.Get<LocaleService>();
			}

			// Handle locale next. The cookie comes lower precedence to the Locale header.
			var localeCookie = request.Cookies[_locales.CookieName];

			StringValues localeIds;

			// Could also handle Accept-Language here. For now we use a custom header called Locale (an ID).
			if (request.Headers.TryGetValue("Locale", out localeIds) && !string.IsNullOrEmpty(localeIds))
			{
				// Locale header is set - use it instead:
				localeCookie = localeIds.FirstOrDefault();
			}

			if (localeCookie != null && uint.TryParse(localeCookie, out uint localeId))
			{
				// Set in the ctx:
				context.LocaleId = localeId;
			}

			await Events.Context.OnInitiate.Dispatch(context, request);

			return context;
		}

		/// <summary>
		/// Gets the user ID for the currently authenticated user. It's 0 if they're not logged in.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public static async ValueTask<Context> GetContext(this Microsoft.AspNetCore.Http.HttpRequest request, Context context = null)
		{
			if (context == null)
			{
				context = await GetBasicContext(request);
			}

			if (_loginTokens == null)
			{
				_loginTokens = Api.Startup.Services.Get<ContextService>();
			}
			
			if(_loginTokens == null || _locales == null)
			{
				return context;
			}
			
			var cookie = request.Cookies[_loginTokens.CookieName];

			if (string.IsNullOrEmpty(cookie))
			{
				StringValues tokenStr;
				if (!request.Headers.TryGetValue("Token", out tokenStr) || string.IsNullOrEmpty(tokenStr))
				{
					cookie = null;
				}
				else
				{
					cookie = tokenStr.FirstOrDefault();
				}
			}

			if (cookie == null || !await _loginTokens.Get(cookie, context))
			{
				// Anon context - trigger setting it up:
				await Events.ContextAfterAnonymous.Dispatch(context, context, request);
			}

			// Do we have an impersonation cookie too?
			var impCookie = request.Cookies[_loginTokens.ImpersonationCookieName];

			if (!string.IsNullOrEmpty(impCookie))
			{
				// There's an impersonation cookie. This situation is pretty rare,
				// so the overhead of allocating a context only to throw it out shortly is really not important.
				// Go ahead and load up that real context then:
				var realContext = new Context();
				if (await _loginTokens.Get(impCookie, realContext))
				{
					// It was legit
					context.RealUserId = realContext.UserId;
				}
			}

			// Context fully loaded:
			await Events.Context.OnLoad.Dispatch(context, request);

			return context;
		}

	}
	
}