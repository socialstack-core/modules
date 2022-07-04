import Text from 'UI/Text';

/*
	Password validation method. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/
export default value => {
	if(!value){
		return;
	}
	
	if (value.length < 10) {
		return {
			error: 'LENGTH',
			ui: <Text>
				Must be at least 10 characters long
			</Text>
		};
	}
	
	const hasNumbers = /\d/.test(value);
	// const hasNonalphas = /\W/.test(value);
	const hasUppercase = /[A-Z]/.test(value);
   //  || !hasNonalphas
	if (!hasNumbers || !hasUppercase) {
		return {
			error: 'COMPLEXITY',
			ui: <Text>
				Password must contain at least 1 number and a capital letter
			</Text>
		};
	}
};