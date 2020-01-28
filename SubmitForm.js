import webRequest from 'UI/Functions/WebRequest';


export default function (e, options) {
	options = options || {};
	e.preventDefault();
	
	var fields = e.target.elements;
	
	var values = {};
	
	for(var i=0;i<fields.length;i++){
		var field = fields[i];
		
		if(!field.name || ((field.type == 'radio' || field.type=='checkbox') && !field.checked)){
			continue;
		}
		
		values[field.name] = field.type=='checkbox' ? true : field.value;
	}
	
	if(options.onValues){
		// Map the values:
		values = options.onValues(values);
	}
	
	// Resolve the values. This permits onValues to do async logic:
	Promise.resolve(values).then(values => {
		
		// Send it off now:
		return webRequest(e.target.action, values).then(
			response => {
				if (response.ok) {
					// How successful was it? (Did we get a 200 response?)
					// NB: Run success last and after a small delay 
					// (to be able to unmount the component in full / reset the form).
					setTimeout(() => {
						options.onSuccess && options.onSuccess(response.json, values);
					}, 1);
					
				} else {
					console.log(response.json);
					if (options.onFailed) {
						options.onFailed(response.json);
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