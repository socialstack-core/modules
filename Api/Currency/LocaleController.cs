using System.Threading.Tasks;
using Api.Contexts;
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
		[HttpGet("set/currency/{id}")]
		public virtual async ValueTask SetCurrency([FromRoute] uint id)
		{
			var context = await Request.GetContext();
			
			// Set locale ID:
			context.CurrencyLocaleId = id;

			await OutputContext(context);
		}

    }

}
