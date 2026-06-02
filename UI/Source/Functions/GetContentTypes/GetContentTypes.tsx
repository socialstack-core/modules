import { AutoFormApi, ContentType } from 'Api/AutoForms';

/* cache */
var cache: ContentType[] | null = null;

/*
* Gets the list of content types on the site. Returns a promise.
*/
export default () => {
	if(cache){
		return Promise.resolve(cache);
	}
	
	return AutoFormApi.allContentForms()
		.then(structure => {
			return cache = structure.contentTypes || null;
		});
}