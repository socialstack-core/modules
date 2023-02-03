/*
	Duplicated from Admin/Functions/GetAutoForm
*/
import webRequest from 'UI/Functions/WebRequest';

/* Autoform cache */
var cache = {};

/*
* Gets info for a particular autoform. Returns a promise.
* Type is the autoform type, usually "content" or e.g. "config".
* Name is the form name, e.g. "content","user"
*/
export default (type, name) => {
	if(!cache[type]){
		cache[type]={};
	}
	
	name = name.toLowerCase();
	
	if(cache[type][name]){
		return Promise.resolve(cache[type][name]);
	}
	
	return webRequest("autoform/" + type + "/" + name).then(response => {
		var form = response.json;

		if (!form.fields) {
			return null;
		}
		
		form = {form, canvas: JSON.stringify({content: form.fields})};
		cache[type][name] = form;
		return form;
	});
}