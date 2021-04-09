import webRequest from "UI/Functions/WebRequest";

const initState = global.gsInit ? {...global.gsInit, loadingUser: false} : {loadingUser: true};

const Session = React.createContext();
const Router = React.createContext();
export { Session, Router };

export function useSession(){
	// returns {session, setSession}
	return React.useContext(Session);
}

export const Provider = (props) => {
	const [session, setSession] = React.useState(initState);
  
	let dispatchWithEvent = updatedVal => {
		var e = new Event('xsession');
		e.state = updatedVal;
		document.dispatchEvent && document.dispatchEvent(e);
		return setSession(updatedVal);
	}
	
	if(session.loadingUser === true){
		session.loadingUser = webRequest("user/self")
			.then((response) => {
				var state = (response?.json) ? { ...response.json, loadingUser: null } : {loadingUser: null};
				dispatchWithEvent(state);
				return state;
			}).catch(() => dispatchWithEvent({
				user: null,
				realUser: null,
				loadingUser: null
			}))
	}
	
	return (
		<Session.Provider
			value={{
				session,
				setSession: dispatchWithEvent
			}}
		>
			{props.children}
		</Session.Provider>
	);
};

export const SessionConsumer = (props) => <Session.Consumer>{v => props.children(v.session, v.setSession)}</Session.Consumer>;
export const RouterConsumer = (props) => <Router.Consumer>{v => props.children(v.pageState, v.setPage)}</Router.Consumer>;

export function useRouter() {
	// returns {page, setPage}
	return React.useContext(Router);
};