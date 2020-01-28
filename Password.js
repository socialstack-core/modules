import Text from 'UI/Text';

/*
	Password validation method. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/
export const validatePassword = value => {
	if(!value){
		return;
	}
	
	if (value.length < 8) {
		return {
			error: 'LENGTH',
			ui: <Text>
				Must be at least 8 characters long
			</Text>
		};
	}
	
	const hasNumbers = /\d/.test(value);
	const hasNonalphas = /\W/.test(value);
	const hasUppercase = /[A-Z]/.test(value);

	if (!hasNumbers || !hasNonalphas || !hasUppercase) {
		return {
			error: 'COMPLEXITY',
			ui: <Text>
				Password must contain at least 1 number, 1 non alphabet character and a capital letter
			</Text>
		};
	}
};