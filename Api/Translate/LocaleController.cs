using System.Threading.Tasks;
using Api.Contexts;
using Microsoft.AspNetCore.Mvc;


namespace Api.Translate
{
    /// <summary>
    /// Handles locale endpoints.
    /// </summary>

    [Route("v1/locale")]
	public partial class LocaleController : AutoController<Locale>
	{
		
		/// <summary>
		/// GET /v1/locale/set/2/
		/// Sets locale by its ID.
		/// </summary>
		[HttpGet("set/{id}")]
		public virtual async ValueTask Set([FromRoute] uint id)
		{
			var context = await Request.GetContext();
			
			// Set locale ID:
			context.LocaleId = id;
			context.CurrencyLocaleId = id;

			await OutputContext(context);
		}

    }

}
