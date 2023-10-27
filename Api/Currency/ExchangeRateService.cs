using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;

namespace Api.Currency
{
	/// <summary>
	/// Handles exchangeRates.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class ExchangeRateService : AutoService<ExchangeRate>
    {
		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public ExchangeRateService() : base(Events.ExchangeRate)
        {
			InstallAdminPages("ExchangeRates", "fa:fa-rocket", new string[] { "id", "name", "rate", "fromLocaleId", "toLocaleId" });

			Cache();
		}
	}
    
}
