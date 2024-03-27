import webRequest from 'UI/Functions/WebRequest';


export default function (e, options) {
	options = options || {};
	e.preventDefault();
	
	var fields = e.target.elements;
	
	var values = {};
	var validationErrors = 0;
	var valuePromises = [];
	
	for(var i=0;i<fields.length;i++){
		(function(field){
			try {
				if(!field.name || field.type=='submit' || (field.type == 'radio' && !field.checked)){
					return;
				}
				
				if(field.onValidationCheck && field.onValidationCheck(field)){
					validationErrors++;
				}
				
				var value = field.type=='checkbox' ? field.checked : field.value;
				
				if(field.onGetValue){
					value = field.onGetValue(value, field, e);
					
					if(value && value.then && typeof value.then === 'function'){
						// It's a promise.
						// Must wait for all of these before proceeding.
						valuePromises.push(value.then((val) => {
							field.value = val;
							values[field.name] = val;
						}));
					}else{
						field.value = value;
						values[field.name] = value;
					}
					
				}else{
					values[field.name] = value;
				}
			} catch (e) {
				// Catch just to flag to the user, and then throw again so that the save process does not continue
				options.onFailed && options.onFailed(e);
				throw e;
			}
		})(fields[i]);
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
	
	Promise.all(valuePromises)
	.then(() => {
		
		if(options.onValues){
			// Map the values:
			values = options.onValues(values, e);
		}
		
		return values;
	})
	.then(values => {
		
		if(values){
			// Tidiness - delete the set action function:
			delete values.setAction;
		}
		
		if(!action){
			// Form doesn't have an action="". Does it have an onSubmitted handler?
			if(options.onSubmitted){
				// Yes! The form can do custom processing here.
				var result = options.onSubmitted(values, options.requestOpts);
				if(result && result.then){
					return result.then(response => {
						// How successful was it? (Did we get a 200 response?)
						// NB: Run success last and after a small delay 
						// (to be able to unmount the component in full / reset the form).
						setTimeout(() => {
							options.onSuccess && options.onSuccess(response, values, e);
						}, 1);
					}); // the catch further down handles this failure too.
				}else{
					options.onSuccess && options.onSuccess(values, values, e);
				}
			}else{
				// Just run onsuccess if validation passed:
				options.onSuccess && options.onSuccess({formHasNoAction: true, ...values}, values, e);
				return;
			}
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
					options.onFailed && options.onFailed(response.json, values, e);
				}
				
			}
		);
		
	}).catch(err => {
		console.log(err);
		
		options.onFailed && options.onFailed(err);
	});
	
	return false;
}
