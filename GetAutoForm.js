import webRequest from 'UI/Functions/WebRequest';

/* Autoform cache */
var cache = null;
let cacheLoading = null;

/*
* Gets info for a particular autoform. Returns a promise.
* Endpoint is e.g. "forum/thread" or "comment" for /v1/comment (create/ update).
*/
export default (endpoint) => {
	endpoint = 'v1/' + endpoint;
	
	if(cache != null){
		return Promise.resolve(cache[endpoint]);
	}
	
	if(cacheLoading == null){
		
		cacheLoading = webRequest("autoform").then(response => {
			var structure = response.json;
			cache = {};
			
			// Ignoring structure.contentTypes for now - we won't need it.
			for(var i=0;i<structure.forms.length;i++){
				var form = structure.forms[i];
				cache[form.endpoint] = {form, canvas: JSON.stringify({content: form.fields})};
			}
			
		});
		
	}
	
	return cacheLoading.then(() => {
		return cache[endpoint];
	});
}