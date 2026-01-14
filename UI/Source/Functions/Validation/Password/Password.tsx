/**
	Ensures a password has basic restraints. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/
export default (value: string) : PublicError | undefined => {
	
	if(!value){
		return;
	}
	
	if (value.length < 10) {
		return {
			type: 'field/length',
			message: `Must be at least 10 characters long`
		};
	}
	
	const hasNumbers = /\d/.test(value);
	// const hasNonalphas = /\W/.test(value);
	const hasUppercase = /[A-Z]/.test(value);
   //  || !hasNonalphas
	if (!hasNumbers || !hasUppercase) {
		return {
			type: 'field/complexity',
			message: `Password must contain at least 1 number and a capital letter`
		};
	}

}
