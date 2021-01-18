import webRequest from 'UI/Functions/WebRequest';

function clearAndNav(url){
	var state = {};
	
	for(var k in global.app.state){
		if(k == 'url' || k == 'loadingUser'){
			continue;
		}
		state[k] = null;
	}
	
	state.role={id: 0};
	global.app.setState(state);
	global.pageRouter.go(url);
}

export default (url) => {
	return webRequest('user/logout')
		.then(() => clearAndNav(url || '/'))
		.catch(e => clearAndNav(url || '/'));
};