using Api.Contexts;
using Api.Startup;
using Api.Translate;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Currency
{
    /// <summary>Handles exchangeRate endpoints.</summary>
    [Route("v1/exchangeRate")]
	public partial class ExchangeRateController : AutoController<ExchangeRate>
    {

	}
}