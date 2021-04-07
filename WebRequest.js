/* This module sends web requests for us. */
import store from 'UI/Functions/Store';
import contentChange from 'UI/Functions/ContentChange';

export default function webRequest(origUrl, data, opts) {
	var url = (global.apiHost || '') + '/v1/' + origUrl;
	
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
				
				success(response);
				
				if((method == 'post' || method =='delete') && json && json.id && json.type){
					
					// If method was 'delete' then this entity was deleted.
					// Otherwise, as it's not specified, contentchange will establish if it was added or deleted based on the given url.
					contentChange(json, origUrl, (method == 'delete') ? {deleted: true} : null);
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
    
	if (global.FormData && data instanceof global.FormData) {
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
