import webRequest from 'UI/Functions/WebRequest';

function clearAndNav(url, setSession, setPage){
	setSession({
		role: {id: 0},
		loadingUser: false
	});
	setPage(url);
}

export default (url, setSession, setPage) => {
	if(!setSession || !setPage){
		throw new Error('Logout requires ctx');
	}
	
	return webRequest('user/logout')
		.then(() => clearAndNav(url || '/', setSession, setPage))
		.catch(e => clearAndNav(url || '/', setSession, setPage));
};