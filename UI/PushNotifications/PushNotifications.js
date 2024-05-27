import store from 'UI/Functions/Store';
import { useSession, getDeviceId } from 'UI/Session';
import webRequest from 'UI/Functions/WebRequest';

// todo: use getConfig and pull from server to make this module easier to version
var firebaseConfig = {
	name: "My Project Name",
	apiKey: "AIzaxxxx",
	authDomain: "com-xxxx.firebaseapp.com",
	projectId: "com-xxxx",
	storageBucket: "com-xxxx.appspot.com",
	messagingSenderId: "xxxx",
	appId: "1:517xxxxx",
	measurementId: "G-xxxx"
};

var Context = React.createContext();
const deviceId = getDeviceId();

// If we're a Cordova app, then..
if (global.cordova) {
	document.addEventListener("deviceready", () => {
		var kb = global.Keyboard;
		kb && kb.shrinkView && kb.shrinkView(true);

		// Called when a push message received while app is in foreground
		cordova.plugins.firebase.messaging.onMessage((payload) => {
			var e = new Event('notification');
			e.mode = 'foreground';
			e.payload = payload;
			UpdateNotifications(payload);
			document.dispatchEvent && document.dispatchEvent(e);
		});

		// Called when a push message received while app is in background
		cordova.plugins.firebase.messaging.onBackgroundMessage((payload) => {
			var e = new Event('notification');
			e.mode = 'background';
			e.payload = payload;
			UpdateNotifications(payload);
			document.dispatchEvent && document.dispatchEvent(e);
		});

		if (window.cordova.platformId === 'android') {
			cordova.plugins.firebase.messaging.createChannel({
				id: "default",
				name: firebaseConfig.name,
				importance: 3,
				badge: true
			});
		}
	}, false);
}

function UpdateNotifications(payload) {
	if (payload) {
		if (payload.unreadNotifications > 0) {
			var storageData = store.get("user_profile");
			if (!storageData) {
				storageData = {};
			}

			storageData.NotificationCount = payload.unreadNotifications;
			store.set("user_profile", storageData);
		}
	}
}


/*
* Use to obtain raw info about the current push notification registration state.
*/
export function usePushState(){
	return React.useContext(Context);
}

function verifyKeyState(active, session, setSession){
	if(!session.userdevice){
		return;
	}

	var devicePushActive = !!session.userdevice.notificationKeyType;
	console.log('firebase state ', active, ' is desired. Current state: ', devicePushActive, session.userdevice.notificationKeyType);
	
	if(active == devicePushActive){
		return;
	}
	
	var pr;
	
	if(!active){
		pr = Promise.resolve({
			notificationKey: null,
			notificationKeyType: null
		});
	}else{
		// Set device key
		pr = setup();
	}

	return pr.then(key => {
		const deviceKeys = key;
		return webRequest('userdevice', key)
			.then(response => {
				const device = { id: deviceId, ...deviceKeys };
				//console.info(device, response);
				setSession({ ...session, userdevice: device });
			});
	})
	.catch(e => console.error(e));
}

/*
* Must use this in your render tree in order to use PN registration.
*/
export const Provider = (props) => {
	const [pushState, setPushState] = React.useState({active: true});

	let setActive = (active, session, setSession) => {

		// If active is true, make sure push notifs are prompted for and setup.
		setPushState({...pushState, active});
		store.set("firebase_state", {active});
		verifyKeyState(active, session, setSession);
	}
	
	React.useEffect(() => {

		var state = store.get("firebase_state");
		if(state){
			setPushState({active: state.active});
		}
		
		document.addEventListener('xsession', e => {
			if(e.state && e.state.userdevice){
				// Got device info. Check if push state matches what the device wants:
				verifyKeyState(pushState.active, e.state, e.setSession);
			}
		});
	}, []);
	
	return (
		<Context.Provider
			value={{
				pushState,
				setActive
			}}
		>
			{props.children}
		</Context.Provider>
	);
};

export const PushNotificationConsumer = (props) => {
	const {session, setSession} = useSession();
	return <Context.Consumer>{
		v => v ? props.children(v.pushState, state => v.setActive(state, session, setSession)) : null
	}</Context.Consumer>;
}

var latestKey = null;

function setup() {
	if(latestKey){
		return Promise.resolve(latestKey);
	}
	
	if(window.cordova){
		return cordova.plugins.firebase.messaging.requestPermission()
			.then(() => cordova.plugins.firebase.messaging.getToken())
			.then((token) => {
				console.log('retrieved firebase token (firebase)');
				latestKey = {
					notificationKey: token,
					notificationKeyType: 'firebase'
				};
				return latestKey;
			});
	}
	
	var pr;
	if(global.Notification && global.Notification.requestPermission){
		pr = global.Notification.requestPermission();
	}else{
		pr = Promise.resolve(true);
	}
	
	return pr.then(result => {
		if (result !== 'granted') {
			console.log('firebase access not granted');
			throw new Error('Not granted');
		}
		
		var firebase = global.firebase;
		
		// Initialize Firebase
		if (firebase.apps.length === 0) {
			firebase.initializeApp(firebaseConfig);
		}

		console.log('attempting to retrieve firebase token');

		return firebase.messaging().getToken(undefined, '*');
	})
	.then((currentToken) => {
		console.log('retrieved firebase token (web)');
		latestKey = {
			notificationKey: currentToken,
			notificationKeyType: 'web'
		};
		return latestKey;
	});
}