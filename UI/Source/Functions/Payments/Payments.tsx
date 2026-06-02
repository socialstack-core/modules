/**
 * returns billing frequency description (1 = week, 2 = month, 3 = quarter, 4 = year)
 * @param billingFreq
 * @returns
 */
export const recurrenceText = (billingFreq: Number) => {
	switch (billingFreq) {
		case 0:
			return '';
		case 1:
			return `/ week`;
		case 2:
			return `/ month`;
		case 3:
			return `/ quarter`;
		case 4:
			return `/ year`;
	}
};
