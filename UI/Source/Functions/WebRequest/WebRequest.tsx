/* This module sends web requests for us. */
import store from 'UI/Functions/Store';
import contentChange from 'UI/Functions/ContentChange';
import { PublicError } from 'UI/Failed';
import { Content } from 'Api/Database';

/**
 * Underlying error type from the server. Use PublicError in the frontend instead.
 */
interface PublicException {

	/**
	 * The error type. Usually a two part general/specific combination such as "invalid/json".
	 */
	code: string;

	/**
	 * A human friendly error message. Display this on the UI.
	 */
	message: string;

}

/**
 * Internal structure for handling responses with includes.
 */
interface HasIncludes {
	includes?: any
}

export interface ApiContent<T> extends HasIncludes {
	/**
	 * The singular result.
	 */
	result?: T
};

export interface ApiList<T> extends HasIncludes {
	/**
	 * The list of results. Will be at least an empty array if there were none.
	 */
	results: T[],

	/**
	 * If you asked for a paginated fragment this 
	 * will be the total number of records.
	 */
	totalResults: int,

	/**
	 * Secondary result sets if there are any. Each one is also an ApiList of an unknown type, 
	 * however, the key will always identify the type.
	 */
	secondary?: Record<string, any>
};

interface WebRequestOptions {
	/**
	 * Additional headers.
	 */
	headers?: Record<string, string>;

	/**
	 * Locale ID.
	 */
	locale?: int;

	/**
	 * An array of fields to include. Some fields, such as tags, will only be sent by the server if you request them this way.
	 */
	includes?: string[];

	/**
	 * Advanced uses only. The origin is automatically identified but sometimes you may need to forcefully state 
	 * that the targeted server should be treated as an origin, meaning it is sent auth related headers.
	 */
	toOrigin?: boolean;

	/**
	 * Http verb to use. If not specified, it will auto be 
	 * either 'post' or 'get' depending on if you provide a post body or not.
	 */
	method?: string;
}

export class ApiIncludes {
    private text: string = '';

    constructor(existing: string = '', addition: string = ''){
        this.text = (existing.length != 0) ? existing : '';
        if (addition.length != 0) {
             if (this.text != ''){
                this.text += '.'
             }
             this.text += addition;
        }
    }

    toString(){ return this.text }
}

export type ApiInclude = ApiIncludes | string;

/**
 * Expands include set on API returned results.
 * @param response
 * @returns
 */
export function expandIncludes<T>(response : any){
	if (!response) {
		return null;
	}

	var { result, results, includes, secondary } = response;

	if(includes){
		for(var i=includes.length-1;i>=0;i--){
			var inc = includes[i];
			var targetValues = inc.on === undefined ? (results || [result]) : includes[inc.on].values;
			
			if (typeof inc.map == 'string') {
				// Each object in values has a field of the given name, which contains an ID that maps to an object in _targetValues_.
				var targetIdMap: Record<number, any> = {};
				targetValues.forEach((v: any) => {
					targetIdMap[v.id] = v;
					v[inc.field] = [];
				});
				inc.values.forEach((v: any) => {
					var target = targetIdMap[v[inc.map]];
					if (target) {
						target[inc.field].push(v);
					}
				});
			} else if (Array.isArray(inc.values)) {
				// Each value has a field named the same as this include (or inc.src if it is set).
				var srcField = inc.src || inc.field;

				var byIdMap: Record<number, any> = {};
				inc.values.forEach((v: any) => byIdMap[v.id] = v);
				targetValues.forEach((val: any) => {
					if (Array.isArray(val[srcField])) {
						val[inc.field] = val[srcField].map((id: any) => {
							if (id.id) {
								// Already loaded - skip.
								return id;
							}

							return byIdMap[id];
						});
					} else {
						val[inc.field] = byIdMap[val[srcField]];
					}
				});
			} else if(inc.values) {
				// Dynamic includes - it's an object of content types.
				var srcField = inc.src || inc.field;

				// Create an id map for each type.
				var byTypeMaps: Map<string, Map<number, any>> = new Map<string, Map<number, any>>();
				for (var contentType in inc.values) {
					var contents = inc.values[contentType]?.results;
					var idLookup = new Map<number, any>();
					byTypeMaps.set(contentType, idLookup);
					contents?.forEach((v: any) => idLookup.set(v.id, v));
				}

				targetValues.forEach((val: any) => {
					var objInfo = val[srcField];
					if (!objInfo?.type || !objInfo.id) {
						val[inc.field] = null;
						return;
					}

					// Look it up by type and then the id.
					var idLookup = byTypeMaps.get(objInfo.type);

					if (!idLookup) {
						return;
					}

					val[inc.field] = idLookup.get(objInfo.id);
				});
			}
		}
	}
	
	if(secondary){
		// Secondary result sets
		for(var k in secondary){
			expandIncludes(secondary[k]);
		}
	}
	
	return (response.result ? response.result : response) as T;
}

/**
 * A fetch response with the loading .text() result from it.
 */
interface ResponseWithText {
	response: Response,
	text: string
}

let _lazyCache : Record<string, any>;

/*
* Lazy loads a .js file represented by a url.
*/
export function lazyLoad(url: string) {
	var cache;
	
	if (!_lazyCache){
		_lazyCache = {};
	}
	
	var entry = _lazyCache[url];
	if(!entry){
		entry = getText(url)
		.then(js => {
			try{
				var ex = {
					window
				};
				var f = new Function('exports','global','window', js);
				f(ex, window, window);
				_lazyCache[url] = Promise.resolve(ex);
			}catch(e){
				console.log(e);
			}
			return _lazyCache[url];
		});
		_lazyCache[url] = entry;
	}
	return entry;
}

/**
* Builds an ?includes (or &includes if isAmp is true) query string.
*/
export function includeString(includes?:ApiInclude[], isAmp?: boolean){
	if(!includes || !Array.isArray(includes) || !includes.length){
		return '';
	}
	
	return (isAmp ? '&' : '?') + 'includes=' + includes.join(',');
}

/**
 * Gets a raw blob.
 */
export function getBlob(origUrl: string, data?: any, opts?: WebRequestOptions): Promise<Blob> {
	return _fetch(origUrl, data, opts)
			.then(response => response.blob());
}

/**
 * Internal use function. Gets the response with text.
 */
function getTextResponse(origUrl: string, data?: any, opts?: WebRequestOptions): Promise<ResponseWithText> {
	return _fetch(origUrl, data, opts)
		.then(response => {
			return response.text()
				.then(text => {
					return {
						response,
						text
					} as ResponseWithText;
				});
		});
}

/**
 * Gets raw text.
 */
export function getText(origUrl: string, data?: any, opts?: WebRequestOptions): Promise<string> {
	return getTextResponse(origUrl, data, opts)
		.then(rwt => rwt.text);
}

/**
 * Gets raw parsed JSON.
 */
export function getJson<T>(origUrl: string, data?: any, opts?: WebRequestOptions): Promise<T> {
	return getTextResponse(origUrl, data, opts)
		.then(rwt => {
			let json;

			try {
				json = (rwt.text && rwt.text.length) ? JSON.parse(rwt.text) : null;
			}
			catch (e: any) {
				throw new PublicError('invalid/json', `Invalid response from the server`, e);
			}

			if (!rwt.response.ok) {
				// json is null if it was a 404 typically
				var fail = json as PublicException;
				var type = fail?.code || `response/failed`;
				var message = fail?.message || `Invalid response from the server`;
				throw new PublicError(type, message);
			}

			return json as T;
		});
}

/**
 * Gets a potentially paginated list fragment. Expands any present includes for you.
 */
export function getList<T>(origUrl: string, data?: any, opts?: WebRequestOptions) {
	return getJson<ApiList<T>>(origUrl, data, opts)
		.then(apiList => {
			var result = expandIncludes<ApiList<T>>(apiList);

			if (!result) {
				throw new PublicError('content/none', `Content was not found`);
			}

			// Ensure the array exists:
			if (!result.results) {
				result.results = [];
			}

			return result;
		});
}

/**
 * Handles API endpoints (load, delete, update) which return {"result": T, "includes": ...}.
 * Expands includes in to the object for you, and then returns it.
 */
export function getOne<T>(origUrl: string, data?: any, opts?: WebRequestOptions) {
	return getJson<ApiContent<T>>(origUrl, data, opts)
		.then(apiContent => {
			var result = expandIncludes<T>(apiContent);

			// If we're not blocked from doing so with the options
			// and this was either a DELETE or POST request which returned an entity
			// then we'll fire off a contentchange event to tell the UI
			// that content has updated.

			if (!result) {
				throw new PublicError('content/none', `Content was not found`);
			}

			// Trigger a contentchange event if it was a POST and it returned an entity:
			var method = 'get';

			if (opts && opts.method) {
				method = opts.method.toLowerCase();
			} else if (data) {
				method = 'post';
			}
			
			var cont = (result as any) as Content<uint>;

			if (cont.id && cont.type && method != 'get') {

				// If method was 'delete' then this entity was deleted.
				// Otherwise, as it's not specified, contentchange will establish if it was added or deleted based on the given url.
				contentChange(cont, origUrl, { deleted: (method == 'delete'), updated: false, added: false, created: false });
			}

			return result;
		});
}

/**
 * Underlying fetch mechanic. Handles constructing the actual URL, 
 * ensuring fetch is configured with auth and so on.
 * @param origUrl
 * @param data
 * @param opts
 * @returns
 */
function _fetch(origUrl: string, data? : any, opts? : WebRequestOptions) {
	var apiUrl = window.apiHost || '';

	if (!apiUrl.endsWith('/')) {
		apiUrl += '/';
	}

	apiUrl += 'v1/';

	var url = (origUrl.indexOf('http') === 0 || origUrl.indexOf('file:') === 0 || origUrl[0] == '/' || origUrl[0] == '.') ? origUrl : apiUrl + origUrl;

	var origUrl = url;
	var credentials : RequestCredentials | undefined = undefined;
	var mode : RequestMode = 'cors';

	var headers = opts ? opts.headers || {} : {};
	
	var toOrigin = true;
	
	// It's not to the origin if url is absolute and is a different server to our location origin
	if(url.indexOf('http') === 0){
		// different origin?
		
		if (!((opts && opts.toOrigin) || (window.apiHost && url.indexOf(window.apiHost) === 0) || url.indexOf(location.origin) === 0)){
			// Non-origin request
			toOrigin = false;
		}
		
	}
	
	if(toOrigin){
		if(window.storedToken){
			headers['Token'] = window.storedToken;
		}
		
		if(opts && opts.locale){
			headers['Locale'] = opts.locale.toString();
		}
		
		credentials = window.storedToken ? undefined : 'include';
	}
	
	var includeSet = opts && opts.includes;
	var includes: string = '';

	if (Array.isArray(includeSet)){
		includes = includeSet.map(x=>x.trim()).join(',');
	}
	
	if(includes){
		url += '?includes=' + includes;
	}

	var req: Promise<Response>;

	if (!data) {
		req = fetch(url, { method: opts && opts.method ? opts.method : 'get', mode, credentials, headers });
	} else if ((FormData && data instanceof FormData) || (Uint8Array && data instanceof Uint8Array)) {
		req = fetch(url, {
			method: opts && opts.method ? opts.method : 'post',
			body: data,
			mode,
			credentials,
			headers
		});
	} else {
		req = fetch(url, {
			method: opts && opts.method ? opts.method : 'post',
			headers: {
				'Accept': 'application/json',
				'Content-Type': 'application/json',
				...headers
			},
			mode,
			body: JSON.stringify(data),
			credentials
		});
	}

	return req.then(response => {
		if (window.storedToken && response.headers) {
			var token = response.headers.get('Token');
			if (token) {
				store.set('context', token);
			}
		}

		return response;
	});
}
