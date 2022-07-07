import webRequest from 'UI/Functions/WebRequest';

function clearAndNav(url, setSession, setPage, ctx){
	if(ctx){
		setSession(ctx);
	}else{
		setSession({
			user: null,
			realUser: null,
			role: {id: 0},
			loadingUser: false
		}, true);
	}
	setPage(url);
}

export default (url, setSession, setPage) => {
	if(!setSession || !setPage){
		throw new Error('Logout requires ctx');
	}
	
	return webRequest('user/logout')
		.then(response => clearAndNav(url || '/', setSession, setPage, response.json))
		.catch(e => clearAndNav(url || '/', setSession, setPage));
};