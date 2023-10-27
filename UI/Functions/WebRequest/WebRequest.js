/* This module sends web requests for us. */
import store from 'UI/Functions/Store';
import contentChange from 'UI/Functions/ContentChange';

export function expandIncludes(response){
	if(!response || (!response.result && !response.results)){
		return response;
	}
	
	var {result, results, includes} = response;
	
	if(includes){
		for(var i=includes.length-1;i>=0;i--){
			var inc = includes[i];
			var targetValues = inc.on === undefined ? (results || [result]) : includes[inc.on].values;
			
			if(inc.src){
				// Source field in the original object has the ID. Use that:
				var byIdMap = {};
				inc.values.forEach(v => byIdMap[v.id] = v);
				targetValues.forEach(val => {
					val[inc.field] = byIdMap[val[inc.src]];
				});
			}else if(typeof inc.map == 'string'){
				// Each object in values has a field of the given name, which contains an ID that maps to an object in _targetValues_.
				var targetIdMap = {};
				targetValues.forEach(v => {
					targetIdMap[v.id] = v;
					v[inc.field] = [];
				});
				inc.values.forEach(v => {
					var target = targetIdMap[v[inc.map]];
					if(target){
						target[inc.field].push(v);
					}
				});
			}else if(inc.map){
				// It's an array of tuples. The first is a source ID, second is target ID.
				var srcIdMap = {};
				targetValues.forEach(v => {
					srcIdMap[v.id] = v;
					v[inc.field] = [];
				});
				var targetIdMap = {};
				inc.values.forEach(v => targetIdMap[v.id] = v);
				for(var n=0;n<inc.map.length;n+=3){
					var id = inc.map[n]; // the mapping entry ID.
					var a = srcIdMap[inc.map[n+1]];
					var b = targetIdMap[inc.map[n+2]];
					if(a && b){
						// todo: only push a if id not already in set.
						// where should id be stored though? (it's not the same thing as a.id - it's the actual mapping row's id).
						a[inc.field].push(b);
					}
				}
			}
		}
	}
	
	return response.result ? response.result : response;
}

function mapWhere(where, args){
	var str = '';
	if(Array.isArray(where)){
		for(var i=0;i<where.length;i++){
			if(str){
				str += where[i].and ? ' and ' : ' or ';
			}
			str+='(' + mapWhere(where[i], args) + ')';
		}
	}else{
		for(var k in where){
			if(k == 'and' || k == 'op'){
				continue;
			}
			var v = where[k];
			if(v === undefined){
				continue;
			}
			
			if(str != ''){str += ' and ';}
			
			if(Array.isArray(v)){
				str += k +' contains [?]'; // contains on an array is the same as containsAll. Different from containsAny and containsNone.
				args.push(v);
			}else if(v!==null && typeof v === 'object'){
				for(var f in v){
					
					switch (f)
					{
						case "startsWith":
							str += k + " sw ?";
							args.push(v[f]);
						break;
						case "contains":
							str += k + " contains " + (Array.isArray(v[f]) ? '[?]' : '?');
							args.push(v[f]);
						break;
						case "containsNone":
						case "containsAny":
						case "containsAll":
							str += k + " " + f + " [?]";
							args.push(v[f]);
						break;
						case "endsWith":
							str += k + " endsWith ?";
							args.push(v[f]);
						break;
						case "geq":
						case "greaterThanOrEqual":
							str += k + ">=?";
							args.push(v[f]);
						break;
						case "greaterThan":
							str += k + ">?";
							args.push(v[f]);
						break;
						case "lessThan":
							str += k + "<?";
							args.push(v[f]);
						break;
						case "leq":
						case "lessThanOrEqual":
							str += k + "<=?";
							args.push(v[f]);
						break;
						case "not":
							str += k + "!=" + (Array.isArray(v[f]) ? '[?]' : '?');
							args.push(v[f]);
							break;

						case "name":
						case "equals":
							str += k + "=" + (Array.isArray(v[f]) ? '[?]' : '?');
							args.push(v[f]);
                            break;
						default:
                            break;
                    }
					
				}
			}else{
				str += k +'=?';
				args.push(v);
			}
		}
	}
	
	return str;
}

const _lazyCache = {};

/*
* Lazy loads a .js file represented by a url.
*/
function lazyLoad(url, globalScope) {
	var cache;
	
	if(globalScope){
		if(!globalScope._lazyCache){
			globalScope._lazyCache = {};
		}
		
		cache = globalScope._lazyCache;
	}else{
		cache = _lazyCache;
		globalScope = global;
	}
	
	var entry = cache[url];
	if(!entry){
		entry = webRequest(url, null, {rawText:1})
		.then(resp => {
			var js = resp.text;
			try{
				var ex={};
				var f = new Function('exports','global','window', js);
				f(ex, globalScope, globalScope);
				ex.window = globalScope;
				cache[url]=Promise.resolve(ex);
			}catch(e){
				console.log(e);
			}
			return cache[url];
		});
		cache[url] = entry;
	}
	return entry;
}

export { expandIncludes, lazyLoad };

export default function webRequest(origUrl, data, opts) {
	var apiUrl = global.apiHost || '';
	
	if(!apiUrl.endsWith('/')){
		apiUrl += '/';
	}
	
	apiUrl += 'v1/';
	
	var url = (origUrl.indexOf('http') === 0 || origUrl.indexOf('file:') === 0 || origUrl[0] == '/' || origUrl[0] == '.') ? origUrl : apiUrl + origUrl;
	
	return new Promise((success, reject) => {
		_fetch(url, data, opts).then(response => {
			if(global.storedToken && response.headers){
				var token = response.headers.get('Token');
				if(token){
					store.set('context', token);
				}
			}
			
			if(opts && opts.blob){
				return response.blob().then(blob => {
					if (!response.ok) {
						return reject({ error: 'invalid response', blob });
					}
					
					// Run success now:
					response.json = response.blob = blob;
					success(response);
				})
			}
			
			if(opts && opts.rawText){
				return response.text().then(text => {
					if (!response.ok) {
						return reject({ error: 'invalid response', text });
					}
					
					// Run success now:
					response.json = response.text = text;
					success(response);
				})
			}
			
			// Get the response as json:
			return response.text().then(text => {
				// Catchable parse:
				let json;

				try {
					json = (text && text.length) ? JSON.parse(text) : null;
				} catch (e) {
					console.log(url, e);
					reject({ error: 'invalid/json', text });
					return;
				}
				
                if (!response.ok) {
                    reject(json || { error: 'invalid response' });
					return;
                }
				
				// Run success now:
				response.json = expandIncludes(json);
				
				// If we're not blocked from doing so with the options
				// and this was either a DELETE or POST request which returned an entity
				// then we'll fire off a contentchange event to tell the UI
				// that content has updated.
				
				// Trigger a contentchange event if it was a POST and it returned an entity:
				var method = 'get';
				
				if(opts && opts.method){
					method = opts.method.toLowerCase();
				}else if(data){
					method = 'post';
				}
				
				success(response);
				
				if(response.json && response.json.id && response.json.type && method != 'get'){
					
					// If method was 'delete' then this entity was deleted.
					// Otherwise, as it's not specified, contentchange will establish if it was added or deleted based on the given url.
					contentChange(response.json, origUrl, (method == 'delete') ? {deleted: true} : null);
				}
				
			}).catch(err => {
                console.log(err);
				reject(err);
			});
		}).catch(err => {
			console.log(err);
			reject(err);
		});
	});
}

// Converts where and on into a query formatted filter.
export function remapData(data, origUrl){
	
	// Data exists - does it have an old format filter?
	if(data.where){
		var where = data.where;
		var d2 = {...data};
		delete d2.where;
		var args = [];
		var str = '';
		
		if(where.from && where.from.type && where.from.id){
			str = 'From(' + where.from.type + ',?,' + where.from.map + ')';
			args.push(where.from.id);
			delete where.from;
		}else{
			str = '';
		}
		
		var q = mapWhere(where, args);
		
		if(q){
			if(str){
				// "From()" can only be combined with an and:
				str += ' and ' + q;
			}else{
				str = q;
			}
		}
		
		d2.query = str;
		d2.args = args;
		data = d2;
	}
	
	// this is done on list calls.
	if(data.on && data.on.type && data.on.id && origUrl.endsWith("/list")){
		var on = data.on;
		var d2 = {...data};
		delete d2.on;
		var onStatement = 'On(' + data.on.type + ',?' + (data.on.map ? ',"' + data.on.map + '"' : '') + ')';
		if(d2.query){
			d2.query = '(' + d2.query + ') and ' + onStatement;
		}else{
			d2.query = onStatement;
		}
		if(!d2.args){
			d2.args = [];
		}
		d2.args.push(data.on.id);
		data = d2;
	}
	
	return data;
}

function _fetch(url, data, opts) {
	var origUrl = url;
	var credentials = undefined;
	var mode = 'cors';

	var headers = opts ? opts.headers || {} : {};
	
	var toOrigin = true;
	
	// It's not to the origin if url is absolute and is a different server to our location origin
	if(url.indexOf('http') === 0){
		// different origin?
		
		if(!((opts && opts.toOrigin) || (global.apiHost && url.indexOf(global.apiHost) === 0) || url.indexOf(location.origin) === 0)){
			// Non-origin request
			toOrigin = false;
		}
		
	}
	
	if(toOrigin){
		if (global.settings && global.settings._version_) {
			headers.Version = global.settings._version_;
		}
		
		if(global.storedToken){
			headers['Token'] = global.storedTokenValue || store.get('context');
		}
		
		if(opts && opts.locale){
			headers['Locale'] = opts.locale;
		}
		
		credentials = global.storedToken ? undefined : 'include';
	}
	
	var includes = opts && opts.includes;
	
	if(Array.isArray(includes)){
		includes = includes.map(x=>x.trim()).join(',');
	}
	
	if(includes){
		url += '?includes=' + includes;
	}
	
	if (!data) {
		return fetch(url, { method: opts && opts.method ? opts.method : 'get', mode, credentials, headers });
	}
    
	if ((global.FormData && data instanceof global.FormData) || (global.Uint8Array && data instanceof global.Uint8Array)) {
		return fetch(url, {
			method: opts && opts.method ? opts.method : 'post',
            body: data,
			mode,
            credentials,
			headers
		});
	}
	
	data = remapData(data, origUrl);
	
	return fetch(url, {
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
