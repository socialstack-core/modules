import { createContext, useState, useEffect, useContext } from 'react';

/*
* ToastInfo will be passed directly to your ToastList render function.
* toastInfo.duration defines how long it's visible for (don't set it at all if you want it to be explicitly closed by the user).
*/

export type ToastInfo = {
    timeout?: number,
    closeTime?: number,
    duration?: number
};

export type SessionToast = {
    toastList: ToastInfo[],
    pop: (toastInfo: ToastInfo) => void,
    close: (toastInfo: ToastInfo) => void
};

const SessionToasts = createContext<SessionToast | undefined>(undefined);

export const Provider = (props : React.PropsWithChildren) => {
	const [toastList, setToastList] = useState<ToastInfo[]>([]);

	let close =  (toastInfo:ToastInfo) => {
		setToastList(toastList.filter(toast => toast != toastInfo));
	}

	useEffect(() => {
        toastList.forEach(toast => {
            var now = Date.now();
            toast.timeout = setTimeout(() => {
                close(toast);
            }, toast.closeTime! - now);
        })

        return () => {
            toastList.forEach(toast => {
                if(toast.timeout){
                    clearTimeout(toast.timeout);
                }
            });
        };

    }, [toastList]);


	let pop = (toastInfo:ToastInfo) => {
        if(toastInfo.duration){
            toastInfo.closeTime = Date.now() + (toastInfo.duration * 1000);
        }

        setToastList([...toastList, toastInfo]);
    }

	return (
		<SessionToasts.Provider
			value={{
				toastList,
				pop,
				close
			}}
		>
			{props.children}
		</SessionToasts.Provider>
	);
};

export { SessionToasts }; 

export function useToast() {
	return useContext(SessionToasts);
}
