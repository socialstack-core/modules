using Api.Database;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Api.Automations;
using Api.Configuration;
using Api.Startup;

namespace Api.Currencies
{
	/// <summary>
	/// Handles currencies.
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
	public partial class CurrencyService : AutoService<Currency>
    {
		private static readonly HttpClient _http = new HttpClient();
		private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};
		private readonly ConfigurationService _config;

		/// <summary>
		/// Cache of currency code to exchange rate (relative to the primary currency).
		/// Cleared whenever a currency is created, updated or deleted.
		/// </summary>
		private readonly ConcurrentDictionary<string, double> _rateByCode = new ConcurrentDictionary<string, double>();

		/// <summary>
		/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
		/// </summary>
		public CurrencyService(ConfigurationService config) : base(Events.Currency)
        {
			_config = config;
			InstallAdminPages("Currencies", "fa:fa-money", new string[] { "id", "name" });
			
			Cache();

			// Currency rates can change at any point, so drop the rate cache whenever one is modified.
			Events.Currency.AfterCreate.AddEventListener(async (Context context, Currency currency) =>
			{
				_rateByCode.Clear();
				return currency;
			});
			Events.Currency.AfterUpdate.AddEventListener(async (Context context, Currency currency, ChangedFields diff) =>
			{
				_rateByCode.Clear();
				return currency;
			});
			Events.Currency.AfterDelete.AddEventListener(async (Context context, Currency currency) =>
			{
				_rateByCode.Clear();
				return currency;
			});

			Events.Automation("exchange-rates", "0 0 6 ? * * *", false, "Fetch daily GBP/USD exchange rates")
				.AddEventListener(async (Context context, AutomationRunInfo runInfo) =>
				{
					await UpdateExchangeRates(context);
					return runInfo;
				});

			Events.OnFrontendConfigBuild.AddEventListener(async (Context context, StringBuilder sb) =>
			{
				var ratesJson = await GetRatesJson(context);

				if (sb.Length != 1)
				{
					sb.Append(',');
				}

				sb.Append("\"currencies\":");
				sb.Append(ratesJson);
				return sb;
			});
		}

		/// <summary>
		/// Gets the primary currency (Id #1).
		/// </summary>
		public async ValueTask<Currency> GetPrimary(Context context)
		{
			return await Get(context, 1, DataOptions.IgnorePermissions);
		}

		/// <summary>
		/// Gets the exchange rate multiplier for the given currency code (relative to the
		/// primary currency, e.g. "USD" -> 1.35). Returns 1 for unknown or unset codes.
		/// Results are cached and cleared whenever any currency changes.
		/// </summary>
		public async ValueTask<double> GetRateForCodeOrDefault(Context context, string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return 1;
			}

			code = code.Trim().ToUpper();

			if (_rateByCode.TryGetValue(code, out var cached))
			{
				return cached;
			}

			var all = await Where("", DataOptions.IgnorePermissions).ListAll(context);

			foreach (var currency in all)
			{
				var key = (currency.Code ?? "").Trim().ToUpper();
				if (key.Length == 0)
				{
					continue;
				}
				_rateByCode[key] = currency.Id == 1 ? 1.0 : currency.ExchangeRateForPrimary;
			}

			return _rateByCode.TryGetValue(code, out var rate) ? rate : 1;
		}

		/// <summary>
		/// Returns a JSON string mapping each currency code to its exchange rate
		/// relative to the primary (e.g. {"GBP":1,"USD":1.35}).
		/// The result is cached and invalidated after each successful rate update.
		/// </summary>
		public async ValueTask<string> GetRatesJson(Context context)
		{
			var all = await Where("", DataOptions.IgnorePermissions).ListAll(context);
			var rates = new Dictionary<string, object>(all.Count);

			foreach (var currency in all)
			{
				rates[currency.Code.Trim().ToUpper()] = new Dictionary<string, object>
				{
					{ "rate", currency.Id == 1 ? 1.0 : currency.ExchangeRateForPrimary },
					{ "symbol", currency.Symbol ?? "" }
				};
			}

			return JsonSerializer.Serialize(rates);
		}

		/// <summary>
		/// Fetches live exchange rates from the primary currency and updates all other currencies.
		/// </summary>
		public async ValueTask UpdateExchangeRates(Context context)
		{
			try
			{
				var primary = await GetPrimary(context);
				if (primary == null)
				{
					Log.Warn("exchange-rates", "Primary currency (Id #1) not found. Skipping rate update.");
					return;
				}

				var json = await _http.GetStringAsync($"https://open.er-api.com/v6/latest/{primary.Code}");
				var response = JsonSerializer.Deserialize<ExchangeRateResponse>(json, _jsonOptions);

				if (response?.Rates == null || !string.Equals(response.BaseCode, primary.Code, StringComparison.OrdinalIgnoreCase))
				{
					Log.Warn("exchange-rates", "Exchange rate API returned null or unexpected format.");
					return;
				}

				var rates = new Dictionary<string, double>(response.Rates, StringComparer.OrdinalIgnoreCase);

				var all = await Where("", DataOptions.IgnorePermissions).ListAll(context);

				foreach (var currency in all)
				{
					if (currency.Id == 1) continue;

					if (rates.TryGetValue(currency.Code, out var rate))
					{
						await Update(context, currency, (Context c, Currency toUpdate, Currency original) =>
						{
							toUpdate.ExchangeRateForPrimary = rate;
						}, DataOptions.IgnorePermissions);
					}
				}

				_config.InvalidateFrontendConfigCache();
			}
			catch (Exception ex)
			{
				Log.Warn("exchange-rates", ex, "Failed to fetch or apply exchange rates. Will retry on next scheduled run.");
			}
		}
	}

	/// <summary>
	/// Response shape of the open.er-api.com exchange rate endpoint:
	/// { "result": "success", "base_code": "GBP", "rates": { "GBP": 1, ... } }
	/// </summary>
	internal class ExchangeRateResponse
	{
		public string Result { get; set; }
		public string BaseCode { get; set; }
		public Dictionary<string, double> Rates { get; set; }
	}
}
