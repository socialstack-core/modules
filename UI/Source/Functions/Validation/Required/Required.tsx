/**
	Ensures a value is provided. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/
export default (value: string) : PublicError | undefined => {
	
	if(!value || (value.trim && value.trim() == '')){
		return {
			type: 'field/required',
			message: `This field is required`
		};
	}
	
}