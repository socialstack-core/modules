import Text from 'UI/Text';

/*
	Phone number (UK/US) validation method. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/

export default value => {
	if(!value){
		// Also use "Required" validation if you want it to check in this scenario.
		return;
	}
	
	if(/[^0-9\+\-\.]+/.test(value)){
		return {
			error: 'FORMAT',
			ui: <Text>
				Please provide a valid phone number
			</Text>
		};
	}
};