import webRequest from 'UI/Functions/WebRequest';

/* cache */
var cache = null;

/*
* Gets the list of content types on the site. Returns a promise.
*/
export default () => {
	if(cache != null){
		return Promise.resolve(cache);
	}
	
	return cache = webRequest("autoform").then(response => {
		return cache = response.json.contentTypes;
	});
}