import Text from 'UI/Text';

/*
	Ensures a value is provided. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/

export default value => {
	
	if(!value || value.trim() == ''){
		return {
			error: 'EMPTY',
			ui: <Text>
				This field is required
			</Text>
		};
	}
	
}