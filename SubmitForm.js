import webRequest from 'UI/Functions/WebRequest';


export default function (e, options) {
	options = options || {};
	e.preventDefault();
	
	var fields = e.target.elements;
	
	var values = {};
	var validationErrors = 0;
	
	for(var i=0;i<fields.length;i++){
		var field = fields[i];
		
		if(!field.name || ((field.type == 'radio' || field.type=='checkbox') && !field.checked)){
			continue;
		}
		
		var validation = field.getAttribute('data-validation');
		if(validation){
			validationErrors++;
		}
		
		var value = field.type=='checkbox' ? true : field.value;
		
		if(field.onGetValue){
			value = field.onGetValue(value);
		}
		
		values[field.name] = value;
	}
	
	if(e.submitter && e.submitter.name){
		values[e.submitter.name] = e.submitter.value;
	}
	
	if(validationErrors){
		if (options.onFailed) {
			options.onFailed('VALIDATION', validationErrors, e);
		}
		return;
	}
	
	var action = e.target.action;
	
	values.setAction = (newAction) => {
		action = newAction;
	};
	
	if(options.onValues){
		// Map the values:
		values = options.onValues(values, e);
	}
	
	// Resolve the values. This permits onValues to do async logic:
	Promise.resolve(values).then(values => {
		
		if(values){
			// Tidiness - delete the set action function:
			delete values.setAction;
		}
		
		// Send it off now:
		return webRequest(action, values, options.requestOpts).then(
			response => {
				if (response.ok) {
					// How successful was it? (Did we get a 200 response?)
					// NB: Run success last and after a small delay 
					// (to be able to unmount the component in full / reset the form).
					setTimeout(() => {
						options.onSuccess && options.onSuccess(response.json, values, e);
					}, 1);
					
				} else {
					console.log(response.json);
					if (options.onFailed) {
						options.onFailed(response.json, values, e);
					}
				}
				
			}
		);
		
	}).catch(err => {
		console.log(err);
		if (options.onFailed) {
			options.onFailed(err);
		}
	});
	
	return false;
}