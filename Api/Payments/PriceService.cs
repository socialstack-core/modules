using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Startup;

namespace Api.Payments
{
	/// <summary>
	/// Handles prices.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class PriceService : AutoService<Price>
	{
		private PriceServiceConfig _config;
		private Dictionary<string, TaxCalculator> _taxCalculators;

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public PriceService() : base(Events.Price)
        {
			InstallAdminPages(null, null, ["id", "amount", "currencyCode"]);

			_config = GetConfig<PriceServiceConfig>();

			UpdateTaxConfig();

			_config.OnChange += () => {
				UpdateTaxConfig();
				return new ValueTask();
			};

#if !DEBUG
			Cache();
#endif
		}

		/// <summary>
		/// Gets a tax calculator by jurisdiction key.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="jurisdiction"></param>
		/// <returns></returns>
		public async ValueTask<TaxCalculator> GetTaxCalculator(Context context, string jurisdiction)
		{
			var calcs = _taxCalculators;

			if (calcs == null)
			{
				// Tax calc is not active.
				return null;
			}

			if (string.IsNullOrEmpty(jurisdiction))
			{
				// Get the locale (from cache, completes instantly):
				var locale = await context.GetLocale();

				jurisdiction = locale.DefaultTaxJurisdiction;

				if (string.IsNullOrEmpty(jurisdiction))
				{
					throw new PublicException("Tax calculation is active but no jurisdiction is configured.", "tax/not_configured");
				}
			}

			if (!calcs.TryGetValue(jurisdiction, out TaxCalculator calc))
			{
				throw new PublicException("Tax calculation is active but jurisdiction is not configured '" + jurisdiction + "'.", "tax/jurisdiction_notfound");
			}

			calc = await Events.Price.ResolveTaxCalculator.Dispatch(context, calc, jurisdiction);

			return calc;
		}

		private void UpdateTaxConfig()
		{
			var taxConfig = _config.Tax;

			if (taxConfig == null)
			{
				_taxCalculators = null;
				return;
			}

			var taxCalcs = new Dictionary<string, TaxCalculator>();

			foreach (var kvp in taxConfig)
			{
				taxCalcs[kvp.Key] = new TaxCalculator(kvp.Value);
			}

			_taxCalculators = taxCalcs;
		}

	}

}
