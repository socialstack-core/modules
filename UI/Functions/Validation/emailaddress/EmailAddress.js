import Text from 'UI/Text';

/*
	Email address validation method. Returns translatable React element if it failed alongside a constant error code, or nothing.
*/
export default value => {
    var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    
    if(!re.test(String(value).toLowerCase())){
        return {
			error: 'FORMAT',
			ui: <Text>
				{`Please provide a valid email address`}
			</Text>
		};
    }
}