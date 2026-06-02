/**
 * format percentage value
 * @param {number} value expects amount in "0.45" format for 45%
 * @param {string} localeCode
 * @param {number} decimals defaults to zero
 * @param {boolean} showNaN defaults to false
 * @param {boolean} showInfinity defaults to false
 */
export const formatPercentage = (value: number, localeCode: string, decimals?: number, showNaN?: boolean, showInfinity?: boolean) => {

	if (!localeCode) {
		throw new Error('formatPercentage: localeCode required');
	}

	var formattedString = new Intl.NumberFormat(localeCode, {
		style: 'percent',
		minimumFractionDigits: decimals || 0,
		maximumFractionDigits: decimals || 0
	}).format(value);

	if (isNaN(value)) {

		if (!showNaN) {
			return;
		}

		// return "-%"
		return formattedString.replace("NaN", "-");
	}

	if (!isFinite(value)) {

		if (!showInfinity) {
			return;
		}

		// return "∞%"
		return formattedString;
	}

	return formattedString;
};
