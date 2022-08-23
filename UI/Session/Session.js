import webRequest, {expandIncludes} from "UI/Functions/WebRequest";

let initState = null;

if(global.gsInit){
	initState = global.gsInit;
	
	for(var k in initState){
		initState[k] = expandIncludes(initState[k]);
	}
	
	initState.loadingUser = false;
}else{
	initState = {loadingUser: true};
}

const Session = React.createContext();
const Router = React.createContext();
export { Session, Router };

export function useSession(){
	// returns {session, setSession}
	return React.useContext(Session);
}

export function useTheme(){
	return getCfg('globaltheme') || {};
}

export function useConfig(name){
	return getCfg(name);
}

function getCfg(name){
	return global.__cfg ? global.__cfg[name.toLowerCase()] : null;
}

export function getDeviceId(){
	var store = window.localStorage;
	if(!store){
		return null;
	}
	var device = store.getItem("device");
	if(device != null){
		device = JSON.parse(device);
	}else{
		device = {v: 1, id: generateId(20)};
		store.setItem("device", JSON.stringify(device));
	}
	return device.id;
}

function generateId (len) {
	var arr = new Uint8Array(len / 2);
	window.crypto.getRandomValues(arr);
	return arr.map(dec => dec.toString(16).padStart(2, '0')).join('');
}

export const Provider = (props) => {
	const [session, setSession] = React.useState(props.initialState || initState);
  
	let dispatchWithEvent = (updatedVal, diff) => {
		if(diff){
			updatedVal = {...session, ...updatedVal};
		}
		for(var k in updatedVal){
			updatedVal[k] = expandIncludes(updatedVal[k]);
		}
		var e = new Event('xsession');
		e.state = updatedVal;
		e.setSession = setSession;
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
export const ConfigConsumer = (props) => props.children(getCfg(props.name));
export const ThemeConsumer = (props) => props.children(getCfg('globaltheme'));

export function useRouter() {
	// returns {page, setPage}
	return React.useContext(Router);
};