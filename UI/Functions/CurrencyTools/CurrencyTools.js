/**
 * return currency symbol for given locale
 * @param {any} locale
 */
const getCurrencySymbol = (locale) => {
	var formattedValue = new Intl.NumberFormat(locale.code, {
		style: 'currency',
		currency: locale.currencyCode
	}).format(0);

	return formattedValue[0];
}

/**
 * return number of decimal places for given locale (e.g. 2 for GBP)
 * @param {any} locale
 */
const fractionDigits = (locale) => {
	return new Intl.NumberFormat(locale.code, {
		style: 'currency',
		currency: locale.currencyCode,
	}).resolvedOptions().maximumFractionDigits;
};

/**
 * format currency value
 * @param {any} value
 * @param {any} locale local locale
 * @param {any} options
 * 
 * options available are:
 * - currencyDisplay (default: "symbol")
 *   "symbol": use a localized currency symbol such as €.
 *   "narrowSymbol": use a narrow format symbol ("$100" rather than "US$100").
 *   "code": use the ISO currency code.
 *   "name": use a localized currency name such as "dollar".
 *   "none" / false: omit currency symbol
 *   
 * - hideDecimals (default: false)
 * - hideSymbol (alias for currencyDisplay: "none" / false)
 * - currencyCode: defaults to locale.currencyCode
 * 
 * for example, to render an amount purchased in a foreign currency:
 * formatCurrency(12345, session.locale, { currencyCode: "DE" })
 */
const formatCurrency = (value, locale, options) => {
	options = options || {};
	var currencyFractionDigits = options.hideDecimals ? 0 : fractionDigits(locale);
	var hideSymbol = options.hideSymbol || (options.currencyDisplay != undefined && (options.currencyDisplay == "none" || options.currencyDisplay == false));

	if (hideSymbol) {
		return (value / Math.pow(10, currencyFractionDigits)).toLocaleString(locale.code, {
			minimumFractionDigits: currencyFractionDigits,
			maximumFractionDigits: currencyFractionDigits
		});

    }

	return new Intl.NumberFormat(locale.code, {
		style: 'currency',
		currency: options.currencyCode ? options.currencyCode : locale.currencyCode,
		currencyDisplay: !hideSymbol && options.currencyDisplay ? options.currencyDisplay : undefined,
		minimumFractionDigits: options.hideDecimals ? 0 : undefined,
		maximumFractionDigits: options.hideDecimals ? 0 : undefined
	}).format(value / Math.pow(10, fractionDigits(locale)));

};

export {
	getCurrencySymbol,
	fractionDigits,
	formatCurrency
};
