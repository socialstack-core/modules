import webRequest from 'UI/Functions/WebRequest';

function clearAndNav(url, context){
	var state = {};
	
	var ctxApp = (context.app || global.app);
	
	for(var k in ctxApp.state){
		if(k == 'url' || k == 'loadingUser'){
			continue;
		}
		state[k] = null;
	}
	
	state.role={id: 0};
	ctxApp.setState(state);
	global.pageRouter.go(url);
}

export default (url, context) => {
	if(!context){
		console.warn('Logout requires context (this.context from a component)');
		context = global;
	}
	return webRequest('user/logout')
		.then(() => clearAndNav(url || '/', context))
		.catch(e => clearAndNav(url || '/', context));
};