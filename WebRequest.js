/* This module sends web requests for us. */
import mapUrl from 'UI/Functions/MapUrl';
import store from 'UI/Functions/Store';
import contentChange from 'UI/Functions/ContentChange';

var evtHandler = null;

function receivedContent(content){
	if(!content || !content.type){
		return;
	}
	
	if(!evtHandler){
		evtHandler = global.events.get('UI/Functions/WebRequest');
	}
	
	// Trigger a general use handler:
	var evtFunc = evtHandler['on' + content.type];
	if(evtFunc){
		evtFunc(content);
	}
}

export default function webRequest(origUrl, data, opts) {
	var url = mapUrl(origUrl);
	
	return new Promise((success, reject) => {
		_fetch(url, data, opts).then(response => {
			if(global.storedToken && response.headers){
				var token = response.headers.get('Token');
				if(token){
					store.set('context', token);
				}
			}
			
			// Get the response as json:
			return response.text().then(text => {
				// Catchable parse:
				let json;

				try {
					json = (text && text.length) ? JSON.parse(text) : null;
				} catch (e) {
					console.log(e);
					reject({ error: 'invalid/json' });
					return;
				}
				
                if (!response.ok) {
                    if (json) {
                        reject(json);
                    } else {
                        reject({ error: 'invalid response' });
                    }
                return;
                }

				// Run success now:
				response.json = json;
				
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
				
				if((method == 'post' || method == 'get') && json && json.results){
					json.results.forEach(content => {
						receivedContent(content);
					});
				}else if(method == 'get'){
					receivedContent(json);
				}
				
				success(response);
				
				if((method == 'post' || method =='delete') && json && json.id && json.type){
					
					// If method was 'delete' then this entity was deleted.
					// Otherwise, as it's not specified, contentchange will establish if it was added or deleted based on the given url.
					contentChange(json, origUrl, {deleted: (method == 'delete')});
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

function _fetch(url, data, opts) {
	// Get the global user state - we want to see if we're phantoming as somebody.
	var storeState = global.app.state;
	var credentials = global.storedToken ? undefined : 'include';
	var mode = 'cors';

	var headers = {};

	if (global.settings && global.settings._version_) {
		headers.Version = global.settings._version_;
	}
    
	if(global.storedToken){
		headers['Token'] = store.get('context');
	}
	
	if(opts && opts.locale){
		headers['Locale'] = opts.locale;
	}
	
	if (!data) {
		return fetch(url, { method: opts && opts.method ? opts.method : 'get', mode, credentials, headers });
	}
    
	if (data instanceof FormData) {
		return fetch(url, {
			method: opts && opts.method ? opts.method : 'post',
            body: data,
			mode,
            credentials,
			headers
		});
	}
	
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
