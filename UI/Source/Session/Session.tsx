import { expandIncludes } from "UI/Functions/WebRequest";
import { createContext, useContext, useState } from 'react';
import userApi from 'Api/User';

interface SessionContext {
	session: Session,
	sessionReload?: () => Promise<Session>,
	setSession: (s:SessionResponse) => Session
}

const sessionCtx = createContext<SessionContext>({
	session: {},
	setSession: (s) => ({ } as Session)
});

export function useSession(){
	return useContext(sessionCtx);
}

export function toSession(sr: SessionResponse) : Session {
	var s: any = {};
	var sra: any = (sr as any);

	for (var k in sra) {
		s[k] = expandIncludes(sra[k]);
	}

	return s as Session;
}

interface SessionProviderProps {
	initialState?: Session
}

export const Provider: React.FC<React.PropsWithChildren<SessionProviderProps>> = (props) => {

	let dispatchWithEvent = (updatedVal: SessionResponse, diff?: boolean) => {

		var ses = diff ? { ...session, ...toSession(updatedVal) } : toSession(updatedVal);

		var e = new CustomEvent('xsession', {
			detail: {
				state: ses,
				setSession
			}
		});

		document.dispatchEvent && document.dispatchEvent(e);
		setSession(ses);
		return ses;
	}
	
	let sessionReload = () => userApi.self(dispatchWithEvent)
		.catch(() => dispatchWithEvent({}));
	
	const [session, setSession] = useState(() : Session => {
		var newSession = props.initialState;

		if (!newSession && window.gsInit) {
			newSession = toSession(window.gsInit);
		}

		if (!newSession) {
			newSession = {
				loadingUser: sessionReload()
			};
		}

		return newSession;
	});
  
	return (
		<sessionCtx.Provider
			value={{
				session,
				sessionReload,
				setSession: dispatchWithEvent
			}}
		>
			{props.children}
		</sessionCtx.Provider>
	);
};

export {
	sessionCtx
}