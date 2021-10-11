import webRequest from 'UI/Functions/WebRequest';


export default function (e, options) {
	options = options || {};
	e.preventDefault();
	
	var fields = e.target.elements;
	
	var values = {};
	var validationErrors = 0;
	
	for(var i=0;i<fields.length;i++){
		var field = fields[i];
		
		if(!field.name || (field.type == 'radio' && !field.checked)){
			continue;
		}
		
		if(field.onValidationCheck && field.onValidationCheck(field)){
			validationErrors++;
		}
		
		var value = field.type=='checkbox' ? field.checked : field.value;
		
		if(field.onGetValue){
			value = field.onGetValue(value, field, e);
			field.value = value;
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
	var actionPieces = action.split('/v1/');
	if(actionPieces.length >= 2){
		action = actionPieces[1];
	}
	
	values.setAction = (newAction) => {
		action = newAction;
	};
	
	if(options.onValues){
		// Map the values:
		values = options.onValues(values, e);
	}

	var html = document.querySelector("html");
	
	// Resolve the values. This permits onValues to do async logic:
	Promise.resolve(values).then(values => {
		
		if(values){
			// Tidiness - delete the set action function:
			delete values.setAction;
		}
		
		if(!action){
			// Form doesn't have an action="", but we run onsuccess if validation passed:
			options.onSuccess && options.onSuccess({formHasNoAction: true, ...values}, values, e);
			return;
		}

		if (html) {
			html.style.cursor = "wait";
		}

		// Send it off now:
		return webRequest(action, values, options.requestOpts).then(
			response => {

				if (html) {
					html.style.cursor = "pointer";
				}

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

		if (html) {
			html.style.cursor = "pointer";
		}

		options.onFailed && options.onFailed(err);
	});
	
	return false;
}