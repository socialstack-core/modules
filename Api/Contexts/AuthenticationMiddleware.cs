using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Api.Translate;
using Api.Signatures;
using Api.Eventing;

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
		/// Gets the user ID for the currently authenticated user. It's 0 if they're not logged in.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="keyPair">Optionaly keypair to use to check the HMAC.</param>
		/// <returns></returns>
		public static async ValueTask<Context> GetContext(this Microsoft.AspNetCore.Http.HttpRequest request, KeyPair keyPair = null)
		{
			if (_loginTokens == null)
			{
				_loginTokens = Api.Startup.Services.Get<ContextService>();
			}
			
			if (_locales == null)
			{
				_locales = Api.Startup.Services.Get<LocaleService>();
			}
			
			// If still null, site is very early in the startup process.
			if(_loginTokens == null || _locales == null)
			{
				return new Context();
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

			var context = cookie == null ? null : await _loginTokens.Get(cookie, keyPair);

			if (context == null)
			{
				context = new Context() { };

				// Anon context - trigger setting it up:
				await Events.ContextAfterAnonymous.Dispatch(context, context, request);
			}

			// Handle locale next. The cookie comes lower precedence to the Locale header.
			cookie = request.Cookies[_locales.CookieName];
			
			StringValues localeIds;

			// Could also handle Accept-Language here. For now we use a custom header called Locale (an ID).
			if (request.Headers.TryGetValue("Locale", out localeIds) && !string.IsNullOrEmpty(localeIds))
			{
				// Locale header is set - use it instead:
				cookie = localeIds.FirstOrDefault();
			}

			if (cookie != null && uint.TryParse(cookie, out uint localeId))
			{
				// Set in the ctx:
				context.LocaleId = localeId;
			}

			await Events.Context.OnLoad.Dispatch(context, request);

			return context;
		}

	}
	
}