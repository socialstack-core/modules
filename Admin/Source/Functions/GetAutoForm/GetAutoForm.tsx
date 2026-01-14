import autoFormApi, { AutoFormInfo } from 'Api/AutoFormController';

interface CachedForm {
	form: AutoFormInfo,
	name: string
}

/* Autoform cache */
var cache: Record<string, Record<string, CachedForm>> = {};

/*
* Gets info for a particular autoform. Returns a promise.
* Type is the autoform type, usually "content" or e.g. "config".
* Name is the form name, e.g. "content","user"
*/
export default (type: string, name : string) => {
	if(!cache[type]){
		cache[type] = {} as Record<string, CachedForm>;
	}
	
	const lcName = name.toLowerCase();
	
	if (cache[type][lcName]){
		return Promise.resolve(cache[type][lcName]);
	}

	return autoFormApi.get(type, lcName).then(form => {
		var cacheInfo: CachedForm = {
			name,
			form
		};
		cache[type][lcName] = cacheInfo;
		return cacheInfo;
	});
}

var gotAll = false;

export function getAllContentTypes() {
	
	if(gotAll){
		return Promise.resolve(cache.content);
	}
	
	return autoFormApi.allContentForms().then(response => {
		gotAll = true;
		
		cache.content = {};
		var byEndpoint : Record<string, AutoFormInfo> = {};
		var forms = response.forms || [];
		
		forms.forEach(form => {
			var ep = form.endpoint;
			if (ep) {
				byEndpoint[ep] = form;
			}
		});
		
		var types = response.contentTypes;
		
		types?.forEach(type => {
			var lcName = type.name?.toLowerCase();
			var form = byEndpoint['v1/' + lcName];
			
			if (!form || !lcName){
				return;
			}
			
			cache.content[lcName] = {
				form,
				name: type.name || ''
			};
		});
		
		return cache.content;
	});
	
}