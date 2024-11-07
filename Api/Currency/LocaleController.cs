using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Api.Translate
{
    /// <summary>
    /// Handles locale endpoints.
    /// </summary>
	public partial class LocaleController
	{

		/// <summary>
		/// GET /v1/locale/set/currency/2/
		/// Sets currency locale by its ID.
		/// </summary>
		[HttpGet("set/currency/{currency}/{locale}")]
		public virtual async ValueTask SetCurrency([FromRoute] uint currency, [FromRoute] uint locale)
		{
			var context = await Request.GetContext();

			// Set locale ID:
			context.LocaleId = locale;
			context.CurrencyLocaleId = currency;

			if (context.RoleId == 6 && currency == locale)
			{
				ContextService _contexts = Services.Get<ContextService>();

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
			}
			else
            {
				await OutputContext(context);
			}
		}

		/// <summary>
		/// A date in the past used to set expiry on cookies.
		/// </summary>
		private static DateTimeOffset ThePast = new DateTimeOffset(1993, 1, 1, 0, 0, 0, TimeSpan.Zero);

		/// <summary>
		/// GET /v1/locale/set/currency/2/
		/// Sets currency locale by its ID.
		/// </summary>
		[HttpGet("reset/{locale}")]
		public virtual async ValueTask ResetLocale([FromRoute] uint locale)
		{
			var context = await Request.GetContext();

			// Set locale ID:
			context.LocaleId = locale;
			context.CurrencyLocaleId = locale;

			if (context.User == null)
			{
				ContextService _contexts = Services.Get<ContextService>();

				// What is the default locale for this request?
				// If it matches the requested one, delete the cookies.
				// Otherwise, send the token.
				uint defaultLocaleId = 1;

				StringValues ipCountry;

				if (Request.Headers.TryGetValue("CF-IPCountry", out ipCountry) && !string.IsNullOrEmpty(ipCountry))
				{
					var ipCountryHeader = ipCountry.FirstOrDefault();
					ipCountryHeader = ipCountryHeader.ToLower();

					// TODO: allow mapping between 2-char country codes to full locale code (e.g. US -> en-US)
					if (ipCountryHeader == "us")
					{
						defaultLocaleId = 2;
					}
				}

				if (locale == defaultLocaleId)
				{
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

				}
				else
				{
					context.SendToken(Response);
				}

			}
			else
            {
				context.SendToken(Response);
			}
		}

	}

}
