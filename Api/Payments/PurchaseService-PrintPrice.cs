namespace Api.Payments;


public partial class PurchaseService
{
	
	/// <summary>
	/// Outputs e.g. $4.00 based on the given amount and currency code.
	/// In a separate file to minimise encoding issues on the characters.
	/// </summary>
	/// <param name="amount"></param>
	/// <param name="currencyCode"></param>
	/// <returns></returns>
	public string PrintPrice(ulong amount, string currencyCode)
	{
		uint divisor = 100;
		var fractionalLengthIndicator = "D2";
		string symbol;
		var postfix = false; // if postfix, symbol goes after the main amount.
		var fractionalSpacer = '.';
		// var decimalSpacer = ',';

		switch (currencyCode)
		{
			case "ARS":
				// Peso
				symbol = "$";
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "AUD":
				// Australian Dollar
				symbol = "$";
			break;
			case "BRL":
				// Brazilian Real
				symbol = "R$";
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "CAD":
				// Canadian Dollar
				symbol = "$";
			break;
			case "CLP":
				// Chilean Peso
				symbol = "$";
			break;
			case "CNY":
				// Yuan
				symbol = "¥";
			break;
			case "COP":
				// Columbian Peso
				symbol = "$";
			break;
			case "CZK":
				// Danish Krone
				symbol = "kr.";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "DKK":
				// Czech Koruna
				symbol = "Kč";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "EUR":
				// Euro
				symbol = "€";
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "HKD":
				// Hong Kong Dollar
				symbol = "HK$";
			break;
			case "HUF":
				// Hungarian Forint
				symbol = "Ft";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "INR":
				// Indian Rupee
				symbol = "₹";
			break;
			case "ILS":
				// New Israeli Shekel
				symbol = "₪";
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "JPY":
				// Yen
				symbol = "¥";
			break;
			case "KRW":
				// Won
				symbol = "₩";
			break;
			case "MYR":
				// Malaysian Ringgit
				symbol = "RM";
			break;
			case "MXN":
				// Mexican Peso
				symbol = "$";
			break;
			case "MAD":
				// Moroccan Dirham
				symbol = ".د.م.";
				postfix = true;
			break;
			case "NZD":
				// New Zealand Dollar
				symbol = "$";
			break;
			case "NOK":
				// Norwegian Krone
				symbol = "kr";
			break;
			case "PHP":
				// Philippine Peso
				symbol = "₱";
			break;
			case "PLN":
				// Zloty (Poland)
				symbol = "zł";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "RUB":
				// Russian Ruble
				symbol = "p.";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "SAR":
				// Saudi Riyal
				symbol = "﷼";
				postfix = true;
			break;
			case "SGD":
				// Singapore Dollar
				symbol = "$";
			break;
			case "ZAR":
				// Rand
				symbol = "R";
			break;
			case "SEK":
				// Swedish Krona
				symbol = "kr";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "CHF":
				// Swiss Franc
				symbol = "fr.";
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			case "TWD":
				// New Taiwan Dollar
				symbol = "元";
			break;
			case "THB":
				// Baht
				symbol = "฿";
				postfix = true;
			break;
			case "TRY":
				// Turkish Lira
				symbol = "₺";
				postfix = true;
			break;
			case "GBP":
				symbol = "£";
			break;
			case "USD":
				symbol = "$";
			break;
			case "VND":
				// Dong (Vietnam)
				symbol = "₫";
				postfix = true;
				fractionalSpacer = ',';
				// decimalSpacer = '.';
			break;
			default:
				symbol = currencyCode;
				postfix = true;
			break;
		}

		var primary = amount / divisor;
		var fractional = amount - (primary * divisor);

		if (postfix)
		{
			return primary + fractionalSpacer + fractional.ToString(fractionalLengthIndicator) + symbol;
		}

		return symbol + primary + fractionalSpacer + fractional.ToString(fractionalLengthIndicator);
	}
	
}