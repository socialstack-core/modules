import autoformApi, { ContentType } from 'Api/AutoFormController';

/* cache */
var cache: ContentType[] | null = null;

/*
* Gets the list of content types on the site. Returns a promise.
*/
export default () => {
	if(cache){
		return Promise.resolve(cache);
	}
	
	return autoformApi.allContentForms()
		.then(structure => {
			return cache = structure.contentTypes;
		});
}