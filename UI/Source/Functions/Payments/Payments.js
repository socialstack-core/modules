const recurrenceText = (billingFreq) => {
	switch(billingFreq){
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
}

export {
	recurrenceText
};
