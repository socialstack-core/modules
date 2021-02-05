import queryString from 'UI/Functions/QueryString';

/*
* Gets the site root relative URL for a particular entity. The entity must have a pageId field.
* Optionally also provide query parameters as an object. {a:'hi', b:'hello'} becomes ?a=hi&b=hello etc.
*/
export default function url(entity, queryParams, adminScope = false) {
	
	var qs = queryString(queryParams);
	
	//console.log("Url.js");
	//console.log(entity);
	if(!entity){
		// 'this' page. Essentially just changing the qParams.
		return qs;
	}
	
	if(entity.pageId){
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

	else if(entity.type) {
		// Which scope are we grabbing from?
		if(adminScope) {
			var page = global.pageRouter.state.adminContentMap[entity.type.toLowerCase()];
		} else {
			var page = global.pageRouter.state.contentMap[entity.type.toLowerCase()];
		}

		if(!page){
			// Page not found for this content.
			return '/*' + qs;
		}

		// Alrighty, now to build our url for this content.
		// explode our string
		var builtUrl ='';
		var parts = page.page.url.split('/');
		
		for(var i=0;i<parts.length;i++){
			console.log(builtUrl);
			var part = parts[i];
			if(part[0] == '{'){
				// It's a :token
				
				console.log(page.parameter);
				builtUrl += '/' + entity[page.parameter];
			}else{
				builtUrl += '/' + part;
			}
		}

		console.log(builtUrl);
		return builtUrl + qs;

	} else {
		return '/' + qs;
	}
	
	
}