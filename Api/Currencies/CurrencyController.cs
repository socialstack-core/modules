using Api.Contexts;
using Api.Startup;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Currencies
{
    /// <summary>Handles currency endpoints.</summary>
    [Route("v1/currency")]
	public partial class CurrencyController : AutoController<Currency>
    {
		/// <summary>
		/// Manually triggers an exchange rate refresh (admin only).
		/// </summary>
		[HttpGet("refresh")]
		public async ValueTask Refresh(Context context)
		{
			if (context.Role == null || !context.Role.CanViewAdmin)
			{
				throw new PublicException("Admin only", "currency/admin_required");
			}

			var service = Services.Get<CurrencyService>();
			await service.UpdateExchangeRates(context);
		}
	}
}