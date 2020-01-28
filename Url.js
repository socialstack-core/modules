import queryString from 'UI/Functions/QueryString';

/*
* Gets the site root relative URL for a particular entity. The entity must have a pageId field.
* Optionally also provide query parameters as an object. {a:'hi', b:'hello'} becomes ?a=hi&b=hello etc.
*/
export default function url(entity, queryParams) {
	
	var qs = queryString(queryParams);
	
	if(!entity){
		// 'this' page. Essentially just changing the qParams.
		return qs;
	}
	
	if(!entity.pageId){
		// Must have a page ID.
		return '/' + qs;
	}
	
	// Get page by its ID
	// Swap out :FieldName with entity.FieldName
	var page = global.pageRouter.state.idMap['' + entity.pageId];
	
	if(!page){
		// Page not found.
		return '/*' + qs;
	}
	
	// Build the url next.
	var builtUrl ='';
	
	var parts = page.url.split('/');
	
	for(var i=0;i<parts.length;i++){
		var part = parts[i];
		if(part[0] == ':'){
			// It's a :token
			builtUrl += '/' + entity[part.substring(1)];
		}else{
			builtUrl += '/' + part;
		}
	}
	
	return builtUrl + qs;
}