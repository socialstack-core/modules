/**
 * return number of decimal places for given currencyCode (e.g. 2 for GBP)
 * @param {any} locale
 */
const fractionDigits = (currencyCode, localeCode) => {
	return new Intl.NumberFormat(localeCode, {
		style: 'currency',
		currency: currencyCode,
	}).resolvedOptions().maximumFractionDigits;
};

/**
 * format currency value
 * @param {any} value expects amount in pennies / cents etc (e.g. 12345 = 123.45)
 * @param {any} options
 * 
 * options available are:
 * - currencyDisplay (default: "symbol")
 *   "symbol": use a localized currency symbol such as the €.
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
 * formatCurrency(12345, { currencyCode: "EUR" })
 */
const formatCurrency = (value, options) => {
	options = options || {};
	const { currencyCode, localeCode } = options;

	if (!currencyCode) {
		throw new Error('currency reqd.');
	}

	/**
	 * Guard against invalid or missing numeric values.
	 *
	 * `undefined`, `null`, or non-numeric values would otherwise
	 * be coerced via arithmetic (value / divisor) into `NaN`,
	 * which Intl.NumberFormat formats as "0", causing misleading
	 * output in the UI.
	 *
	 * Returning an empty string prevents accidental display of
	 * an incorrect zero-value price.
	 */
	const numericValue = typeof value === "number"
		? value
		: typeof value === "string" && value.trim() !== ""
			? Number(value)
			: NaN;

	if (!Number.isFinite(numericValue)) {
		return "";
	}

	var currencyFractionDigits = fractionDigits(currencyCode, localeCode);
	var hideSymbol = options.hideSymbol || (options.currencyDisplay != undefined && (options.currencyDisplay == "none" || options.currencyDisplay == false));

	var divisor = Math.pow(10, currencyFractionDigits);

	if (hideSymbol) {
		return (value / divisor).toLocaleString(localeCode, {
			minimumFractionDigits: currencyFractionDigits,
			maximumFractionDigits: currencyFractionDigits
		});

    }

	return new Intl.NumberFormat(localeCode, {
		style: 'currency',
		currency: currencyCode,
		currencyDisplay: !hideSymbol && options.currencyDisplay ? options.currencyDisplay : undefined,
		minimumFractionDigits: options.hideDecimals ? 0 : undefined,
		maximumFractionDigits: options.hideDecimals ? 0 : undefined
	}).format(value / divisor);

};

/**
 * format "price on application" label for missing prices (e.g. "€POA")
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
 * - currencyCode: defaults to locale.currencyCode
 * 
 * for example, to render "POA" in a foreign currency:
 * formatPOA({ currencyCode: "EUR" })
 */
const formatPOA = (options) => {
	options = options || {};
	const { currencyCode, localeCode } = options;

	if (!currencyCode) {
		throw new Error('currency reqd.');
	}

	var formatter = new Intl.NumberFormat(localeCode, {
		style: 'currency',
		currency: currencyCode,
		currencyDisplay: options.currencyDisplay ? options.currencyDisplay : undefined,
		minimumFractionDigits: 0,
		maximumFractionDigits: 0
	});

	const parts = formatter.formatToParts(0);

	// Reconstruct the string, replacing the number with 'POA'
	const result = parts.map(part => {
		if (part.type === 'integer' || part.type === 'decimal' || part.type === 'fraction') {
			return `POA`;
		}
		return part.value;
	}).join('');

	return result;
};

export {
	fractionDigits,
	formatCurrency,
	formatPOA
};
