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
		form = {form, canvas: JSON.stringify({content: form.fields})};
		cache[type][name] = form;
		return form;
	});
}

var gotAll = false;

export function getAllContentTypes() {
	
	if(gotAll){
		return Promise.resolve(cache.content);
	}
	
	return webRequest("autoform").then(response => {
		gotAll = true;
		
		cache.content = {};
		var byEndpoint = {};
		var forms = response.json.forms;
		
		forms.forEach(form => {
			byEndpoint[form.endpoint] = form;
		});
		
		var types = response.json.contentTypes;
		
		types.forEach(type => {
			var lcName = type.name.toLowerCase();
			var form = byEndpoint['v1/' + lcName];
			
			if(!form){
				return;
			}
			
			cache.content[lcName] = {form, name: type.name, canvas: JSON.stringify({content: form.fields})};
		});
		
		return cache.content;
	});
	
}