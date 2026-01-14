import userApi from 'Api/User';

/**
 * Logs out and then navigates to the specified URL, or the homepage if one is not specified.
 * Provide setSession from UI/Session and setPage from UI/Router.
 */
export default (url: string, setSession: (s: SessionResponse) => Session, setPage : (url: string) => void) => {
	if(!setSession || !setPage){
		throw new Error('Logout requires ctx');
	}
	
	return userApi.logout(setSession)
		.then(response => setPage(url || '/'))
		.catch(e => {
			setSession({});
			setPage(url || '/');
		});
};