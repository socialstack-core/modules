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


namespace Api.Contexts
{
    /// <summary>
    /// Extensions to enable custom cookie based authentication.
    /// </summary>
    public static class UserAuthenticationExtensions
	{
		private static IContextService _loginTokens;

		/// <summary>
		/// Adds the middleware which enables identifying the user on each API request.
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static IServiceCollection AddUserAuthentication(this IServiceCollection services)
        {
            return services;
        }

		/// <summary>
		/// Gets the user ID for the currently authenticated user. It's 0 if they're not logged in.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static int GetUserId(this Microsoft.AspNetCore.Http.HttpRequest request)
		{
			var token = request.HttpContext.User as Context;

			if (token == null)
			{
				return 0;
			}

			return token.UserId;
		}

		/// <summary>
		/// Gets the user ID for the currently authenticated user. It's 0 if they're not logged in.
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static Context GetContext(this Microsoft.AspNetCore.Http.HttpRequest request)
		{
			if (_loginTokens == null)
			{
				_loginTokens = Api.Startup.Services.Get<IContextService>();
			}
			
			var cookie = request.Cookies[_loginTokens.CookieName];

			if (string.IsNullOrEmpty(cookie))
			{
				StringValues tokenStr;
				if (!request.Headers.TryGetValue("Token", out tokenStr) || string.IsNullOrEmpty(tokenStr))
				{
					return new Context();
				}
				else
				{
					cookie = tokenStr.FirstOrDefault();
				}
			}

			var context = _loginTokens.Get(cookie);

			if (context == null)
			{
				return new Context();
			}
			
			return context;
		}

	}
	
}