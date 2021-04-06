import webRequest from 'UI/Functions/WebRequest';

/* cache */
var cache = null;

/*
* Gets the list of content types on the site. Returns a promise.
*/
export default (capName, context) => {
	var gs = (context.app || global.app).state;
	
	if(gs.loadingUser){
		return gs.loadingUser.then(() => loadCached(capName, context));
	}
	
	return loadCached(capName);
}

function loadCached(capName, context) {
	if(cache != null){
		return Promise.resolve(cache).then(caps => !!caps[capName]);
	}
	
	var gs = (context.app || global.app).state;
	var roleId = gs.user ? gs.user.role : 0;
	
	return cache = webRequest("permission/role/" + roleId).then(response => {
		cache = {};
		response.json.capabilities.forEach(c => {
			cache[c.name] = c;
		});
		return cache;
	}).then(caps => !!caps[capName]);
}