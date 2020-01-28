/* This module sends web requests for us. */
import mapUrl from 'UI/Functions/MapUrl';
import store from 'UI/Functions/Store';
import contentChange from 'UI/Functions/ContentChange';

// import 'fetch-polyfill';

export default function webRequest(origUrl, data, opts) {
	var url = mapUrl(origUrl);
	
	return new Promise((success, reject) => {
		_fetch(url, data, opts).then(response => {
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
					reject(json);
					return;
				}

				// Run success now:
				response.json = json;
				
				
				// If we're not blocked from doing so with the options
				// and this was either a DELETE or POST request which returned an entity
				// then we'll fire off a contentchange event to tell the UI
				// that content has updated.
				
				// Trigger a contentchange event if it was a POST and it returned an entity:
				var method = '';
				
				if(opts && opts.method){
					method = opts.method.toLowerCase();
				}else if(data){
					method = 'post';
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

	// If we're logged in and the real user is not the same as the apparent user then tell the API we're phantoming:
	var headers = (storeState.user && storeState.user.id != storeState.realUser.id) ? {
		'Phantom-As': storeState.user.id
	} : {};

	if (global.settings && global.settings._version_) {
		headers.Version = global.settings._version_;
	}
    
	if(global.apiHost){
		headers['Token'] = store.get('user');
	}
	
	if (!data) {
		return fetch(url, { method: opts && opts.method ? opts.method : 'get', mode: 'cors', credentials: 'include', headers });
	}
    
	if (data instanceof FormData) {
		return fetch(url, {
			method: opts && opts.method ? opts.method : 'post',
            body: data,
			mode: 'cors',
            credentials: 'include',
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
		mode: 'cors',
		body: JSON.stringify(data),
		credentials: 'include'
	});
}
