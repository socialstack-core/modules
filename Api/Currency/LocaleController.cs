using System;
using System.Threading.Tasks;
using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;


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

			if (context.RoleId == 6)
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
				context.SendToken(Response);
			}
		}

	}

}
